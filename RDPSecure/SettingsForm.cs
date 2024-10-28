using RDPSecure.Logging;

namespace RDPSecure
{
    public partial class SettingsForm : Form
    {
        private readonly AppSettings currentSettings;
        private readonly SecurityLogger logger;

        public SettingsForm()
        {
            InitializeComponent();
            logger = new SecurityLogger();
            currentSettings = SettingsManager.LoadSettings();

            // Load current settings into form
            LoadSettingsToForm();

            // Connect the OK button click event
            btnOK.Click += btnOK_Click;
        }

        private void LoadSettingsToForm()
        {
            try
            {
                // Protection Settings
                nudMaxAttempts.Value = currentSettings.MaxAttempts;
                nudTimeWindow.Value = currentSettings.TimeWindow;
                nudPrivateIPBanHours.Value = currentSettings.PrivateIPBanHours;
                nudPublicIPBanDays.Value = currentSettings.PublicIPBanDays;
                chkBurstProtection.Checked = currentSettings.BurstProtectionEnabled;

                // Load IP List
                gridIPList.Rows.Clear();
                foreach (var ip in currentSettings.WhitelistedIPs)
                {
                    gridIPList.Rows.Add(
                        ip.IPAddress,
                        ip.Type,
                        ip.AddedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        ip.IsEnabled ? "Enabled" : "Disabled"
                    );
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error loading settings into form", ex);
                MessageBox.Show(
                    $"Error loading settings: {ex.Message}",
                    "Settings Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                // Update the settings object with form values
                currentSettings.MaxAttempts = (int)nudMaxAttempts.Value;
                currentSettings.TimeWindow = (int)nudTimeWindow.Value;
                currentSettings.PrivateIPBanHours = (int)nudPrivateIPBanHours.Value;
                currentSettings.PublicIPBanDays = (int)nudPublicIPBanDays.Value;
                currentSettings.BurstProtectionEnabled = chkBurstProtection.Checked;

                // Update whitelist
                currentSettings.WhitelistedIPs.Clear();
                foreach (DataGridViewRow row in gridIPList.Rows)
                {
                    if (row.Cells["colIP"].Value != null)
                    {
                        var ipEntry = new IPEntry
                        {
                            IPAddress = row.Cells["colIP"].Value.ToString() ?? "",
                            Type = row.Cells["colType"].Value?.ToString() ?? "Whitelist",
                            AddedDate = DateTime.Parse(row.Cells["colAddedDate"].Value?.ToString() ?? DateTime.Now.ToString()),
                            IsEnabled = row.Cells["colStatus"].Value?.ToString() == "Enabled"
                        };
                        currentSettings.WhitelistedIPs.Add(ipEntry);
                    }
                }

                // Save the settings
                SettingsManager.SaveSettings(currentSettings);

                // Log success and close the form
                logger.LogInformation("Settings saved successfully");
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                logger.LogError("Error saving settings", ex);
                MessageBox.Show(
                    $"Error saving settings: {ex.Message}",
                    "Settings Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}