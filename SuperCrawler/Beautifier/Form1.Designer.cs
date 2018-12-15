namespace Beautifier
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
            this.textboxScrapeFolder = new System.Windows.Forms.TextBox();
            this.buttonScrapeFolder = new System.Windows.Forms.Button();
            this.textboxOutput = new System.Windows.Forms.RichTextBox();
            this.buttonBeautify = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textboxScrapeFolder
            // 
            this.textboxScrapeFolder.Location = new System.Drawing.Point(12, 12);
            this.textboxScrapeFolder.Name = "textboxScrapeFolder";
            this.textboxScrapeFolder.Size = new System.Drawing.Size(451, 20);
            this.textboxScrapeFolder.TabIndex = 0;
            // 
            // buttonScrapeFolder
            // 
            this.buttonScrapeFolder.Location = new System.Drawing.Point(469, 10);
            this.buttonScrapeFolder.Name = "buttonScrapeFolder";
            this.buttonScrapeFolder.Size = new System.Drawing.Size(128, 23);
            this.buttonScrapeFolder.TabIndex = 1;
            this.buttonScrapeFolder.Text = "Select Scrape Folder";
            this.buttonScrapeFolder.UseVisualStyleBackColor = true;
            this.buttonScrapeFolder.Click += new System.EventHandler(this.buttonScrapeFolder_Click);
            // 
            // textboxOutput
            // 
            this.textboxOutput.Location = new System.Drawing.Point(12, 54);
            this.textboxOutput.Name = "textboxOutput";
            this.textboxOutput.Size = new System.Drawing.Size(585, 344);
            this.textboxOutput.TabIndex = 2;
            this.textboxOutput.Text = "";
            // 
            // buttonBeautify
            // 
            this.buttonBeautify.Location = new System.Drawing.Point(469, 404);
            this.buttonBeautify.Name = "buttonBeautify";
            this.buttonBeautify.Size = new System.Drawing.Size(128, 23);
            this.buttonBeautify.TabIndex = 3;
            this.buttonBeautify.Text = "Beautify";
            this.buttonBeautify.UseVisualStyleBackColor = true;
            this.buttonBeautify.Click += new System.EventHandler(this.buttonBeautify_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(609, 439);
            this.Controls.Add(this.buttonBeautify);
            this.Controls.Add(this.textboxOutput);
            this.Controls.Add(this.buttonScrapeFolder);
            this.Controls.Add(this.textboxScrapeFolder);
            this.Name = "Form1";
            this.Text = "Beautifier";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textboxScrapeFolder;
        private System.Windows.Forms.Button buttonScrapeFolder;
        private System.Windows.Forms.RichTextBox textboxOutput;
        private System.Windows.Forms.Button buttonBeautify;
    }
}

