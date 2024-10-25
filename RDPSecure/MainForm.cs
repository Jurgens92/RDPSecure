namespace RDPSecure;

public partial class MainForm : Form
{
    private AppSettings settings;
    public MainForm()
    {
        InitializeComponent();
        AddSampleData();
        SetupEventHandlers();
        settings = SettingsManager.LoadSettings();
        SetupEventHandlers();
        UpdateUIFromSettings();
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


        // system tray icon------------------------------------
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
        // end of tray icon--------------------------------------


        // Configure notify icon
        notifyIconRDPSecure.Icon = SystemIcons.Shield;
        notifyIconRDPSecure.ContextMenuStrip = contextMenuTray;
        notifyIconRDPSecure.Visible = true;
        notifyIconRDPSecure.DoubleClick += OnOpenDashboard;
    }


    private void OnOpenDashboard(object sender, EventArgs e)
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }


    private void OnOpenSettings(object sender, EventArgs e)
    {
        using (var settingsForm = new SettingsForm())
        {
            if (settingsForm.ShowDialog(this) == DialogResult.OK)
            {
                // Reload settings after dialog closes
                settings = SettingsManager.LoadSettings();
                UpdateUIFromSettings();
            }
        }
    }


    private void OnExit(object sender, EventArgs e)
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
            notifyIconRDPSecure.ShowBalloonTip(2000, "RDPSecure",
                "Application is still running in the system tray.",
                ToolTipIcon.Info);
        }
    }










    //Sample Data-------------------------------------------------------------------------------------
    private void AddSampleData()
    {
        gridBannedIPs.Rows.Add("192.168.1.100", "Internal Network", "1 hour", "3", "Banned");
        gridBannedIPs.Rows.Add("203.0.113.45", "United States", "30 days", "5", "Banned");
        gridBannedIPs.Rows.Add("198.51.100.76", "Germany", "30 days", "8", "Banned");
    }

  
}

