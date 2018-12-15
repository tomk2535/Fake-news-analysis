namespace SuperCrawler
{
    partial class Form1
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
            this.textboxOutput = new System.Windows.Forms.RichTextBox();
            this.progressbar = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // textboxOutput
            // 
            this.textboxOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.textboxOutput.Location = new System.Drawing.Point(12, 823);
            this.textboxOutput.Name = "textboxOutput";
            this.textboxOutput.Size = new System.Drawing.Size(459, 138);
            this.textboxOutput.TabIndex = 0;
            this.textboxOutput.Text = "";
            // 
            // progressbar
            // 
            this.progressbar.Location = new System.Drawing.Point(12, 12);
            this.progressbar.Name = "progressbar";
            this.progressbar.Size = new System.Drawing.Size(232, 23);
            this.progressbar.TabIndex = 2;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1469, 983);
            this.Controls.Add(this.progressbar);
            this.Controls.Add(this.textboxOutput);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox textboxOutput;
        private System.Windows.Forms.ProgressBar progressbar;
    }
}

