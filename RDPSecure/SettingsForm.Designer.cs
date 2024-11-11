namespace RDPSecure
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            contextMenuIPList = new ContextMenuStrip(components);
            menuItemRemoveIP = new ToolStripMenuItem();
            menuItemToggleStatus = new ToolStripMenuItem();
            toolTip1 = new ToolTip(components);
            tabSystem = new TabPage();
            tabIPManagement = new TabPage();
            panelIPControls = new Panel();
            txtIPAddress = new TextBox();
            btnAddWhitelist = new Button();
            btnAddBlacklist = new Button();
            panelIPGrid = new Panel();
            gridIPList = new DataGridView();
            colStatus = new DataGridViewTextBoxColumn();
            colAddedDate = new DataGridViewTextBoxColumn();
            colType = new DataGridViewTextBoxColumn();
            colIP = new DataGridViewTextBoxColumn();
            tabProtection = new TabPage();
            groupBox1 = new GroupBox();
            nudMaxAttempts = new NumericUpDown();
            lblMaxAttempts = new Label();
            nudTimeWindow = new NumericUpDown();
            lblTimeWindow = new Label();
            gbBanDuration = new GroupBox();
            nudPrivateIPBanHours = new NumericUpDown();
            lblPrivateIPBan = new Label();
            nudPublicIPBanDays = new NumericUpDown();
            lblPublicIPBan = new Label();
            btnOK = new Button();
            btnCancel = new Button();
            tabControlSettings = new TabControl();
            contextMenuIPList.SuspendLayout();
            tabIPManagement.SuspendLayout();
            panelIPControls.SuspendLayout();
            panelIPGrid.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridIPList).BeginInit();
            tabProtection.SuspendLayout();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudMaxAttempts).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudTimeWindow).BeginInit();
            gbBanDuration.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudPrivateIPBanHours).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudPublicIPBanDays).BeginInit();
            tabControlSettings.SuspendLayout();
            SuspendLayout();
            // 
            // contextMenuIPList
            // 
            contextMenuIPList.Items.AddRange(new ToolStripItem[] { menuItemRemoveIP, menuItemToggleStatus });
            contextMenuIPList.Name = "contextMenuIPList";
            contextMenuIPList.Size = new Size(168, 48);
            // 
            // menuItemRemoveIP
            // 
            menuItemRemoveIP.Name = "menuItemRemoveIP";
            menuItemRemoveIP.Size = new Size(167, 22);
            menuItemRemoveIP.Text = "Remove from List";
            // 
            // menuItemToggleStatus
            // 
            menuItemToggleStatus.Name = "menuItemToggleStatus";
            menuItemToggleStatus.Size = new Size(167, 22);
            menuItemToggleStatus.Text = "Enable/Disable";
            // 
            // tabSystem
            // 
            tabSystem.Location = new Point(4, 24);
            tabSystem.Name = "tabSystem";
            tabSystem.Padding = new Padding(3);
            tabSystem.Size = new Size(576, 433);
            tabSystem.TabIndex = 2;
            tabSystem.Text = "System";
            tabSystem.UseVisualStyleBackColor = true;
            // 
            // tabIPManagement
            // 
            tabIPManagement.Controls.Add(panelIPGrid);
            tabIPManagement.Controls.Add(panelIPControls);
            tabIPManagement.Location = new Point(4, 24);
            tabIPManagement.Name = "tabIPManagement";
            tabIPManagement.Padding = new Padding(3);
            tabIPManagement.Size = new Size(576, 433);
            tabIPManagement.TabIndex = 1;
            tabIPManagement.Text = "IP Management";
            tabIPManagement.UseVisualStyleBackColor = true;
            // 
            // panelIPControls
            // 
            panelIPControls.Controls.Add(btnAddBlacklist);
            panelIPControls.Controls.Add(btnAddWhitelist);
            panelIPControls.Controls.Add(txtIPAddress);
            panelIPControls.Dock = DockStyle.Top;
            panelIPControls.Location = new Point(3, 3);
            panelIPControls.Name = "panelIPControls";
            panelIPControls.Padding = new Padding(10);
            panelIPControls.Size = new Size(570, 60);
            panelIPControls.TabIndex = 0;
            // 
            // txtIPAddress
            // 
            txtIPAddress.Location = new Point(10, 20);
            txtIPAddress.Name = "txtIPAddress";
            txtIPAddress.PlaceholderText = "Enter IP Address";
            txtIPAddress.Size = new Size(200, 23);
            txtIPAddress.TabIndex = 0;
            // 
            // btnAddWhitelist
            // 
            btnAddWhitelist.BackColor = Color.FromArgb(39, 174, 96);
            btnAddWhitelist.FlatStyle = FlatStyle.Flat;
            btnAddWhitelist.ForeColor = Color.White;
            btnAddWhitelist.Location = new Point(220, 20);
            btnAddWhitelist.Name = "btnAddWhitelist";
            btnAddWhitelist.Size = new Size(120, 23);
            btnAddWhitelist.TabIndex = 1;
            btnAddWhitelist.Text = "Add to Whitelist";
            btnAddWhitelist.UseVisualStyleBackColor = false;
            // 
            // btnAddBlacklist
            // 
            btnAddBlacklist.BackColor = Color.Red;
            btnAddBlacklist.FlatStyle = FlatStyle.Flat;
            btnAddBlacklist.ForeColor = Color.White;
            btnAddBlacklist.Location = new Point(346, 20);
            btnAddBlacklist.Name = "btnAddBlacklist";
            btnAddBlacklist.Size = new Size(120, 23);
            btnAddBlacklist.TabIndex = 2;
            btnAddBlacklist.Text = "Add to Banlist";
            btnAddBlacklist.UseVisualStyleBackColor = false;
            // 
            // panelIPGrid
            // 
            panelIPGrid.Controls.Add(gridIPList);
            panelIPGrid.Dock = DockStyle.Fill;
            panelIPGrid.Location = new Point(3, 63);
            panelIPGrid.Name = "panelIPGrid";
            panelIPGrid.Padding = new Padding(10);
            panelIPGrid.Size = new Size(570, 367);
            panelIPGrid.TabIndex = 1;
            // 
            // gridIPList
            // 
            gridIPList.AllowUserToAddRows = false;
            gridIPList.AllowUserToDeleteRows = false;
            gridIPList.AllowUserToResizeColumns = false;
            gridIPList.AllowUserToResizeRows = false;
            gridIPList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridIPList.BackgroundColor = Color.FromArgb(224, 224, 224);
            gridIPList.BorderStyle = BorderStyle.None;
            gridIPList.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            gridIPList.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridIPList.Columns.AddRange(new DataGridViewColumn[] { colIP, colType, colAddedDate, colStatus });
            gridIPList.Dock = DockStyle.Fill;
            gridIPList.EnableHeadersVisualStyles = false;
            gridIPList.Location = new Point(10, 10);
            gridIPList.MultiSelect = false;
            gridIPList.Name = "gridIPList";
            gridIPList.RowHeadersVisible = false;
            gridIPList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridIPList.Size = new Size(550, 347);
            gridIPList.TabIndex = 0;
            // 
            // colStatus
            // 
            colStatus.HeaderText = "Status";
            colStatus.Name = "colStatus";
            colStatus.ReadOnly = true;
            // 
            // colAddedDate
            // 
            colAddedDate.HeaderText = "Added Date";
            colAddedDate.Name = "colAddedDate";
            colAddedDate.ReadOnly = true;
            // 
            // colType
            // 
            colType.HeaderText = "Type";
            colType.Name = "colType";
            colType.ReadOnly = true;
            // 
            // colIP
            // 
            colIP.HeaderText = "IP Address";
            colIP.Name = "colIP";
            colIP.ReadOnly = true;
            // 
            // tabProtection
            // 
            tabProtection.Controls.Add(btnCancel);
            tabProtection.Controls.Add(btnOK);
            tabProtection.Controls.Add(gbBanDuration);
            tabProtection.Controls.Add(groupBox1);
            tabProtection.Location = new Point(4, 24);
            tabProtection.Name = "tabProtection";
            tabProtection.Padding = new Padding(3);
            tabProtection.Size = new Size(576, 433);
            tabProtection.TabIndex = 0;
            tabProtection.Text = "Protection";
            tabProtection.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(lblTimeWindow);
            groupBox1.Controls.Add(nudTimeWindow);
            groupBox1.Controls.Add(lblMaxAttempts);
            groupBox1.Controls.Add(nudMaxAttempts);
            groupBox1.Location = new Point(20, 20);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(520, 100);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Failed Login Attempts";
            // 
            // nudMaxAttempts
            // 
            nudMaxAttempts.Location = new Point(189, 30);
            nudMaxAttempts.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            nudMaxAttempts.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudMaxAttempts.Name = "nudMaxAttempts";
            nudMaxAttempts.Size = new Size(60, 23);
            nudMaxAttempts.TabIndex = 0;
            nudMaxAttempts.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // lblMaxAttempts
            // 
            lblMaxAttempts.AutoSize = true;
            lblMaxAttempts.Location = new Point(20, 32);
            lblMaxAttempts.Name = "lblMaxAttempts";
            lblMaxAttempts.Size = new Size(115, 15);
            lblMaxAttempts.TabIndex = 1;
            lblMaxAttempts.Text = "Maximum attempts:";
            // 
            // nudTimeWindow
            // 
            nudTimeWindow.Location = new Point(189, 59);
            nudTimeWindow.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            nudTimeWindow.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudTimeWindow.Name = "nudTimeWindow";
            nudTimeWindow.Size = new Size(60, 23);
            nudTimeWindow.TabIndex = 2;
            nudTimeWindow.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // lblTimeWindow
            // 
            lblTimeWindow.AutoSize = true;
            lblTimeWindow.Location = new Point(20, 62);
            lblTimeWindow.Name = "lblTimeWindow";
            lblTimeWindow.Size = new Size(135, 15);
            lblTimeWindow.TabIndex = 3;
            lblTimeWindow.Text = "Time window (minutes):";
            // 
            // gbBanDuration
            // 
            gbBanDuration.Controls.Add(lblPublicIPBan);
            gbBanDuration.Controls.Add(nudPublicIPBanDays);
            gbBanDuration.Controls.Add(lblPrivateIPBan);
            gbBanDuration.Controls.Add(nudPrivateIPBanHours);
            gbBanDuration.Location = new Point(20, 126);
            gbBanDuration.Name = "gbBanDuration";
            gbBanDuration.Size = new Size(520, 111);
            gbBanDuration.TabIndex = 1;
            gbBanDuration.TabStop = false;
            gbBanDuration.Text = "Ban Duration Settings";
            // 
            // nudPrivateIPBanHours
            // 
            nudPrivateIPBanHours.Location = new Point(189, 30);
            nudPrivateIPBanHours.Maximum = new decimal(new int[] { 24, 0, 0, 0 });
            nudPrivateIPBanHours.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudPrivateIPBanHours.Name = "nudPrivateIPBanHours";
            nudPrivateIPBanHours.Size = new Size(60, 23);
            nudPrivateIPBanHours.TabIndex = 0;
            nudPrivateIPBanHours.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblPrivateIPBan
            // 
            lblPrivateIPBan.AutoSize = true;
            lblPrivateIPBan.Location = new Point(20, 32);
            lblPrivateIPBan.Name = "lblPrivateIPBan";
            lblPrivateIPBan.Size = new Size(123, 15);
            lblPrivateIPBan.TabIndex = 1;
            lblPrivateIPBan.Text = "Private IP Ban (hours):";
            // 
            // nudPublicIPBanDays
            // 
            nudPublicIPBanDays.Location = new Point(189, 70);
            nudPublicIPBanDays.Maximum = new decimal(new int[] { 365, 0, 0, 0 });
            nudPublicIPBanDays.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudPublicIPBanDays.Name = "nudPublicIPBanDays";
            nudPublicIPBanDays.Size = new Size(60, 23);
            nudPublicIPBanDays.TabIndex = 2;
            nudPublicIPBanDays.Value = new decimal(new int[] { 30, 0, 0, 0 });
            // 
            // lblPublicIPBan
            // 
            lblPublicIPBan.AutoSize = true;
            lblPublicIPBan.Location = new Point(20, 72);
            lblPublicIPBan.Name = "lblPublicIPBan";
            lblPublicIPBan.Size = new Size(114, 15);
            lblPublicIPBan.TabIndex = 3;
            lblPublicIPBan.Text = "Public IP Ban (days):";
            // 
            // btnOK
            // 
            btnOK.BackColor = Color.FromArgb(52, 152, 219);
            btnOK.DialogResult = DialogResult.OK;
            btnOK.ForeColor = Color.White;
            btnOK.Location = new Point(20, 292);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(80, 30);
            btnOK.TabIndex = 2;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = false;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(20, 332);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(80, 30);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // tabControlSettings
            // 
            tabControlSettings.Controls.Add(tabProtection);
            tabControlSettings.Controls.Add(tabIPManagement);
            tabControlSettings.Controls.Add(tabSystem);
            tabControlSettings.Dock = DockStyle.Fill;
            tabControlSettings.Location = new Point(0, 0);
            tabControlSettings.Name = "tabControlSettings";
            tabControlSettings.SelectedIndex = 0;
            tabControlSettings.Size = new Size(584, 461);
            tabControlSettings.TabIndex = 0;
            // 
            // SettingsForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(584, 461);
            Controls.Add(tabControlSettings);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "RDPSecure Settings";
            contextMenuIPList.ResumeLayout(false);
            tabIPManagement.ResumeLayout(false);
            panelIPControls.ResumeLayout(false);
            panelIPControls.PerformLayout();
            panelIPGrid.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)gridIPList).EndInit();
            tabProtection.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudMaxAttempts).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudTimeWindow).EndInit();
            gbBanDuration.ResumeLayout(false);
            gbBanDuration.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudPrivateIPBanHours).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudPublicIPBanDays).EndInit();
            tabControlSettings.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private ContextMenuStrip contextMenuIPList;
        private ToolStripMenuItem menuItemRemoveIP;
        private ToolStripMenuItem menuItemToggleStatus;
        private ToolTip toolTip1;
        private TabPage tabSystem;
        private TabPage tabIPManagement;
        private Panel panelIPGrid;
        private DataGridView gridIPList;
        private DataGridViewTextBoxColumn colIP;
        private DataGridViewTextBoxColumn colType;
        private DataGridViewTextBoxColumn colAddedDate;
        private DataGridViewTextBoxColumn colStatus;
        private Panel panelIPControls;
        private Button btnAddBlacklist;
        private Button btnAddWhitelist;
        private TextBox txtIPAddress;
        private TabPage tabProtection;
        private Button btnCancel;
        private Button btnOK;
        private GroupBox gbBanDuration;
        private Label lblPublicIPBan;
        private NumericUpDown nudPublicIPBanDays;
        private Label lblPrivateIPBan;
        private NumericUpDown nudPrivateIPBanHours;
        private GroupBox groupBox1;
        private Label lblTimeWindow;
        private NumericUpDown nudTimeWindow;
        private Label lblMaxAttempts;
        private NumericUpDown nudMaxAttempts;
        private TabControl tabControlSettings;
    }
}