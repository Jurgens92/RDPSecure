// UpdateForm.Designer.cs
namespace RDPSecure
{
    partial class UpdateForm
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
            lblNewVersion = new Label();
            txtReleaseNotes = new TextBox();
            btnInstall = new Button();
            btnCancel = new Button();
            progressBar = new ProgressBar();
            lblStatus = new Label();
            lblCritical = new Label();
            lblReleaseDate = new Label();
            SuspendLayout();
            // 
            // lblNewVersion
            // 
            lblNewVersion.AutoSize = true;
            lblNewVersion.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblNewVersion.Location = new Point(12, 9);
            lblNewVersion.Name = "lblNewVersion";
            lblNewVersion.Size = new Size(200, 21);
            lblNewVersion.TabIndex = 0;
            lblNewVersion.Text = "New Version Available";
            // 
            // lblReleaseDate
            // 
            lblReleaseDate.AutoSize = true;
            lblReleaseDate.Location = new Point(14, 40);
            lblReleaseDate.Name = "lblReleaseDate";
            lblReleaseDate.Size = new Size(200, 15);
            lblReleaseDate.TabIndex = 1;
            // 
            // lblCritical
            // 
            lblCritical.AutoSize = true;
            lblCritical.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblCritical.ForeColor = Color.Red;
            lblCritical.Location = new Point(250, 14);
            lblCritical.Name = "lblCritical";
            lblCritical.Size = new Size(100, 15);
            lblCritical.TabIndex = 2;
            lblCritical.Visible = false;
            // 
            // txtReleaseNotes
            // 
            txtReleaseNotes.Location = new Point(12, 70);
            txtReleaseNotes.Multiline = true;
            txtReleaseNotes.Name = "txtReleaseNotes";
            txtReleaseNotes.ReadOnly = true;
            txtReleaseNotes.ScrollBars = ScrollBars.Vertical;
            txtReleaseNotes.Size = new Size(360, 150);
            txtReleaseNotes.TabIndex = 3;
            // 
            // progressBar
            // 
            progressBar.Location = new Point(12, 230);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(360, 23);
            progressBar.TabIndex = 4;
            progressBar.Visible = false;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(12, 260);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(200, 15);
            lblStatus.TabIndex = 5;
            lblStatus.Visible = false;
            // 
            // btnInstall
            // 
            btnInstall.Location = new Point(200, 290);
            btnInstall.Name = "btnInstall";
            btnInstall.Size = new Size(82, 27);
            btnInstall.TabIndex = 6;
            btnInstall.Text = "Install";
            btnInstall.Click += btnInstall_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(290, 290);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(82, 27);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "Cancel";
            // 
            // UpdateForm
            // 
            AcceptButton = btnInstall;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(384, 331);
            Controls.AddRange(new Control[] {
                lblNewVersion,
                lblReleaseDate,
                lblCritical,
                txtReleaseNotes,
                progressBar,
                lblStatus,
                btnInstall,
                btnCancel
            });
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "UpdateForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "RDPSecure Update";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblNewVersion;
        private Label lblReleaseDate;
        private Label lblCritical;
        private TextBox txtReleaseNotes;
        private ProgressBar progressBar;
        private Label lblStatus;
        private Button btnInstall;
        private Button btnCancel;
    }
}