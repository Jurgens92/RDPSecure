using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RDPSecure
{
    public partial class SettingsForm : Form
    {
        private AppSettings currentSettings;


        public SettingsForm()
        {
            InitializeComponent();
            SetupIPManagement();
            InitializeSettings();
            currentSettings = SettingsManager.LoadSettings();
            LoadSettingsToForm();
        }
        //x---------------------------------------------------------------------------------------------------------

        private void LoadSettingsToForm()
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

        private void SaveSettingsFromForm()
        {
            // Protection Settings
            currentSettings.MaxAttempts = (int)nudMaxAttempts.Value;
            currentSettings.TimeWindow = (int)nudTimeWindow.Value;
            currentSettings.PrivateIPBanHours = (int)nudPrivateIPBanHours.Value;
            currentSettings.PublicIPBanDays = (int)nudPublicIPBanDays.Value;
            currentSettings.BurstProtectionEnabled = chkBurstProtection.Checked;

            // Save IP List
            currentSettings.WhitelistedIPs.Clear();
            foreach (DataGridViewRow row in gridIPList.Rows)
            {
                currentSettings.WhitelistedIPs.Add(new IPEntry
                {
                    IPAddress = row.Cells["colIP"].Value?.ToString() ?? "",
                    Type = row.Cells["colType"].Value?.ToString() ?? "Whitelist",
                    AddedDate = DateTime.Parse(row.Cells["colAddedDate"].Value?.ToString() ?? DateTime.Now.ToString()),
                    IsEnabled = row.Cells["colStatus"].Value?.ToString() == "Enabled"
                });
            }

            SettingsManager.SaveSettings(currentSettings);
        }


        private void btnOK_Click(object sender, EventArgs e)
        {
            SaveSettingsFromForm();
            DialogResult = DialogResult.OK;
            Close();
        }



        //x------------------------------------------------------------------------------------------------------------










        //------------------------------------------------------------------------------------------------------------------
        private void InitializeSettings()
        {
            // Set default values
            SetDefaultValues();
            SetupEventHandlers();
        }

        private void SetDefaultValues()
        {
            // Failed Login Attempts
            nudMaxAttempts.Value = 3;
            nudTimeWindow.Value = 5;

            // Ban Duration Settings
            nudPrivateIPBanHours.Value = 1;
            nudPublicIPBanDays.Value = 30;
            chkBurstProtection.Checked = true;
        }

        private void SetupEventHandlers()
        {
            // Add value changed handlers
            nudMaxAttempts.ValueChanged += Settings_ValueChanged;
            nudTimeWindow.ValueChanged += Settings_ValueChanged;
            nudPrivateIPBanHours.ValueChanged += Settings_ValueChanged;
            nudPublicIPBanDays.ValueChanged += Settings_ValueChanged;
            chkBurstProtection.CheckedChanged += Settings_ValueChanged;
        }

        private void Settings_ValueChanged(object sender, EventArgs e)
        {
            // Here we'll add code to handle settings changes
            // For now, we'll just validate the values
            if (sender is NumericUpDown nud)
            {
                if (nud.Value < nud.Minimum) nud.Value = nud.Minimum;
                if (nud.Value > nud.Maximum) nud.Value = nud.Maximum;
            }
        }

        public int MaxAttempts => (int)nudMaxAttempts.Value;
        public int TimeWindow => (int)nudTimeWindow.Value;
        public int PrivateIPBanHours => (int)nudPrivateIPBanHours.Value;
        public int PublicIPBanDays => (int)nudPublicIPBanDays.Value;
        public bool BurstProtectionEnabled => chkBurstProtection.Checked;





        //------------------------------------------------------------------------------------------------
        private void SetupIPManagement()
        {
            // Add button click handler
            btnAddWhitelist.Click += OnAddWhitelist;

            // Grid context menu handlers
            menuItemRemoveIP.Click += OnRemoveIP;
            menuItemToggleStatus.Click += OnToggleIPStatus;

            // Ip validation
            txtIPAddress.TextChanged += (s, e) => ValidateIPTextBox(txtIPAddress);
            txtIPAddress.PlaceholderText = "Enter IP Address (e.g., 192.168.1.1)";
            var toolTip = new ToolTip();
            toolTip.SetToolTip(txtIPAddress, "Enter a valid IPv4 address");



            //sample data
            AddSampleIPData();
        }

        private void OnAddWhitelist(object sender, EventArgs e)
        {
            {
                string ip = txtIPAddress.Text.Trim();

                if (!IsValidIP(ip))
                {
                    MessageBox.Show(
                        "Please enter a valid IPv4 address.",
                        "Invalid IP Address",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    txtIPAddress.Focus();
                    return;
                }

                // Check if IP already exists
                foreach (DataGridViewRow row in gridIPList.Rows)
                {
                    if (row.Cells["colIP"].Value?.ToString() == ip)
                    {
                        MessageBox.Show(
                            "This IP address is already in the list.",
                            "Duplicate IP",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                        return;
                    }
                }

                // Add the IP to the grid
                gridIPList.Rows.Add(
                    ip,
                    "Whitelist",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    "Enabled"
                );

                txtIPAddress.Clear();
                txtIPAddress.Focus();
            }
        }


        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Add enter key handler for IP textbox
            txtIPAddress.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    e.Handled = true;
                    if (btnAddWhitelist.Enabled)
                    {
                        OnAddWhitelist(s, EventArgs.Empty);
                    }
                }
            };
        }


        //Remove IP address
        private void OnRemoveIP(object sender, EventArgs e)
        {
            if (gridIPList.SelectedRows.Count > 0)
            {
                gridIPList.Rows.RemoveAt(gridIPList.SelectedRows[0].Index);
            }
        }

        private void OnToggleIPStatus(object sender, EventArgs e)
        {
            if (gridIPList.SelectedRows.Count > 0)
            {
                var row = gridIPList.SelectedRows[0];
                row.Cells["colStatus"].Value =
                    row.Cells["colStatus"].Value.ToString() == "Enabled"
                    ? "Disabled"
                    : "Enabled";
            }
        }


        //Ip validation bool----------------------------------------------------------------------
        private bool IsValidIP(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return false;

            // Try to parse the IP address
            if (System.Net.IPAddress.TryParse(ipAddress, out System.Net.IPAddress? parsedIP))
            {
                // Check if it's IPv4
                return parsedIP.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
            }

            return false;
        }

        private void ValidateIPTextBox(TextBox textBox)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.BackColor = SystemColors.Window;
                return;
            }

            if (IsValidIP(textBox.Text))
            {
                textBox.BackColor = Color.FromArgb(223, 240, 216); // Light green for valid
                btnAddWhitelist.Enabled = true;
            }
            else
            {
                textBox.BackColor = Color.FromArgb(255, 233, 233); // Light red for invalid
                btnAddWhitelist.Enabled = false;
            }
        }
        //end of IP validation--------------------------------------------------------------------















        //sample data
        private void AddSampleIPData()
        {
            gridIPList.Rows.Add("192.168.1.100", "Whitelist",
                DateTime.Now.AddDays(-5).ToString("yyyy-MM-dd HH:mm:ss"), "Enabled");
            gridIPList.Rows.Add("10.0.0.50", "Whitelist",
                DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd HH:mm:ss"), "Disabled");
        }
    }
}







