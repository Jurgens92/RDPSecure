using System.Collections.Concurrent;
using System.ComponentModel;
using RDPSecure.Logging;
using RDPSecure.Services;

namespace RDPSecure
{
    public partial class RecentAttemptsForm : Form
    {
        private readonly RDPMonitorService _monitorService;
        private readonly SecurityLogger _logger;
        private System.Windows.Forms.Timer _refreshTimer;
        private readonly BindingSource _bindingSource;
        private readonly ConcurrentQueue<Action> _updateQueue;
        private readonly object _gridLock = new object();
        private bool _isUpdating;
        private DataGridView _gridAttempts;  
          
        private const int MAX_DISPLAYED_ATTEMPTS = 1000;

        public RecentAttemptsForm(RDPMonitorService monitorService)
        {
            _monitorService = monitorService ?? throw new ArgumentNullException(nameof(monitorService));
            _logger = new SecurityLogger();
            _updateQueue = new ConcurrentQueue<Action>();
            _bindingSource = new BindingSource();

            _gridAttempts = new DataGridView();
            _refreshTimer = new System.Windows.Forms.Timer();

            InitializeComponent();
            InitializeGrid();
            SetupTimer();
            LoadInitialData();

            // Subscribe to events
            _monitorService.LoginAttemptDetected += OnLoginAttemptDetected;
        }

        private void InitializeComponent()
        {  

            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            _gridAttempts = new DataGridView();
            ((ISupportInitialize)_gridAttempts).BeginInit();
            SuspendLayout();
            // 
            // _gridAttempts
            // 
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(240, 240, 240);
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = Color.FromArgb(64, 64, 64);
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            _gridAttempts.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            _gridAttempts.ColumnHeadersHeight = 40;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = SystemColors.Window;
            dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(52, 152, 219);
            dataGridViewCellStyle2.SelectionForeColor = Color.White;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            _gridAttempts.DefaultCellStyle = dataGridViewCellStyle2;
            _gridAttempts.Location = new Point(0, 0);
            _gridAttempts.Name = "_gridAttempts";
            _gridAttempts.RowTemplate.Height = 30;
            _gridAttempts.Size = new Size(240, 150);
            _gridAttempts.TabIndex = 0;
            // 
            // RecentAttemptsForm
            // 
            BackColor = Color.White;            
            Controls.Add(_gridAttempts);                      
            Name = "RecentAttemptsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Recent Login Attempts";
            ((ISupportInitialize)_gridAttempts).EndInit();
            ResumeLayout(false);            
            Size = new Size(584, 461);             
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ;
        }

