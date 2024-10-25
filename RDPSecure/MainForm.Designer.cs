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
        panelStats.SuspendLayout();
        panelWhitelisted.SuspendLayout();
        panelRecentAttempts.SuspendLayout();
        panelActiveBans.SuspendLayout();
        SuspendLayout();
        // 
        // panelStats
        // 
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
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(784, 561);
        Controls.Add(panelStats);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Form1";
        panelStats.ResumeLayout(false);
        panelWhitelisted.ResumeLayout(false);
        panelWhitelisted.PerformLayout();
        panelRecentAttempts.ResumeLayout(false);
        panelRecentAttempts.PerformLayout();
        panelActiveBans.ResumeLayout(false);
        panelActiveBans.PerformLayout();
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
}
