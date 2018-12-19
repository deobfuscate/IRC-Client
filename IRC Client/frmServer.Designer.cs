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
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // ui
            // 
            this.ui.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ui.Location = new System.Drawing.Point(0, 0);
            this.ui.Name = "ui";
            this.ui.Size = new System.Drawing.Size(1058, 653);
            this.ui.TabIndex = 5;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(603, 114);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(64, 25);
            this.button1.TabIndex = 6;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // frmServer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1058, 653);
            this.Controls.Add(this.ui);
            this.Controls.Add(this.button1);
            this.DataBindings.Add(new System.Windows.Forms.Binding("Location", global::IRC_Client.Properties.Settings.Default, "Location", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.Location = global::IRC_Client.Properties.Settings.Default.Location;
            this.MinimumSize = new System.Drawing.Size(725, 432);
            this.Name = "frmServer";
            this.Text = "IRC  Client";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Closing);
            this.Resize += new System.EventHandler(this.Resized);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.WebBrowser ui;
        private System.Windows.Forms.Button button1;
    }
}

