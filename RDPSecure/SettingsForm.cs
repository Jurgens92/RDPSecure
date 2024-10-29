using RDPSecure.Logging;
using RDPSecure.Services;
using System.Diagnostics;
using System.Net;
using System.ServiceProcess;

namespace RDPSecure
{
    public partial class SettingsForm : Form
    {
        private readonly AppSettings currentSettings;
        private readonly SecurityLogger logger;
        private readonly RDPMonitorService monitorService;

        public SettingsForm(RDPMonitorService monitorService)
        {
            InitializeComponent();
            InitializeSystemTab();
            this.monitorService = monitorService;
            logger = new SecurityLogger();
            currentSettings = SettingsManager.LoadSettings();

            LoadSettingsToForm();
            SetupIPManagement();
            btnOK.Click += btnOK_Click;
        }

        private void SetupIPManagement()
        {
            // Existing button click handlers
            btnAddWhitelist.Click += OnAddWhitelist;
            btnAddBlacklist.Click += OnAddBlacklist;

            // Setup IP validation
            txtIPAddress.TextChanged += (s, e) => ValidateIPAddress();

            // Setup context menu for the whitelist grid
            var contextMenu = new ContextMenuStrip();

            var removeItem = new ToolStripMenuItem("Remove from Whitelist");
            removeItem.Click += OnRemoveWhitelist;

            var toggleItem = new ToolStripMenuItem("Enable/Disable");
            toggleItem.Click += OnToggleWhitelistStatus;

            contextMenu.Items.AddRange(new ToolStripItem[]
            {
            removeItem,
            new ToolStripSeparator(),
            toggleItem
            });

            gridIPList.ContextMenuStrip = contextMenu;
        }

