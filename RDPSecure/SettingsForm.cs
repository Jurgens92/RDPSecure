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
        private System.Windows.Forms.Timer? firewallTimer;

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
            // Eevent handler for real-time validation
            txtIPAddress.TextChanged += (s, e) => ValidateIPAddress();

            // Initialize tooltip
            if (toolTip1 == null)
            {
                toolTip1 = new ToolTip
                {
                    InitialDelay = 0,
                    ReshowDelay = 0,
                    ShowAlways = true
                };
            }

            // Setup initial button states
            btnAddWhitelist.Enabled = false;
            btnAddBlacklist.Enabled = false;

            // Button click handlers
            btnAddWhitelist.Click += OnAddWhitelist;
            btnAddBlacklist.Click += OnAddBlacklist;

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

        private void OnAddBlacklist(object? sender, EventArgs e)
        {
            string ip = txtIPAddress.Text.Trim();
            var validation = IPValidator.ValidateIP(ip);

            if (!validation.IsValid)
            {
                MessageBox.Show(
                    validation.ErrorMessage,
                    "Invalid IP Address",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!IPAddress.TryParse(ip, out IPAddress? parsedIP))
            {
                MessageBox.Show(
                    "Failed to parse IP address.",
                    "Invalid IP Address",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (IPValidator.IsReservedIP(parsedIP))
            {
                MessageBox.Show(
                    "Reserved IP addresses cannot be blacklisted.",
                    "Invalid IP Address",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
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

            // Add to blacklist
            var entry = new IPEntry
            {
                IPAddress = ip,
                Type = "Blacklist",
                AddedDate = DateTime.Now,
                IsEnabled = true,
                Notes = string.Empty
            };

            currentSettings.BlacklistedIPs.Add(entry);

            // Add to banned IPs in monitor service
            monitorService.AddManualBan(ip, TimeSpan.FromDays(36500)); // Duration for manual blacklist
                        
            SaveSettings();
                        
            txtIPAddress.Clear();

            MessageBox.Show(
                $"IP {ip} has been successfully blacklisted.",
                "IP Blacklisted",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            logger.LogInformation($"IP {ip} manually added to blacklist");
        }

        private void SetupWhitelistManagement()
        {
            // IP input validation
            txtIPAddress.TextChanged += (s, e) => ValidateIPAddress();

            // Whitelist button handler
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
            var validation = IPValidator.ValidateIP(ip);

            if (string.IsNullOrWhiteSpace(ip))
            {
                // Empty input - disable buttons but don't show error
                btnAddWhitelist.Enabled = false;
                btnAddBlacklist.Enabled = false;
                txtIPAddress.BackColor = SystemColors.Window;
                toolTip1.SetToolTip(txtIPAddress, "Enter an IP address");
                return;
            }

            if (!validation.IsValid)
            {
                btnAddWhitelist.Enabled = false;
                btnAddBlacklist.Enabled = false;
                txtIPAddress.BackColor = Color.MistyRose;
                toolTip1.SetToolTip(txtIPAddress, validation.ErrorMessage);
                return;
            }

            // Create an IPAddress instance for reserved check
            if (!IPAddress.TryParse(ip, out IPAddress? parsedIP))
            {
                // This shouldn't happen since we already validated, but handle it just in case
                btnAddWhitelist.Enabled = false;
                btnAddBlacklist.Enabled = false;
                txtIPAddress.BackColor = Color.MistyRose;
                toolTip1.SetToolTip(txtIPAddress, "Invalid IP address format");
                return;
            }

            // Check for reserved IPs
            if (IPValidator.IsReservedIP(parsedIP))
            {
                btnAddWhitelist.Enabled = false;
                btnAddBlacklist.Enabled = false;
                txtIPAddress.BackColor = Color.MistyRose;
                toolTip1.SetToolTip(txtIPAddress, "Reserved IP addresses cannot be added");
                return;
            }

            // Valid IP
            btnAddWhitelist.Enabled = true;
            btnAddBlacklist.Enabled = true;
            txtIPAddress.BackColor = Color.LightGreen;
            toolTip1.SetToolTip(txtIPAddress, $"Valid {validation.Version} address");
        }

        private void OnAddWhitelist(object? sender, EventArgs e)
        {
            string ip = txtIPAddress.Text.Trim();
            var validation = IPValidator.ValidateIP(ip);

            if (!validation.IsValid)
            {
                MessageBox.Show(
                    validation.ErrorMessage,
                    "Invalid IP Address",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!IPAddress.TryParse(ip, out IPAddress? parsedIP))
            {
                MessageBox.Show(
                    "Failed to parse IP address.",
                    "Invalid IP Address",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (IPValidator.IsReservedIP(parsedIP))
            {
                MessageBox.Show(
                    "Reserved IP addresses cannot be added to the whitelist.",
                    "Invalid IP Address",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Check if IP already exists in whitelist
            if (currentSettings.WhitelistedIPs.Any(w =>
                string.Equals(w.IPAddress, ip, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(
                    "This IP is already whitelisted.",
                    "Duplicate IP",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                // Add to whitelist
                var entry = new IPEntry
                {
                    IPAddress = IPValidator.NormalizeIP(ip),
                    Type = "Whitelist",
                    AddedDate = DateTime.Now,
                    IsEnabled = true,
                    Notes = $"Added manually ({validation.Version})"
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

                // Clear the input and reset validation
                txtIPAddress.Clear();
                txtIPAddress.BackColor = SystemColors.Window;
                btnAddWhitelist.Enabled = false;
                btnAddBlacklist.Enabled = false;

                logger.LogInformation($"{validation.Version} address {ip} added to whitelist");

                // Show success message
                MessageBox.Show(
                    $"{validation.Version} address {ip} has been successfully added to the whitelist.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Refresh the main form's whitelist count
                if (Owner is MainForm mainForm)
                {
                    mainForm.BeginInvoke(() => mainForm.UpdateUIFromSettings());
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error adding IP to whitelist: {ex.Message}", ex);
                MessageBox.Show(
                    $"Error adding IP to whitelist: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void OnRemoveFromWhitelist(object? sender, EventArgs e)
        {
            if (gridIPList.SelectedRows.Count > 0)
            {
                var row = gridIPList.SelectedRows[0];
                string ip = row.Cells["colIP"].Value?.ToString() ?? "";

                var result = MessageBox.Show(
                    $"Are you sure you want to remove {ip} from the whitelist?",
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

                        // Force main form to refresh
                        if (Owner is MainForm mainForm)
                        {
                            mainForm.Invoke(new Action(() => mainForm.RefreshWhitelistCount()));
                        }

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

                // Force main form to refresh immediately after save
                if (Owner is MainForm mainForm)
                {
                    mainForm.Invoke(new Action(() => {
                        mainForm.RefreshWhitelistCount();
                        logger.LogInformation("Main form whitelist count refreshed");
                    }));
                }
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

        private void btnOK_Click(object? sender, EventArgs e)
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

        public void SelectIPManagementTab()
        {
            tabControlSettings.SelectedTab = tabIPManagement;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // Stop and dispose of the firewall timer
            if (firewallTimer != null)
            {
                firewallTimer.Stop();
                firewallTimer.Dispose();
            }

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

            var groupBoxFirewall = new GroupBox
            {
                Text = "Firewall Status",
                Location = new Point(20, 160), // Position below service management
                Size = new Size(520, 100),
                Parent = tabSystem
            };

            // Firewall status label
            var lblFirewallStatusText = new Label
            {
                Text = "Windows Firewall:",
                Location = new Point(20, 30),
                AutoSize = true,
                Parent = groupBoxFirewall
            };

            var lblFirewallStatus = new Label
            {
                Location = new Point(130, 30),
                AutoSize = true,
                Parent = groupBoxFirewall
            };

            // Enable firewall button
            var btnEnableFirewall = new Button
            {
                Text = "Enable Firewall",
                Location = new Point(20, 60),
                Size = new Size(120, 30),
                Visible = false,
                Parent = groupBoxFirewall
            };
            
            // Add click handler for the enable button
            btnEnableFirewall.Click += async (s, e) =>
            {
                try
                {
                    var result = MessageBox.Show(
                        "Windows Firewall is currently disabled. Would you like to enable it?\n\n" +
                        "Note: This requires administrator privileges.",
                        "Enable Windows Firewall",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        await Task.Run(() =>
                        {
                            EnableWindowsFirewall();
                            this.BeginInvoke(() => UpdateFirewallStatus(lblFirewallStatus, btnEnableFirewall));
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Error enabling firewall: " + ex.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };

            // Timer to update firewall status
            firewallTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000  // Check every 5 seconds
            };

            firewallTimer.Tick += (s, e) => UpdateFirewallStatus(lblFirewallStatus, btnEnableFirewall);
            firewallTimer.Start();

            // Initial status check
            UpdateFirewallStatus(lblFirewallStatus, btnEnableFirewall);

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

        private void UpdateFirewallStatus(Label statusLabel, Button enableButton)
        {
            bool isEnabled = IsWindowsFirewallEnabled();

            statusLabel.Text = isEnabled ? "Enabled" : "Disabled";
            statusLabel.ForeColor = isEnabled ? Color.Green : Color.Red;
            enableButton.Visible = !isEnabled;

            // Show notification if firewall is disabled
            if (!isEnabled && WindowState == FormWindowState.Normal)
            {
                MessageBox.Show(
                    "Windows Firewall is currently disabled. It is recommended to enable the firewall for proper protection.",
                    "Firewall Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            var groupBoxAudit = new GroupBox
            {
                Text = "Audit Policy Status",
                Location = new Point(20, 280), // Position below firewall group
                Size = new Size(520, 120),
                Parent = tabSystem
            };

            // Logon Audit Status
            var lblLogonAuditText = new Label
            {
                Text = "Logon Auditing:",
                Location = new Point(20, 30),
                AutoSize = true,
                Parent = groupBoxAudit
            };

            var lblLogonAudit = new Label
            {
                Location = new Point(130, 30),
                AutoSize = true,
                Parent = groupBoxAudit
            };

            // Other Logon Events Status
            var lblOtherLogonText = new Label
            {
                Text = "Other Logon Events:",
                Location = new Point(20, 55),
                AutoSize = true,
                Parent = groupBoxAudit
            };

            var lblOtherLogon = new Label
            {
                Location = new Point(130, 55),
                AutoSize = true,
                Parent = groupBoxAudit
            };

            // Enable button
            var btnEnableAudit = new Button
            {
                Text = "Enable Auditing",
                Location = new Point(20, 85),
                Size = new Size(120, 23),
                Parent = groupBoxAudit
            };

            btnEnableAudit.Click += async (s, e) =>
            {
                try
                {
                    if (!Program.IsAdministrator())
                    {
                        MessageBox.Show(
                            "Administrator privileges are required to modify audit policies.",
                            "Administrator Required",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    var result = MessageBox.Show(
                        "Do you want to enable the required audit policies?\n\n" +
                        "This will enable success and failure auditing for:\n" +
                        "- Logon Events\n" +
                        "- Other Logon/Logoff Events",
                        "Enable Audit Policies",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        Cursor = Cursors.WaitCursor;
                        btnEnableAudit.Enabled = false;

                        var auditManager = new AuditPolicyManager(logger);
                        bool success = await Task.Run(() => auditManager.EnableAuditPolicies());

                        if (success)
                        {
                            MessageBox.Show(
                                "Audit policies have been successfully enabled.",
                                "Success",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                            // Refresh status
                            UpdateAuditStatus(lblLogonAudit, lblOtherLogon, btnEnableAudit);
                        }
                        else
                        {
                            MessageBox.Show(
                                "Failed to enable audit policies. Please check the logs for details.",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error enabling audit policies: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                finally
                {
                    Cursor = Cursors.Default;
                    btnEnableAudit.Enabled = true;
                }
            };

            // Timer to update audit status
            var auditTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000  // Check every 5 seconds
            };

            auditTimer.Tick += (s, e) => UpdateAuditStatus(lblLogonAudit, lblOtherLogon, btnEnableAudit);
            auditTimer.Start();

            // Initial status check
            UpdateAuditStatus(lblLogonAudit, lblOtherLogon, btnEnableAudit);

        }

        private void UpdateAuditStatus(Label lblLogonAudit, Label lblOtherLogon, Button btnEnableAudit)
        {
            try
            {
                var auditManager = new AuditPolicyManager(logger);
                var status = auditManager.CheckAuditPolicies();

                // Update Logon status
                lblLogonAudit.Text = status.LogonStatus;
                lblLogonAudit.ForeColor = status.LogonEnabled ? Color.Green : Color.Red;

                // Update Other Logon status
                lblOtherLogon.Text = status.OtherLogonStatus;
                lblOtherLogon.ForeColor = status.OtherLogonEnabled ? Color.Green : Color.Red;

                // Update button state
                btnEnableAudit.Enabled = !(status.LogonEnabled && status.OtherLogonEnabled);

                // Show warning if policies are not properly configured
                if (!status.LogonEnabled || !status.OtherLogonEnabled)
                {
                    if (WindowState == FormWindowState.Normal)
                    {
                        toolTip1.SetToolTip(btnEnableAudit,
                            "Required audit policies are not properly configured.\n" +
                            "Click to enable required policies.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error updating audit status", ex);
                lblLogonAudit.Text = "Error";
                lblOtherLogon.Text = "Error";
                lblLogonAudit.ForeColor = Color.Red;
                lblOtherLogon.ForeColor = Color.Red;
            }
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
                await Task.Run(() => Program.InstallService());
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
                await Task.Run(() => Program.UninstallService());
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
                await Task.Run(() =>
                {
                    using var controller = new ServiceController("RDPSecure");
                    if (controller.Status == ServiceControllerStatus.Running)
                    {
                        controller.Stop();
                        controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    }
                    else if (controller.Status == ServiceControllerStatus.Stopped)
                    {
                        controller.Start();
                        controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error controlling service: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsWindowsFirewallEnabled()
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "advfirewall show allprofiles",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Check if any profile is disabled
                return !output.Contains("State                                 OFF");
            }
            catch (Exception ex)
            {
                logger.LogError("Error checking firewall status", ex);
                return false;
            }
        }

        private void EnableWindowsFirewall()
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "advfirewall set allprofiles state on",
                        UseShellExecute = true,
                        Verb = "runas" // This will prompt for admin rights
                    }
                };

                process.Start();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                logger.LogError("Error enabling firewall", ex);
                throw;
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