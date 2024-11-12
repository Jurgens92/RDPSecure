namespace RDPSecure
{
    partial class LicenseForm
    {
        private System.ComponentModel.IContainer components = null;
        private Label lblStatus;
        private TextBox txtLicenseKey;
        private Button btnActivate;
        private Button btnContinue;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblStatus = new Label();
            this.txtLicenseKey = new TextBox();
            this.btnActivate = new Button();
            this.btnContinue = new Button();

            this.SuspendLayout();

            // lblStatus
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new Point(12, 9);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new Size(200, 15);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "Checking license...";

            // txtLicenseKey
            this.txtLicenseKey.Location = new Point(12, 36);
            this.txtLicenseKey.Multiline = true;
            this.txtLicenseKey.Name = "txtLicenseKey";
            this.txtLicenseKey.Size = new Size(360, 100);
            this.txtLicenseKey.TabIndex = 1;

            // btnActivate
            this.btnActivate.Location = new Point(12, 142);
            this.btnActivate.Name = "btnActivate";
            this.btnActivate.Size = new Size(75, 23);
            this.btnActivate.TabIndex = 2;
            this.btnActivate.Text = "Activate";
            this.btnActivate.UseVisualStyleBackColor = true;
            this.btnActivate.Click += new EventHandler(this.btnActivate_Click);

            // btnContinue
            this.btnContinue.Location = new Point(297, 142);
            this.btnContinue.Name = "btnContinue";
            this.btnContinue.Size = new Size(75, 23);
            this.btnContinue.TabIndex = 3;
            this.btnContinue.Text = "Continue";
            this.btnContinue.UseVisualStyleBackColor = true;
            this.btnContinue.Enabled = false;
            this.btnContinue.Click += new EventHandler(this.btnContinue_Click);

            // LicenseForm
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(384, 181);
            this.Controls.Add(this.btnContinue);
            this.Controls.Add(this.btnActivate);
            this.Controls.Add(this.txtLicenseKey);
            this.Controls.Add(this.lblStatus);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LicenseForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "RDPSecure License Activation";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}