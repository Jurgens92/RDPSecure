using RDPSecure.Logging;
using RDPSecure.Services;
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
    public partial class RecentAttemptsForm : Form
    {
        private readonly RDPMonitorService _monitorService;
        private readonly SecurityLogger _logger;
        private System.Windows.Forms.Timer _refreshTimer;
        private DataGridView gridAttempts;

        public RecentAttemptsForm(RDPMonitorService monitorService)
        {
            _monitorService = monitorService;
            _logger = new SecurityLogger();
            InitializeComponent();
            SetupGrid();
            SetupTimer();
        }

        private void InitializeComponent()
        {
            this.Text = "Recent Login Attempts";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            gridAttempts = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.WhiteSmoke
            };

            gridAttempts.Columns.Add("IPAddress", "IP Address");
            gridAttempts.Columns.Add("Timestamp", "Timestamp");
            gridAttempts.Columns.Add("Status", "Status");

            this.Controls.Add(gridAttempts);
        }

        private void SetupTimer()
        {
            _refreshTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000 // Refresh every 5 seconds
            };
            _refreshTimer.Tick += (s, e) => RefreshAttempts();
            _refreshTimer.Start();
        }

        private void SetupGrid()
        {
            RefreshAttempts();

            // Subscribe to new attempts
            _monitorService.LoginAttemptDetected += (s, e) =>
            {
                BeginInvoke(new Action(() => AddAttemptToGrid(e.IPAddress, e.Timestamp)));
            };
        }

        private void RefreshAttempts()
        {
            try
            {
                var attempts = _monitorService.GetRecentAttempts(); // You'll need to add this method
                gridAttempts.Rows.Clear();

                foreach (var attempt in attempts)
                {
                    string status = _monitorService.IsWhitelisted(attempt.IPAddress) ? "Whitelisted" :
                                   _monitorService.IsIPBanned(attempt.IPAddress) ? "Banned" : "Normal";

                    gridAttempts.Rows.Add(
                        attempt.IPAddress,
                        attempt.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        status
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error refreshing attempts grid", ex);
            }
        }

        private void AddAttemptToGrid(string ipAddress, DateTime timestamp)
        {
            try
            {
                string status = _monitorService.IsWhitelisted(ipAddress) ? "Whitelisted" :
                               _monitorService.IsIPBanned(ipAddress) ? "Banned" : "Normal";

                gridAttempts.Rows.Insert(0,
                    ipAddress,
                    timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    status
                );
            }
            catch (Exception ex)
            {
                _logger.LogError("Error adding attempt to grid", ex);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
        }
    }
}