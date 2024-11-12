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
        private int _lastSelectedIndex = -1;

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
                Interval = 20000  // Change to 20 seconds instead of 5
            };
            _refreshTimer.Tick += (s, e) => RefreshAttempts();
            _refreshTimer.Start();
        }

        private void SetupGrid()
        {
            gridAttempts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridAttempts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridAttempts.MultiSelect = false;
            gridAttempts.ReadOnly = true;
            gridAttempts.AllowUserToAddRows = false;
            gridAttempts.AllowUserToDeleteRows = false;
            gridAttempts.RowHeadersVisible = false;
            gridAttempts.AllowUserToResizeColumns = false;
            gridAttempts.AllowUserToResizeRows = false;

            // Handle selection change
            gridAttempts.SelectionChanged += (s, e) =>
            {
                if (gridAttempts.SelectedRows.Count > 0)
                {
                    _lastSelectedIndex = gridAttempts.SelectedRows[0].Index;
                }
            };

            RefreshAttempts();

            // Subscribe to new attempts
            _monitorService.LoginAttemptDetected += (s, e) =>
            {
                BeginInvoke(new Action(() => AddAttemptToGrid(e.IPAddress, e.Timestamp)));
            };
        }

        private void RefreshAttempts(bool maintainPosition = false)
        {
            try
            {
                // Store current position if needed
                int firstDisplayedRow = -1;
                int selectedRow = -1;
                if (maintainPosition && gridAttempts.Rows.Count > 0)
                {
                    firstDisplayedRow = gridAttempts.FirstDisplayedScrollingRowIndex;
                    selectedRow = gridAttempts.SelectedRows.Count > 0 ?
                        gridAttempts.SelectedRows[0].Index : -1;
                }

                var attempts = _monitorService.GetRecentAttempts();

                // Only refresh if there are changes
                if (attempts.Count != gridAttempts.Rows.Count)
                {
                    gridAttempts.SuspendLayout();
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

                    // Restore position if needed
                    if (maintainPosition && firstDisplayedRow >= 0 && firstDisplayedRow < gridAttempts.Rows.Count)
                    {
                        gridAttempts.FirstDisplayedScrollingRowIndex = firstDisplayedRow;
                        if (selectedRow >= 0 && selectedRow < gridAttempts.Rows.Count)
                        {
                            gridAttempts.Rows[selectedRow].Selected = true;
                        }
                    }

                    gridAttempts.ResumeLayout();
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

                int firstDisplayed = gridAttempts.FirstDisplayedScrollingRowIndex;

                gridAttempts.SuspendLayout();
                gridAttempts.Rows.Insert(0,
                    ipAddress,
                    timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    status
                );

                // Maintain scroll position if user has scrolled down
                if (firstDisplayed > 0)
                {
                    gridAttempts.FirstDisplayedScrollingRowIndex = firstDisplayed + 1;
                }
                gridAttempts.ResumeLayout();
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