        private void InitializeGrid()
        {
            _gridAttempts.DataSource = _bindingSource;

            // Configure columns

            _gridAttempts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;       
            _gridAttempts.ReadOnly = true;
            _gridAttempts.EditMode = DataGridViewEditMode.EditProgrammatically;
            _gridAttempts.ShowEditingIcon = false;
            _gridAttempts.AllowUserToAddRows = false;
            _gridAttempts.AllowUserToDeleteRows = false;
            _gridAttempts.AllowUserToResizeColumns = false;
            _gridAttempts.AutoResizeColumns(); 
            _gridAttempts.Columns.Clear();

            _gridAttempts.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn
                {
                    Name = "IPAddress",
                    DataPropertyName = "IPAddress",
                    HeaderText = "IP Address",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    FillWeight = 30,
                    MinimumWidth = 150
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "Timestamp",
                    DataPropertyName = "Timestamp",
                    HeaderText = "Time",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    MinimumWidth = 150,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "yyyy-MM-dd HH:mm:ss"
                    }
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "Status",
                    DataPropertyName = "Status",
                    HeaderText = "Status",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    MinimumWidth = 100
                }
            });

            _gridAttempts.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(64, 64, 64),
                SelectionBackColor = SystemColors.Highlight,
                SelectionForeColor = SystemColors.HighlightText,
                Padding = new Padding(10, 0, 0, 0)
            };

            _gridAttempts.Columns["IPAddress"].Width = 200;
            _gridAttempts.Columns["Timestamp"].Width = 250;
            _gridAttempts.Columns["Status"].Width = 150;
            _gridAttempts.AllowUserToOrderColumns = false;
            _gridAttempts.AllowUserToResizeColumns = false;
            _gridAttempts.AllowUserToResizeRows = false;
            _gridAttempts.RowHeadersVisible = false;
            

            _gridAttempts.Dock = DockStyle.Fill;

            // Additional styling
            _gridAttempts.ColumnHeadersHeight = 40;
            _gridAttempts.RowTemplate.Height = 30;

            // Add resize handler
            _gridAttempts.SizeChanged += (s, e) => _gridAttempts.AutoResizeColumns();

            // Style status column based on value
            _gridAttempts.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex == 2 &&
                    e.Value is not null &&
                    e.CellStyle is DataGridViewCellStyle cellStyle)  // Pattern matching
                {
                    var status = e.Value.ToString() ?? string.Empty;
                    switch (status)
                    {
                        case "Banned":
                            cellStyle.ForeColor = Color.Red;
                            break;
                        case "Whitelisted":
                            cellStyle.ForeColor = Color.Green;
                            break;
                        default:
                            cellStyle.ForeColor = Color.Black;
                            break;
                    }
                }
            };
        }

        private void SetupTimer()
        {
            _refreshTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000  // Refresh every 5 seconds
            };
            _refreshTimer.Tick += (s, e) => ProcessUpdateQueue();
            _refreshTimer.Start();
        }

        private void LoadInitialData()
        {
            try
            {
                RefreshAttempts();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error loading initial data", ex);
                MessageBox.Show(
                    "Error loading attempt data. Please try closing and reopening this window.",
                    "Data Loading Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void OnLoginAttemptDetected(object? sender, LoginAttemptEventArgs e)
        {
            if (IsDisposed) return;

            _updateQueue.Enqueue(() => AddAttemptToGrid(e.IPAddress, e.Timestamp));

            if (InvokeRequired)
                BeginInvoke(ProcessUpdateQueue);
            else
                ProcessUpdateQueue();
        }

        private void ProcessUpdateQueue()
        {
            if (_isUpdating || IsDisposed) return;

            lock (_gridLock)
            {
                try
                {
                    _isUpdating = true;
                    _gridAttempts.SuspendLayout();

                    while (_updateQueue.TryDequeue(out var update))
                    {
                        update.Invoke();
                    }

                    var currentPosition = _gridAttempts.FirstDisplayedScrollingRowIndex;
                    RefreshAttempts(maintainPosition: true);

                    if (currentPosition >= 0 && currentPosition < _gridAttempts.RowCount)
                    {
                        _gridAttempts.FirstDisplayedScrollingRowIndex = currentPosition;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error processing update queue", ex);
                }
                finally
                {
                    _gridAttempts.ResumeLayout();
                    _isUpdating = false;
                }
            }
        }

        private void RefreshAttempts(bool maintainPosition = false)
        {
            try
            {
                var attempts = _monitorService.GetRecentAttempts()
                    .Select(a => new AttemptViewModel
                    {
                        IPAddress = a.IPAddress,
                        Timestamp = a.Timestamp,
                        Status = GetAttemptStatus(a.IPAddress)
                    })
                    .OrderByDescending(a => a.Timestamp)
                    .Take(MAX_DISPLAYED_ATTEMPTS)
                    .ToList();

                var currentPosition = maintainPosition ? _gridAttempts.FirstDisplayedScrollingRowIndex : -1;
                _bindingSource.DataSource = attempts;

                if (maintainPosition && currentPosition >= 0 && currentPosition < _gridAttempts.RowCount)
                {
                    _gridAttempts.FirstDisplayedScrollingRowIndex = currentPosition;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error refreshing attempts grid", ex);
            }
        }

        private string GetAttemptStatus(string ipAddress)
        {
            if (_monitorService.IsWhitelisted(ipAddress))
                return "Whitelisted";
            if (_monitorService.IsIPBanned(ipAddress))
                return "Banned";
            return "Normal";
        }

        private void AddAttemptToGrid(string ipAddress, DateTime timestamp)
        {
            try
            {
                var attempt = new AttemptViewModel
                {
                    IPAddress = ipAddress,
                    Timestamp = timestamp,
                    Status = GetAttemptStatus(ipAddress)
                };

                var attempts = (_bindingSource.DataSource as List<AttemptViewModel>)?.ToList()
                             ?? new List<AttemptViewModel>();

                attempts.Insert(0, attempt);

                if (attempts.Count > MAX_DISPLAYED_ATTEMPTS)
                {
                    attempts = attempts.Take(MAX_DISPLAYED_ATTEMPTS).ToList();
                }

                _bindingSource.DataSource = new BindingList<AttemptViewModel>(attempts);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error adding attempt to grid", ex);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
                _monitorService.LoginAttemptDetected -= OnLoginAttemptDetected;

                base.OnFormClosing(e);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during form closing", ex);
            }
        }

        private class AttemptViewModel
        {
            public string IPAddress { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
            public string Status { get; set; } = string.Empty;
        }
    }
}