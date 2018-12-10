namespace IRC_Client
{
    partial class frmServer
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
            this.ui = new System.Windows.Forms.WebBrowser();
            this.SuspendLayout();
            // 
            // webBrowser1
            // 
            this.ui.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ui.Location = new System.Drawing.Point(0, 0);
            this.ui.MinimumSize = new System.Drawing.Size(20, 20);
            this.ui.Name = "webBrowser1";
            this.ui.Size = new System.Drawing.Size(709, 393);
            this.ui.TabIndex = 5;
            // 
            // frmServer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(709, 393);
            this.Controls.Add(this.ui);
            this.Name = "frmServer";
            this.Text = "IRC  Client";
            this.Resize += new System.EventHandler(this.Resized);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.WebBrowser ui;
    }
}

