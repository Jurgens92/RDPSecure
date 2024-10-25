namespace RDPSecure;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
        panelStats = new Panel();
        btnSettings = new Button();
        panelWhitelisted = new Panel();
        lblWhitelistedCount = new Label();
        lblWhitelistedTitle = new Label();
        panelRecentAttempts = new Panel();
        lblRecentAttemptsCount = new Label();
        lblRecentAttemptsTitle = new Label();
        panelActiveBans = new Panel();
        lblActiveBansCount = new Label();
        lblActiveBansTitle = new Label();
        toolTip1 = new ToolTip(components);
        panelGrid = new Panel();
        gridBannedIPs = new DataGridView();
        Column1 = new DataGridViewTextBoxColumn();
        Column2 = new DataGridViewTextBoxColumn();
        Column3 = new DataGridViewTextBoxColumn();
        Column4 = new DataGridViewTextBoxColumn();
        Column5 = new DataGridViewTextBoxColumn();
        notifyIconRDPSecure = new NotifyIcon(components);
        contextMenuTray = new ContextMenuStrip(components);
        toolStripMenuItem1 = new ToolStripMenuItem();
        toolStripMenuItem2 = new ToolStripMenuItem();
        settingsToolStripMenuItem = new ToolStripMenuItem();
        toolStripMenuItem3 = new ToolStripMenuItem();
        exitToolStripMenuItem = new ToolStripMenuItem();
        btnExit = new Button();
        panelStats.SuspendLayout();
        panelWhitelisted.SuspendLayout();
        panelRecentAttempts.SuspendLayout();
        panelActiveBans.SuspendLayout();
        panelGrid.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)gridBannedIPs).BeginInit();
        contextMenuTray.SuspendLayout();
        SuspendLayout();
        // 
        // panelStats
        // 
        panelStats.Controls.Add(btnExit);
        panelStats.Controls.Add(btnSettings);
        panelStats.Controls.Add(panelWhitelisted);
        panelStats.Controls.Add(panelRecentAttempts);
        panelStats.Controls.Add(panelActiveBans);
        panelStats.Dock = DockStyle.Top;
        panelStats.Location = new Point(0, 0);
        panelStats.Name = "panelStats";
        panelStats.Padding = new Padding(20);
        panelStats.Size = new Size(784, 120);
        panelStats.TabIndex = 0;
        // 
        // btnSettings
        // 
        btnSettings.BackColor = Color.FromArgb(52, 152, 219);
        btnSettings.Cursor = Cursors.Hand;
        btnSettings.FlatStyle = FlatStyle.Flat;
        btnSettings.ForeColor = Color.White;
        btnSettings.Location = new Point(661, 10);
        btnSettings.Name = "btnSettings";
        btnSettings.Size = new Size(100, 30);
        btnSettings.TabIndex = 3;
        btnSettings.Text = "Settings";
        toolTip1.SetToolTip(btnSettings, "Open Settings");
        btnSettings.UseVisualStyleBackColor = false;
        // 
        // panelWhitelisted
        // 
        panelWhitelisted.BackColor = Color.White;
        panelWhitelisted.BorderStyle = BorderStyle.FixedSingle;
        panelWhitelisted.Controls.Add(lblWhitelistedCount);
        panelWhitelisted.Controls.Add(lblWhitelistedTitle);
        panelWhitelisted.Location = new Point(392, 10);
        panelWhitelisted.Name = "panelWhitelisted";
        panelWhitelisted.Size = new Size(180, 100);
        panelWhitelisted.TabIndex = 2;
        // 
        // lblWhitelistedCount
        // 
        lblWhitelistedCount.AutoSize = true;
        lblWhitelistedCount.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
        lblWhitelistedCount.ForeColor = Color.Green;
        lblWhitelistedCount.Location = new Point(21, 45);
        lblWhitelistedCount.Name = "lblWhitelistedCount";
        lblWhitelistedCount.Size = new Size(33, 37);
        lblWhitelistedCount.TabIndex = 2;
        lblWhitelistedCount.Text = "0";
        // 
        // lblWhitelistedTitle
        // 
        lblWhitelistedTitle.AutoSize = true;
        lblWhitelistedTitle.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        lblWhitelistedTitle.Location = new Point(15, 15);
        lblWhitelistedTitle.Name = "lblWhitelistedTitle";
        lblWhitelistedTitle.Size = new Size(92, 17);
        lblWhitelistedTitle.TabIndex = 1;
        lblWhitelistedTitle.Text = "Whitelisted IPs";
        // 
        // panelRecentAttempts
        // 
        panelRecentAttempts.BackColor = Color.White;
        panelRecentAttempts.BorderStyle = BorderStyle.FixedSingle;
        panelRecentAttempts.Controls.Add(lblRecentAttemptsCount);
        panelRecentAttempts.Controls.Add(lblRecentAttemptsTitle);
        panelRecentAttempts.Location = new Point(206, 10);
        panelRecentAttempts.Name = "panelRecentAttempts";
        panelRecentAttempts.Size = new Size(180, 100);
        panelRecentAttempts.TabIndex = 1;
        // 
        // lblRecentAttemptsCount
        // 
        lblRecentAttemptsCount.AutoSize = true;
        lblRecentAttemptsCount.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
        lblRecentAttemptsCount.ForeColor = SystemColors.Highlight;
        lblRecentAttemptsCount.Location = new Point(15, 45);
        lblRecentAttemptsCount.Name = "lblRecentAttemptsCount";
        lblRecentAttemptsCount.Size = new Size(33, 37);
        lblRecentAttemptsCount.TabIndex = 1;
        lblRecentAttemptsCount.Text = "0";
        // 
        // lblRecentAttemptsTitle
        // 
        lblRecentAttemptsTitle.AutoSize = true;
        lblRecentAttemptsTitle.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        lblRecentAttemptsTitle.Location = new Point(15, 15);
        lblRecentAttemptsTitle.Name = "lblRecentAttemptsTitle";
        lblRecentAttemptsTitle.Size = new Size(136, 17);
        lblRecentAttemptsTitle.TabIndex = 0;
        lblRecentAttemptsTitle.Text = "Recent Attempts (24h)";
        // 
        // panelActiveBans
        // 
        panelActiveBans.BackColor = Color.White;
        panelActiveBans.BorderStyle = BorderStyle.FixedSingle;
        panelActiveBans.Controls.Add(lblActiveBansCount);
        panelActiveBans.Controls.Add(lblActiveBansTitle);
        panelActiveBans.Location = new Point(20, 10);
        panelActiveBans.Name = "panelActiveBans";
        panelActiveBans.Size = new Size(180, 100);
        panelActiveBans.TabIndex = 0;
        // 
        // lblActiveBansCount
        // 
        lblActiveBansCount.AutoSize = true;
        lblActiveBansCount.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
        lblActiveBansCount.ForeColor = Color.Red;
        lblActiveBansCount.Location = new Point(15, 45);
        lblActiveBansCount.Name = "lblActiveBansCount";
        lblActiveBansCount.Size = new Size(33, 37);
        lblActiveBansCount.TabIndex = 2;
        lblActiveBansCount.Text = "0";
        // 
        // lblActiveBansTitle
        // 
        lblActiveBansTitle.AutoSize = true;
        lblActiveBansTitle.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        lblActiveBansTitle.Location = new Point(15, 15);
        lblActiveBansTitle.Name = "lblActiveBansTitle";
        lblActiveBansTitle.Size = new Size(73, 17);
        lblActiveBansTitle.TabIndex = 1;
        lblActiveBansTitle.Text = "Active Bans";
        // 
        // panelGrid
        // 
        panelGrid.Controls.Add(gridBannedIPs);
        panelGrid.Dock = DockStyle.Fill;
        panelGrid.Location = new Point(0, 120);
        panelGrid.Name = "panelGrid";
        panelGrid.Padding = new Padding(20);
        panelGrid.Size = new Size(784, 441);
        panelGrid.TabIndex = 1;
        // 
        // gridBannedIPs
        // 
        gridBannedIPs.AllowUserToAddRows = false;
        gridBannedIPs.AllowUserToDeleteRows = false;
        gridBannedIPs.AllowUserToResizeColumns = false;
        gridBannedIPs.AllowUserToResizeRows = false;
        dataGridViewCellStyle1.BackColor = Color.FromArgb(249, 249, 249);
        gridBannedIPs.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
        gridBannedIPs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        gridBannedIPs.BackgroundColor = Color.FromArgb(224, 224, 224);
        gridBannedIPs.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle2.BackColor = Color.WhiteSmoke;
        dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle2.ForeColor = Color.FromArgb(64, 64, 64);
        dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle2.WrapMode = DataGridViewTriState.True;
        gridBannedIPs.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
        gridBannedIPs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        gridBannedIPs.Columns.AddRange(new DataGridViewColumn[] { Column1, Column2, Column3, Column4, Column5 });
        gridBannedIPs.Cursor = Cursors.Hand;
        dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle3.BackColor = Color.White;
        dataGridViewCellStyle3.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle3.ForeColor = Color.FromArgb(64, 64, 64);
        dataGridViewCellStyle3.SelectionBackColor = Color.FromArgb(52, 152, 219);
        dataGridViewCellStyle3.SelectionForeColor = Color.White;
        dataGridViewCellStyle3.WrapMode = DataGridViewTriState.False;
        gridBannedIPs.DefaultCellStyle = dataGridViewCellStyle3;
        gridBannedIPs.Dock = DockStyle.Fill;
        gridBannedIPs.EnableHeadersVisualStyles = false;
        gridBannedIPs.Location = new Point(20, 20);
        gridBannedIPs.MultiSelect = false;
        gridBannedIPs.Name = "gridBannedIPs";
        gridBannedIPs.ReadOnly = true;
        gridBannedIPs.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Sunken;
        gridBannedIPs.RowHeadersVisible = false;
        gridBannedIPs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gridBannedIPs.Size = new Size(744, 401);
        gridBannedIPs.TabIndex = 0;
        // 
        // Column1
        // 
        Column1.HeaderText = "IP Address";
        Column1.Name = "Column1";
        Column1.ReadOnly = true;
        // 
        // Column2
        // 
        Column2.HeaderText = "Location";
        Column2.Name = "Column2";
        Column2.ReadOnly = true;
        // 
        // Column3
        // 
        Column3.HeaderText = "Ban Duration";
        Column3.Name = "Column3";
        Column3.ReadOnly = true;
        // 
        // Column4
        // 
        Column4.HeaderText = "Attempts";
        Column4.Name = "Column4";
        Column4.ReadOnly = true;
        // 
        // Column5
        // 
        Column5.HeaderText = "Status";
        Column5.Name = "Column5";
        Column5.ReadOnly = true;
        // 
        // notifyIconRDPSecure
        // 
        notifyIconRDPSecure.Icon = (Icon)resources.GetObject("notifyIconRDPSecure.Icon");
        notifyIconRDPSecure.Text = "RDPSecure";
        notifyIconRDPSecure.Visible = true;
        // 
        // contextMenuTray
        // 
        contextMenuTray.Items.AddRange(new ToolStripItem[] { toolStripMenuItem1, toolStripMenuItem2, settingsToolStripMenuItem, toolStripMenuItem3, exitToolStripMenuItem });
        contextMenuTray.Name = "contextMenuTray";
        contextMenuTray.Size = new Size(164, 114);
        // 
        // toolStripMenuItem1
        // 
        toolStripMenuItem1.Name = "toolStripMenuItem1";
        toolStripMenuItem1.Size = new Size(163, 22);
        toolStripMenuItem1.Text = "Open Dashboard";
        // 
        // toolStripMenuItem2
        // 
        toolStripMenuItem2.Name = "toolStripMenuItem2";
        toolStripMenuItem2.Size = new Size(163, 22);
        toolStripMenuItem2.Text = "-";
        // 
        // settingsToolStripMenuItem
        // 
        settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
        settingsToolStripMenuItem.Size = new Size(163, 22);
        settingsToolStripMenuItem.Text = "Settings";
        // 
        // toolStripMenuItem3
        // 
        toolStripMenuItem3.Name = "toolStripMenuItem3";
        toolStripMenuItem3.Size = new Size(163, 22);
        toolStripMenuItem3.Text = "-";
        // 
        // exitToolStripMenuItem
        // 
        exitToolStripMenuItem.Name = "exitToolStripMenuItem";
        exitToolStripMenuItem.Size = new Size(163, 22);
        exitToolStripMenuItem.Text = "Exit";
        // 
        // btnExit
        // 
        btnExit.BackColor = Color.FromArgb(52, 152, 219);
        btnExit.Cursor = Cursors.Hand;
        btnExit.FlatStyle = FlatStyle.Flat;
        btnExit.ForeColor = Color.White;
        btnExit.Location = new Point(661, 46);
        btnExit.Name = "btnExit";
        btnExit.Size = new Size(100, 30);
        btnExit.TabIndex = 4;
        btnExit.Text = "Exit";
        toolTip1.SetToolTip(btnExit, "Open Settings");
        btnExit.UseVisualStyleBackColor = false;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(784, 561);
        Controls.Add(panelGrid);
        Controls.Add(panelStats);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "RDP Secure";
        panelStats.ResumeLayout(false);
        panelWhitelisted.ResumeLayout(false);
        panelWhitelisted.PerformLayout();
        panelRecentAttempts.ResumeLayout(false);
        panelRecentAttempts.PerformLayout();
        panelActiveBans.ResumeLayout(false);
        panelActiveBans.PerformLayout();
        panelGrid.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)gridBannedIPs).EndInit();
        contextMenuTray.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private Panel panelStats;
    private Panel panelActiveBans;
    private Panel panelWhitelisted;
    private Label lblWhitelistedTitle;
    private Panel panelRecentAttempts;
    private Label lblRecentAttemptsCount;
    private Label lblRecentAttemptsTitle;
    private Label lblWhitelistedCount;
    private Label lblActiveBansCount;
    private Label lblActiveBansTitle;
    private Button btnSettings;
    private ToolTip toolTip1;
    private Panel panelGrid;
    private DataGridView gridBannedIPs;
    private DataGridViewTextBoxColumn Column1;
    private DataGridViewTextBoxColumn Column2;
    private DataGridViewTextBoxColumn Column3;
    private DataGridViewTextBoxColumn Column4;
    private DataGridViewTextBoxColumn Column5;
    private NotifyIcon notifyIconRDPSecure;
    private ContextMenuStrip contextMenuTray;
    private ToolStripMenuItem toolStripMenuItem1;
    private ToolStripMenuItem toolStripMenuItem2;
    private ToolStripMenuItem settingsToolStripMenuItem;
    private ToolStripMenuItem toolStripMenuItem3;
    private ToolStripMenuItem exitToolStripMenuItem;
    private Button btnExit;
}
