using RDPSecure.Data;
using RDPSecure.Logging;
using RDPSecure.Services;

namespace RDPSecure;

public partial class MainForm : Form
{

    //------------------------------------------Modern Design (Dark Theme)
      private static class ThemeColors
    {
        public static readonly Color FormBackground = Color.FromArgb(45, 45, 48);
        public static readonly Color PanelBackground = Color.FromArgb(28, 28, 30);
        public static readonly Color CardBackground = Color.FromArgb(37, 37, 38);
        public static readonly Color TextPrimary = Color.FromArgb(240, 240, 240);
        public static readonly Color TextSecondary = Color.FromArgb(180, 180, 180);
        public static readonly Color GridHeader = Color.FromArgb(45, 45, 48);
        public static readonly Color GridAlternate = Color.FromArgb(32, 32, 35);
        public static readonly Color AccentBlue = Color.FromArgb(52, 152, 219);

        public static readonly Color ActiveBansColor = Color.FromArgb(255, 69, 58);      // Red
        public static readonly Color AttemptsColor = Color.FromArgb(52, 152, 219);       // Blue
        public static readonly Color WhitelistedColor = Color.FromArgb(50, 195, 50);     // Green
    }

    private void ApplyDarkTheme()
    {
        // Form level styling
        this.BackColor = ThemeColors.FormBackground;

        // Stats panel styling
        panelStats.BackColor = ThemeColors.FormBackground;

        // Style the stat panels
        StyleStatPanel(panelActiveBans, lblActiveBansTitle, lblActiveBansCount, "Active Bans", ThemeColors.ActiveBansColor);
        StyleStatPanel(panelRecentAttempts, lblRecentAttemptsTitle, lblRecentAttemptsCount, "Recent Attempts (24h)", ThemeColors.AttemptsColor);
        StyleStatPanel(panelWhitelisted, lblWhitelistedTitle, lblWhitelistedCount, "Whitelisted IPs", ThemeColors.WhitelistedColor);

        // Style buttons
        StyleDarkButton(btnSettings);
        StyleDarkButton(btnExit);

        // Style the grid panel
        panelGrid.BackColor = ThemeColors.FormBackground;

        // Style the grid
        StyleDarkGrid();
    }

    private void StyleStatPanel(Panel panel, Label titleLabel, Label countLabel, string title, Color accentColor)
    {
        // Panel styling
        panel.BackColor = ThemeColors.CardBackground;
        panel.BorderStyle = BorderStyle.None;

        // Title label styling
        titleLabel.Text = title;
        titleLabel.ForeColor = ThemeColors.TextSecondary;
        titleLabel.Font = new Font("Segoe UI", 9.75F);
        titleLabel.BackColor = Color.Transparent;

        // Count label styling
        countLabel.ForeColor = accentColor;
        countLabel.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
        countLabel.BackColor = Color.Transparent;

        // Add subtle border effect
        panel.Paint += (s, e) =>
        {
            var darkBorder = Color.FromArgb(20, 20, 20);
            using (var pen = new Pen(darkBorder))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            }
        };
    }

    private void StyleDarkButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = ThemeColors.AccentBlue;
        button.ForeColor = Color.White;
        button.Font = new Font("Segoe UI", 9F);
        button.Cursor = Cursors.Hand;

        // Hover effect
        button.MouseEnter += (s, e) =>
        {
            button.BackColor = ControlPaint.Light(ThemeColors.AccentBlue);
        };
        button.MouseLeave += (s, e) =>
        {
            button.BackColor = ThemeColors.AccentBlue;
        };
    }
    private void StyleDarkGrid()
    {
        gridBannedIPs.BackgroundColor = ThemeColors.PanelBackground;
        gridBannedIPs.GridColor = Color.FromArgb(50, 50, 50);
        gridBannedIPs.BorderStyle = BorderStyle.None;
        gridBannedIPs.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

        // Header styling
        var headerStyle = gridBannedIPs.ColumnHeadersDefaultCellStyle;
        headerStyle.BackColor = ThemeColors.GridHeader;
        headerStyle.ForeColor = ThemeColors.TextPrimary;
        headerStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        headerStyle.SelectionBackColor = ThemeColors.GridHeader;
        headerStyle.SelectionForeColor = ThemeColors.TextPrimary;
        gridBannedIPs.EnableHeadersVisualStyles = false;

        // Row styling
        var defaultStyle = gridBannedIPs.DefaultCellStyle;
        defaultStyle.BackColor = ThemeColors.CardBackground;
        defaultStyle.ForeColor = ThemeColors.TextPrimary;
        defaultStyle.SelectionBackColor = ThemeColors.AccentBlue;
        defaultStyle.SelectionForeColor = Color.White;
        defaultStyle.Font = new Font("Segoe UI", 9F);

        // Alternate row styling
        var alternateStyle = gridBannedIPs.AlternatingRowsDefaultCellStyle;
        alternateStyle.BackColor = ThemeColors.GridAlternate;
        alternateStyle.ForeColor = ThemeColors.TextPrimary;
        alternateStyle.SelectionBackColor = ThemeColors.AccentBlue;
        alternateStyle.SelectionForeColor = Color.White;

        // Fix the "Status" column style for banned entries
        gridBannedIPs.Columns["Column5"].DefaultCellStyle.ForeColor = ThemeColors.ActiveBansColor;

        // Handle status colors in CellFormatting event
        gridBannedIPs.CellFormatting += (s, e) =>
        {
            if (e.ColumnIndex == gridBannedIPs.Columns["Column5"].Index)
            {
                if (e.Value?.ToString() == "Banned")
                {
                    e.CellStyle.ForeColor = ThemeColors.ActiveBansColor;
                }
            }
        };
    }
    private void InitializeDarkTheme()
    {
        ApplyDarkTheme();

        // Enable double buffering for smoother rendering
        this.SetStyle(
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint,
            true
        );
    }

    //------------------------------------------Modern Design (Dark Theme)

    private void UpdateBannedIPsDisplay()
    {
        gridBannedIPs.Rows.Clear();
        var activeBans = monitorService.GetActiveBans();

        foreach (var ban in activeBans.Values)
        {
            gridBannedIPs.Rows.Add(
                ban.IPAddress,
                ban.Location,
                TimeFormatter.FormatDuration(ban.Duration),
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
            InitializeDarkTheme();

            // Set form and notification icons
            string iconPath = Path.Combine(Application.StartupPath, "shield.ico");
            if (File.Exists(iconPath))
            {
                using var icon = new Icon(iconPath);
                this.Icon = icon;
                notifyIconRDPSecure.Icon = icon;
            }

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
                    TimeFormatter.FormatDuration(e.Duration),
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