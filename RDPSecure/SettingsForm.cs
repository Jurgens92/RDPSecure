using RDPSecure.Logging;
using RDPSecure.Services;
using System.Net;

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
            this.monitorService = monitorService;
            logger = new SecurityLogger();
            currentSettings = SettingsManager.LoadSettings();

            SetupIPManagement();
            LoadSettingsToForm();
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
    }
}