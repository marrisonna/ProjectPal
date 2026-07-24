namespace TaskMan.Projects
{
    partial class FormPlanDisplay
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormPlanDisplay));
            this.elementHostArea = new System.Windows.Forms.Integration.ElementHost();
            this.SuspendLayout();
            // 
            // elementHostArea
            // 
            this.elementHostArea.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.elementHostArea.Location = new System.Drawing.Point(0, 0);
            this.elementHostArea.Name = "elementHostArea";
            this.elementHostArea.Size = new System.Drawing.Size(952, 665);
            this.elementHostArea.TabIndex = 0;
            this.elementHostArea.Text = "elementHost1";
            this.elementHostArea.Child = null;
            // 
            // FormPlanDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(953, 665);
            this.Controls.Add(this.elementHostArea);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormPlanDisplay";
            this.Text = "Gantt Display";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormPlanDisplay_FormClosed);
            this.Load += new System.EventHandler(this.FormPlanDisplay_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Integration.ElementHost elementHostArea;
    }
}