namespace PrepareForAnalysis
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
            this.SuspendLayout();
            // 
            // textboxOutput
            // 
            this.textboxOutput.Location = new System.Drawing.Point(12, 12);
            this.textboxOutput.Name = "textboxOutput";
            this.textboxOutput.Size = new System.Drawing.Size(722, 586);
            this.textboxOutput.TabIndex = 0;
            this.textboxOutput.Text = "";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(746, 610);
            this.Controls.Add(this.textboxOutput);
            this.Name = "Form1";
            this.Text = "Prepare for Analysis";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox textboxOutput;
    }
}

