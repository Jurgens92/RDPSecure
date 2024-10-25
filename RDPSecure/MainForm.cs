namespace RDPSecure;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
        AddSampleData();
        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        btnSettings.Click += OnOpenSettings;
    }

    private void OnOpenSettings(object sender, EventArgs e)
    {
        // We'll implement this when we create the settings form
        MessageBox.Show("Settings coming soon!");
    }
        //Sample Data
    private void AddSampleData()
    {
        gridBannedIPs.Rows.Add("192.168.1.100", "Internal Network", "1 hour", "3", "Banned");
        gridBannedIPs.Rows.Add("203.0.113.45", "United States", "30 days", "5", "Banned");
        gridBannedIPs.Rows.Add("198.51.100.76", "Germany", "30 days", "8", "Banned");
    }

}