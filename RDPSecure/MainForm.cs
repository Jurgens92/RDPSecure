namespace RDPSecure;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
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
}