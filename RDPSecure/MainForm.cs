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



    private  AppSettings settings;
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

            AddSampleData();
            SetupEventHandlers();
            UpdateUIFromSettings();

            // Start monitoring
            monitorService.StartMonitoring();
            UpdateBannedIPsDisplay();
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



    private void OnLoginAttemptDetected(object? sender, LoginAttemptEventArgs e)
    {
        BeginInvoke(() => {
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
        BeginInvoke(() => {
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


    private void UpdateUIFromSettings()
    {
        // Update whitelisted count
        lblWhitelistedCount.Text = settings.WhitelistedIPs.Count.ToString();
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
            using (var settingsForm = new SettingsForm())
            {
                if (settingsForm.ShowDialog(this) == DialogResult.OK)
                {
                    // Reload settings after dialog closes
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










    //Sample Data-------------------------------------------------------------------------------------
    private void AddSampleData()
    {
        gridBannedIPs.Rows.Add("192.168.1.100", "Internal Network", "1 hour", "3", "Banned");
        gridBannedIPs.Rows.Add("203.0.113.46", "United States", "30 days", "5", "Banned");
        gridBannedIPs.Rows.Add("198.51.100.76", "Germany", "30 days", "8", "Banned");
    }

  
}

