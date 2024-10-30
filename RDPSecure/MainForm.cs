using RDPSecure.Logging;
using RDPSecure.Services;

namespace RDPSecure;

public partial class MainForm : Form
{
    private void UpdateBannedIPsDisplay()
    {
        gridBannedIPs.Rows.Clear();
        var activeBans = monitorService.GetActiveBans();

        foreach (var ban in activeBans.Values)
        {
            gridBannedIPs.Rows.Add(
                ban.IPAddress,
                ban.Location,
                ban.Duration.ToString(),
                ban.AttemptCount.ToString(),
                "Banned"
            );
        }

        lblActiveBansCount.Text = activeBans.Count.ToString();
    }


    private AppSettings settings;
    private readonly RDPMonitorService monitorService;
    private readonly SecurityLogger logger;


    public MainForm()
    {
        try
        {
            InitializeComponent();

            logger = new SecurityLogger();
            settings = SettingsManager.LoadSettings();
            monitorService = new RDPMonitorService(settings);

            // Subscribe to events
            monitorService.LoginAttemptDetected += OnLoginAttemptDetected;
            monitorService.IPBanned += OnIPBanned;

            SetupContextMenu();
            SetupEventHandlers();
            UpdateUIFromSettings();
            UpdateBannedIPsDisplay();

            // Start monitoring
            monitorService.StartMonitoring();

        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error initializing application: {ex.Message}",
                "Initialization Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            throw;
        }
    }


    private void SetupContextMenu()
    {
        var contextMenu = new ContextMenuStrip();

        // Remove IP option
        var removeItem = new ToolStripMenuItem("Remove Ban");
        removeItem.Click += OnRemoveBan;

        // Add to Whitelist option
        var whitelistItem = new ToolStripMenuItem("Add to Whitelist");
        whitelistItem.Click += OnAddToWhitelist;

        // Add items to context menu
        contextMenu.Items.AddRange(new ToolStripItem[]
        {
            removeItem,
            new ToolStripSeparator(),
            whitelistItem
        });

        // Assign context menu to grid
        gridBannedIPs.ContextMenuStrip = contextMenu;
    }

