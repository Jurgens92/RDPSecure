using RDPSecure.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace RDPSecure
{
    public partial class UpdateForm : Form
    {
        private readonly UpdateManager _updateManager;
        private readonly UpdateInfo _updateInfo;
        private readonly ISecurityLogger _logger;

        public UpdateForm(UpdateManager updateManager, UpdateInfo updateInfo, ISecurityLogger logger)
        {
            _updateManager = updateManager;
            _updateInfo = updateInfo;
            _logger = logger;

            InitializeComponent();
            LoadUpdateInfo();
        }

        private void LoadUpdateInfo()
        {
            lblNewVersion.Text = $"Version {_updateInfo.Version}";
            txtReleaseNotes.Text = _updateInfo.ReleaseNotes;
            lblReleaseDate.Text = $"Released: {_updateInfo.ReleaseDate:d}";

            if (_updateInfo.IsCritical)
            {
                lblCritical.Visible = true;
                lblCritical.Text = "Critical Update";
                lblCritical.ForeColor = Color.Red;
            }
        }

        private async void btnInstall_Click(object sender, EventArgs e)
        {
            try
            {
                btnInstall.Enabled = false;
                btnCancel.Enabled = false;
                progressBar.Visible = true;
                lblStatus.Visible = true;

                var progress = new Progress<int>(value =>
                {
                    progressBar.Value = value;
                    lblStatus.Text = $"Downloading... {value}%";
                });

                await _updateManager.DownloadAndInstallUpdate(_updateInfo, progress);

                MessageBox.Show(
                    "Update installed successfully. The application will now restart.",
                    "Update Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error installing update", ex);
                MessageBox.Show(
                    $"Error installing update: {ex.Message}",
                    "Update Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                btnInstall.Enabled = true;
                btnCancel.Enabled = true;
                progressBar.Visible = false;
                lblStatus.Visible = false;
            }
        }
    }
}