        private void OnRemoveWhitelist(object? sender, EventArgs e)
        {
            if (gridIPList.SelectedRows.Count > 0)
            {
                var row = gridIPList.SelectedRows[0];
                string ip = row.Cells["colIP"].Value?.ToString() ?? "";

                if (string.IsNullOrEmpty(ip))
                    return;

                var result = MessageBox.Show(
                    $"Are you sure you want to remove {ip} from the whitelist?\n\n" +
                    "This IP will be subject to normal protection rules after removal.",
                    "Confirm Remove",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        // Remove from settings
                        currentSettings.WhitelistedIPs.RemoveAll(w =>
                            string.Equals(w.IPAddress, ip, StringComparison.OrdinalIgnoreCase));

                        // Remove from grid
                        gridIPList.Rows.Remove(row);

                        // Save settings
                        SaveSettings();

                        logger.LogInformation($"IP {ip} removed from whitelist");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("Error removing IP from whitelist", ex);
                        MessageBox.Show(
                            $"Error removing IP: {ex.Message}",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
        }


        private void OnAddBlacklist(object sender, EventArgs e)
        {
            string ip = txtIPAddress.Text.Trim();

            // Validate IP
            if (!IPAddress.TryParse(ip, out _))
            {
                MessageBox.Show("Please enter a valid IP address.", "Invalid IP",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if IP is already in whitelist
            if (currentSettings.WhitelistedIPs.Any(w =>
                string.Equals(w.IPAddress, ip, StringComparison.OrdinalIgnoreCase)))
            {
                var result = MessageBox.Show(
                    "This IP is currently whitelisted. Do you want to remove it from whitelist and add to blacklist?",
                    "IP Whitelisted",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    // Remove from whitelist
                    currentSettings.WhitelistedIPs.RemoveAll(w =>
                        string.Equals(w.IPAddress, ip, StringComparison.OrdinalIgnoreCase));

                    // Update whitelist grid
                    for (int i = gridIPList.Rows.Count - 1; i >= 0; i--)
                    {
                        if (string.Equals(gridIPList.Rows[i].Cells["colIP"].Value.ToString(),
                            ip, StringComparison.OrdinalIgnoreCase))
                        {
                            gridIPList.Rows.RemoveAt(i);
                        }
                    }
                }
                else
                {
                    return;
                }
            }

            // Check if already blacklisted
            if (currentSettings.BlacklistedIPs.Any(b =>
                string.Equals(b.IPAddress, ip, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("This IP is already blacklisted.", "Duplicate IP",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Ask for optional notes
            string notes = "";
            using (var noteForm = new Form())
            {
                noteForm.Text = "Add Notes";
                noteForm.Size = new Size(400, 200);
                noteForm.StartPosition = FormStartPosition.CenterParent;

                var textBox = new TextBox
                {
                    Multiline = true,
                    Size = new Size(350, 100),
                    Location = new Point(15, 15)
                };

                var okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(290, 125)
                };

                noteForm.Controls.AddRange(new Control[] { textBox, okButton });
                if (noteForm.ShowDialog() == DialogResult.OK)
                {
                    notes = textBox.Text;
                }
            }

            // Add to blacklist
            var entry = new IPEntry
            {
                IPAddress = ip,
                Type = "Blacklist",
                AddedDate = DateTime.Now,
                IsEnabled = true,
                Notes = notes
            };

            currentSettings.BlacklistedIPs.Add(entry);

            // Add to banned IPs in monitor service
            monitorService.AddManualBan(ip, TimeSpan.FromDays(36500)); // Set a very long ban duration for manual blacklist

            // Save settings
            SaveSettings();

            // Clear the input
            txtIPAddress.Clear();

            MessageBox.Show(
                $"IP {ip} has been successfully blacklisted.",
                "IP Blacklisted",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            logger.LogInformation($"IP {ip} manually added to blacklist. Notes: {notes}");
        }

        private void SetupWhitelistManagement()
        {
            // Setup the IP input validation
            txtIPAddress.TextChanged += (s, e) => ValidateIPAddress();

            // Add whitelist button handler
            btnAddWhitelist.Click += OnAddWhitelist;

            // Setup context menu for the grid
            var contextMenu = new ContextMenuStrip();
            var removeItem = new ToolStripMenuItem("Remove from Whitelist");
            var toggleItem = new ToolStripMenuItem("Enable/Disable");

            removeItem.Click += OnRemoveFromWhitelist;
            toggleItem.Click += OnToggleWhitelistStatus;

            contextMenu.Items.AddRange(new ToolStripItem[] { removeItem, toggleItem });
            gridIPList.ContextMenuStrip = contextMenu;
        }

        private void ValidateIPAddress()
        {
            string ip = txtIPAddress.Text.Trim();
            bool isValid = IPAddress.TryParse(ip, out _);

            btnAddWhitelist.Enabled = isValid;
            txtIPAddress.BackColor = isValid ? Color.White : Color.MistyRose;
        }

        private void OnAddWhitelist(object sender, EventArgs e)
        {
            string ip = txtIPAddress.Text.Trim();

            // Check if IP is valid
            if (!IPAddress.TryParse(ip, out _))
            {
                MessageBox.Show("Please enter a valid IP address.", "Invalid IP",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if IP already exists in whitelist
            if (currentSettings.WhitelistedIPs.Any(w =>
                string.Equals(w.IPAddress, ip, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("This IP is already whitelisted.", "Duplicate IP",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Add to whitelist
            var entry = new IPEntry
            {
                IPAddress = ip,
                Type = "Whitelist",
                AddedDate = DateTime.Now,
                IsEnabled = true
            };

            currentSettings.WhitelistedIPs.Add(entry);

            // Add to grid
            gridIPList.Rows.Add(
                entry.IPAddress,
                entry.Type,
                entry.AddedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                "Enabled"
            );

            // If IP was banned, remove the ban
            monitorService.RemoveBan(ip);

            // Save settings immediately after adding
            SaveSettings();

            // Clear the input
            txtIPAddress.Clear();
            logger.LogInformation($"IP {ip} added to whitelist");
        }

        private void OnRemoveFromWhitelist(object sender, EventArgs e)
        {
            if (gridIPList.SelectedRows.Count > 0)
            {
                var row = gridIPList.SelectedRows[0];
                string ip = row.Cells["colIP"].Value.ToString() ?? "";

                var result = MessageBox.Show(
                    $"Are you sure you want to remove {ip} from the whitelist?",
                    "Confirm Remove",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    currentSettings.WhitelistedIPs.RemoveAll(w =>
                        string.Equals(w.IPAddress, ip, StringComparison.OrdinalIgnoreCase));
                    gridIPList.Rows.Remove(row);

                    // Save settings immediately after removing
                    SaveSettings();

                    logger.LogInformation($"IP {ip} removed from whitelist");
                }
            }
        }


        private void OnToggleWhitelistStatus(object? sender, EventArgs e)
        {
            if (gridIPList.SelectedRows.Count > 0)
            {
                var row = gridIPList.SelectedRows[0];
                string ip = row.Cells["colIP"].Value?.ToString() ?? "";
                bool currentlyEnabled = row.Cells["colStatus"].Value?.ToString() == "Enabled";

                if (string.IsNullOrEmpty(ip))
                    return;

                try
                {
                    // Update status in settings
                    var entry = currentSettings.WhitelistedIPs.First(w =>
                        string.Equals(w.IPAddress, ip, StringComparison.OrdinalIgnoreCase));

                    entry.IsEnabled = !currentlyEnabled;

                    // Update grid
                    row.Cells["colStatus"].Value = entry.IsEnabled ? "Enabled" : "Disabled";

                    // Save settings
                    SaveSettings();

                    logger.LogInformation($"IP {ip} whitelist status changed to: {(entry.IsEnabled ? "Enabled" : "Disabled")}");

                    string message = entry.IsEnabled ?
                        $"{ip} has been enabled in the whitelist." :
                        $"{ip} has been disabled in the whitelist and will be subject to normal protection rules.";

                    MessageBox.Show(
                        message,
                        "Status Updated",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    logger.LogError("Error toggling IP status", ex);
                    MessageBox.Show(
                        $"Error updating status: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Ensure right-click selects the row
            gridIPList.MouseClick += (s, args) =>
            {
                if (args.Button == MouseButtons.Right)
                {
                    var hit = gridIPList.HitTest(args.X, args.Y);
                    if (hit.RowIndex >= 0)
                    {
                        gridIPList.ClearSelection();
                        gridIPList.Rows[hit.RowIndex].Selected = true;
                    }
                }
            };
        }

        private void SaveSettings()
        {
            try
            {
                SettingsManager.SaveSettings(currentSettings);
                logger.LogInformation("Settings saved successfully");
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


        private void LoadSettingsToForm()
        {
            try
            {
                // Protection Settings
                nudMaxAttempts.Value = currentSettings.MaxAttempts;
                nudTimeWindow.Value = currentSettings.TimeWindow;
                nudPrivateIPBanHours.Value = currentSettings.PrivateIPBanHours;
                nudPublicIPBanDays.Value = currentSettings.PublicIPBanDays;
                
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
                
                // Save the settings
                SettingsManager.SaveSettings(currentSettings);

                // Notify the service of settings change
                monitorService.OnSettingsChanged();

                // Log success and close the form
                logger.LogInformation($"Settings saved successfully. MaxAttempts set to: {currentSettings.MaxAttempts}");
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
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (DialogResult == DialogResult.OK)
            {
                try
                {
                    SaveSettings();
                }
                catch (Exception ex)
                {
                    logger.LogError("Error saving settings on form close", ex);
                    MessageBox.Show(
                        $"Error saving settings: {ex.Message}",
                        "Settings Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    e.Cancel = true;  // Prevent closing if save failed
                }
            }
        }

        private void InitializeSystemTab()
        {
            // Create Service Management group box
            var groupBoxService = new GroupBox
            {
                Text = "Service Management",
                Location = new Point(20, 20),
                Size = new Size(520, 120),
                Parent = tabSystem
            };

            // Service status label
            var lblStatusText = new Label
            {
                Text = "Service Status:",
                Location = new Point(20, 30),
                AutoSize = true,
                Parent = groupBoxService
            };

            var lblStatus = new Label
            {
                Text = "Checking...",
                Location = new Point(110, 30),
                AutoSize = true,
                Parent = groupBoxService
            };

            // Install button
            var btnInstall = new Button
            {
                Text = "Install Service",
                Location = new Point(20, 60),
                Size = new Size(120, 30),
                Parent = groupBoxService
            };

            // Uninstall button
            var btnUninstall = new Button
            {
                Text = "Uninstall Service",
                Location = new Point(150, 60),
                Size = new Size(120, 30),
                Parent = groupBoxService
            };

            // Service control button
            var btnStartStop = new Button
            {
                Text = "Start Service",
                Location = new Point(280, 60),
                Size = new Size(120, 30),
                Parent = groupBoxService
            };

            // Add click handlers
            btnInstall.Click += async (s, e) => await InstallServiceAsync();
            btnUninstall.Click += async (s, e) => await UninstallServiceAsync();
            btnStartStop.Click += async (s, e) => await ToggleServiceAsync();

            // Timer to update service status
            var statusTimer = new System.Windows.Forms.Timer
            {
                Interval = 2000  // Check every 2 seconds
            };

            statusTimer.Tick += (s, e) => UpdateServiceStatus(lblStatus, btnStartStop, btnInstall, btnUninstall);
            statusTimer.Start();

            // Initial status check
            UpdateServiceStatus(lblStatus, btnStartStop, btnInstall, btnUninstall);
        }

        private void UpdateServiceStatus(Label statusLabel, Button startStopButton, Button installButton, Button uninstallButton)
        {
            try
            {
                using var controller = new ServiceController("RDPSecure");
                var status = controller.Status;

                // Update status label
                statusLabel.Text = status.ToString();
                statusLabel.ForeColor = status == ServiceControllerStatus.Running ? Color.Green : Color.Red;

                // Update buttons
                startStopButton.Enabled = true;
                startStopButton.Text = status == ServiceControllerStatus.Running ? "Stop Service" : "Start Service";

                installButton.Enabled = false;
                uninstallButton.Enabled = true;
            }
            catch
            {
                // Service doesn't exist
                statusLabel.Text = "Not Installed";
                statusLabel.ForeColor = Color.Red;
                startStopButton.Enabled = false;
                installButton.Enabled = true;
                uninstallButton.Enabled = false;
            }
        }
        private async Task InstallServiceAsync()
        {
            if (!Program.IsAdministrator())
            {
                MessageBox.Show("Administrator privileges are required to install the service.",
                    "Administrator Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Program.InstallService();
                MessageBox.Show("Service installed successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error installing service: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async Task UninstallServiceAsync()
        {
            if (!Program.IsAdministrator())
            {
                MessageBox.Show("Administrator privileges are required to uninstall the service.",
                    "Administrator Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Program.UninstallService();
                MessageBox.Show("Service uninstalled successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error uninstalling service: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ToggleServiceAsync()
        {
            if (!Program.IsAdministrator())
            {
                MessageBox.Show("Administrator privileges are required to control the service.",
                    "Administrator Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var controller = new ServiceController("RDPSecure");

                if (controller.Status == ServiceControllerStatus.Running)
                {
                    controller.Stop();
                    await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30)));
                }
                else if (controller.Status == ServiceControllerStatus.Stopped)
                {
                    controller.Start();
                    await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30)));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error controlling service: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsAdministrator()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
    }
}