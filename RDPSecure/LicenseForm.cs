using RDPSecure.Licensing;
using RDPSecure.Logging;

namespace RDPSecure
{
    public partial class LicenseForm : Form
    {
        private readonly LicenseManager _licenseManager;
        private readonly ISecurityLogger _logger;

        public LicenseForm(ISecurityLogger logger)
        {
            _logger = logger;
            _licenseManager = new LicenseManager(logger);
            InitializeComponent();
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            try
            {
                var (isValid, expiryDate) = _licenseManager.ValidateLicense();

                if (isValid)
                {
                    if (expiryDate.HasValue)
                    {
                        lblStatus.Text = $"Licensed until: {expiryDate.Value:d}";
                        lblStatus.ForeColor = Color.Green;
                    }
                    else
                    {
                        var trialDays = 30 - (DateTime.UtcNow - File.GetCreationTime(_licenseManager.TrialPath)).TotalDays;
                        lblStatus.Text = $"Trial Version: {trialDays:F0} days remaining";
                        lblStatus.ForeColor = Color.Blue;
                    }
                    btnContinue.Enabled = true;
                }
                else
                {
                    lblStatus.Text = "License expired or invalid";
                    lblStatus.ForeColor = Color.Red;
                    btnContinue.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error updating license status", ex);
                lblStatus.Text = "Error checking license status";
                lblStatus.ForeColor = Color.Red;
                btnContinue.Enabled = false;
            }
        }

        private void btnActivate_Click(object sender, EventArgs e)
        {
            try
            {
                string licenseKey = txtLicenseKey.Text.Trim();
                if (string.IsNullOrEmpty(licenseKey))
                {
                    MessageBox.Show(
                        "Please enter a license key.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                if (_licenseManager.ActivateLicense(licenseKey))
                {
                    MessageBox.Show(
                        "License activated successfully!",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    UpdateStatus();
                }
                else
                {
                    MessageBox.Show(
                        "Invalid license key.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error activating license", ex);
                MessageBox.Show(
                    $"Error activating license: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void btnContinue_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!btnContinue.Enabled && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                MessageBox.Show(
                    "Please activate a valid license or use trial version to continue.",
                    "License Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
            base.OnFormClosing(e);
        }
    }
}