    private void OnRemoveBan(object? sender, EventArgs e)
    {
        if (gridBannedIPs.SelectedRows.Count > 0)
        {
            var row = gridBannedIPs.SelectedRows[0];
            string ip = row.Cells[0].Value.ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(ip))
                return;

            var result = MessageBox.Show(
                $"Are you sure you want to remove the ban for IP {ip}?",
                "Confirm Ban Removal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    monitorService.RemoveBan(ip);
                    gridBannedIPs.Rows.Remove(row);

                    // Update the counter
                    if (int.TryParse(lblActiveBansCount.Text, out int count))
                    {
                        lblActiveBansCount.Text = (count - 1).ToString();
                    }

                    logger.LogInformation($"Ban removed for IP: {ip}");
                }
                catch (Exception ex)
                {
                    logger.LogError("Error removing ban", ex);
                    MessageBox.Show(
                        $"Error removing ban: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }
    }

    private void OnAddToWhitelist(object? sender, EventArgs e)
    {
        if (gridBannedIPs.SelectedRows.Count > 0)
        {
            var row = gridBannedIPs.SelectedRows[0];
            string ip = row.Cells[0].Value.ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(ip))
                return;

            var result = MessageBox.Show(
                $"Are you sure you want to add {ip} to the whitelist?\n\nThis will remove the ban and allow future connections from this IP.",
                "Confirm Add to Whitelist",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    // First remove the ban
                    monitorService.RemoveBan(ip);
                    gridBannedIPs.Rows.Remove(row);

                    // Update the banned counter
                    if (int.TryParse(lblActiveBansCount.Text, out int count))
                    {
                        lblActiveBansCount.Text = (count - 1).ToString();
                    }

                    // Add to whitelist
                    var entry = new IPEntry
                    {
                        IPAddress = ip,
                        Type = "Whitelist",
                        AddedDate = DateTime.Now,
                        IsEnabled = true
                    };

                    // Load current settings, add the IP, and save
                    var settings = SettingsManager.LoadSettings();
                    settings.WhitelistedIPs.Add(entry);
                    SettingsManager.SaveSettings(settings);

                    // Update whitelist counter
                    if (int.TryParse(lblWhitelistedCount.Text, out int whitelistCount))
                    {
                        lblWhitelistedCount.Text = (whitelistCount + 1).ToString();
                    }

                    logger.LogInformation($"IP {ip} removed from ban list and added to whitelist");

                    MessageBox.Show(
                        $"IP {ip} has been successfully added to the whitelist.",
                        "Added to Whitelist",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    logger.LogError("Error managing IP whitelist", ex);
                    MessageBox.Show(
                        $"Error adding to whitelist: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }
    }


    private void OnLoginAttemptDetected(object? sender, LoginAttemptEventArgs e)
    {
        BeginInvoke(() =>
        {
            try
            {
                lblRecentAttemptsCount.Text =
                    (int.Parse(lblRecentAttemptsCount.Text) + 1).ToString();
            }
            catch (Exception ex)
            {
                logger.LogError("Error updating attempt count", ex);
            }
        });
    }

    private void OnIPBanned(object? sender, IPBanEventArgs e)
    {
        BeginInvoke(() =>
        {
            try
            {
                lblActiveBansCount.Text =
                    (int.Parse(lblActiveBansCount.Text) + 1).ToString();

                gridBannedIPs.Rows.Add(
                    e.IPAddress,
                    "Detecting...",
                    e.Duration.ToString(),
                    "Max attempts exceeded",
                    "Banned"
                );
            }
            catch (Exception ex)
            {
                logger.LogError("Error updating banned IPs", ex);
            }
        });
    }

    public void UpdateUIFromSettings()
    {
        // Update whitelisted count
        UpdateWhitelistCount();
    }

    public void RefreshWhitelistCount()
    {
        try
        {
            settings = SettingsManager.LoadSettings();  // Reload settings
            int count = settings.WhitelistedIPs.Count;
            lblWhitelistedCount.Text = count.ToString();
            logger.LogInformation($"Whitelist count updated to: {count}");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error refreshing whitelist count: {ex.Message}");
        }
    }

    public void UpdateWhitelistCount()
    {
        try
        {
            var currentSettings = SettingsManager.LoadSettings();
            lblWhitelistedCount.Text = currentSettings.WhitelistedIPs.Count.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError("Error updating whitelist count", ex);
        }
    }

    private void SetupEventHandlers()
    {
        //button clicks
        btnSettings.Click += OnOpenSettings;
        btnExit.Click += OnExit;

        //tooltip clicks
        toolStripMenuItem1.Click += OnOpenDashboard;  // Open Dashboard
        settingsToolStripMenuItem.Click += OnOpenSettings;  // Settings
        exitToolStripMenuItem.Click += OnExit;  // Exit
        notifyIconRDPSecure.DoubleClick += OnOpenDashboard; //Open Dashboard

        // whitelist click
        panelWhitelisted.Click += OnOpenWhitelistSettings;
        lblWhitelistedCount.Click += OnOpenWhitelistSettings;
        lblWhitelistedTitle.Click += OnOpenWhitelistSettings;
        panelWhitelisted.Cursor = Cursors.Hand;
        lblWhitelistedCount.Cursor = Cursors.Hand;
        lblWhitelistedTitle.Cursor = Cursors.Hand;

        // system tray icon
        contextMenuTray.Items.Clear();
        var openItem = new ToolStripMenuItem("Open Dashboard");
        openItem.Click += OnOpenDashboard;
        contextMenuTray.Items.Add(openItem);
        contextMenuTray.Items.Add(new ToolStripSeparator());
        var settingsItem = new ToolStripMenuItem("Settings");
        settingsItem.Click += OnOpenSettings;
        contextMenuTray.Items.Add(settingsItem);
        contextMenuTray.Items.Add(new ToolStripSeparator());
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += OnExit;
        contextMenuTray.Items.Add(exitItem);

        // Configure notify icon
        notifyIconRDPSecure.Icon = SystemIcons.Shield;
        notifyIconRDPSecure.ContextMenuStrip = contextMenuTray;
        notifyIconRDPSecure.Visible = true;
    }

    private void OnOpenWhitelistSettings(object? sender, EventArgs e)
    {
        try
        {
            using (var settingsForm = new SettingsForm(monitorService))
            {
                // Select the IP Management tab
                settingsForm.SelectIPManagementTab();

                if (settingsForm.ShowDialog(this) == DialogResult.OK)
                {
                    settings = SettingsManager.LoadSettings();
                    logger.LogInformation("Settings reloaded in MainForm");
                    UpdateUIFromSettings();
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Error opening settings dialog", ex);
            MessageBox.Show(
                "Error opening settings: " + ex.Message,
                "Settings Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private void OnOpenDashboard(object? sender, EventArgs e)
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void OnOpenSettings(object? sender, EventArgs e)
    {
        try
        {
            using (var settingsForm = new SettingsForm(monitorService))
            {
                if (settingsForm.ShowDialog(this) == DialogResult.OK)
                {
                    settings = SettingsManager.LoadSettings();
                    logger.LogInformation("Settings reloaded in MainForm");
                    UpdateUIFromSettings();
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Error handling settings dialog", ex);
            MessageBox.Show(
                "Error updating settings: " + ex.Message,
                "Settings Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        notifyIconRDPSecure.Visible = false;
        Application.Exit();
    }

    //on close event
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            notifyIconRDPSecure.ShowBalloonTip(
                2000,
                "RDPSecure Notification",
                "Application is still running in the system tray.",
                ToolTipIcon.Info
            );
        }
        else
        {
            monitorService.StopMonitoring();
        }

        base.OnFormClosing(e);
    }  

}