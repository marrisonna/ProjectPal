namespace TaskMan
{
    partial class FormTaskMerge
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
            this.labelFieldName = new System.Windows.Forms.Label();
            this.labelYour = new System.Windows.Forms.Label();
            this.labelOther = new System.Windows.Forms.Label();
            this.labelMerged = new System.Windows.Forms.Label();
            this.elementHostMainArea = new System.Windows.Forms.Integration.ElementHost();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // labelFieldName
            // 
            this.labelFieldName.AutoSize = true;
            this.labelFieldName.Location = new System.Drawing.Point(13, 13);
            this.labelFieldName.Name = "labelFieldName";
            this.labelFieldName.Size = new System.Drawing.Size(60, 13);
            this.labelFieldName.TabIndex = 0;
            this.labelFieldName.Text = "Field Name";
            // 
            // labelYour
            // 
            this.labelYour.AutoSize = true;
            this.labelYour.Location = new System.Drawing.Point(103, 13);
            this.labelYour.Name = "labelYour";
            this.labelYour.Size = new System.Drawing.Size(59, 13);
            this.labelYour.TabIndex = 1;
            this.labelYour.Text = "Your Value";
            // 
            // labelOther
            // 
            this.labelOther.AutoSize = true;
            this.labelOther.Location = new System.Drawing.Point(256, 13);
            this.labelOther.Name = "labelOther";
            this.labelOther.Size = new System.Drawing.Size(63, 13);
            this.labelOther.TabIndex = 2;
            this.labelOther.Text = "Other Value";
            // 
            // labelMerged
            // 
            this.labelMerged.AutoSize = true;
            this.labelMerged.Location = new System.Drawing.Point(476, 13);
            this.labelMerged.Name = "labelMerged";
            this.labelMerged.Size = new System.Drawing.Size(73, 13);
            this.labelMerged.TabIndex = 3;
            this.labelMerged.Text = "Merged Value";
            // 
            // elementHostMainArea
            // 
            this.elementHostMainArea.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.elementHostMainArea.Location = new System.Drawing.Point(0, 30);
            this.elementHostMainArea.Margin = new System.Windows.Forms.Padding(0);
            this.elementHostMainArea.Name = "elementHostMainArea";
            this.elementHostMainArea.Size = new System.Drawing.Size(637, 335);
            this.elementHostMainArea.TabIndex = 4;
            this.elementHostMainArea.Text = "elementHost1";
            this.elementHostMainArea.ChildChanged += new System.EventHandler<System.Windows.Forms.Integration.ChildChangedEventArgs>(this.elementHost1_ChildChanged);
            this.elementHostMainArea.Child = null;
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.buttonOK.Location = new System.Drawing.Point(193, 370);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 5;
            this.buttonOK.Text = "&OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.buttonCancel.Location = new System.Drawing.Point(421, 370);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 6;
            this.buttonCancel.Text = "&Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // FormTaskMerge
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(638, 405);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.elementHostMainArea);
            this.Controls.Add(this.labelMerged);
            this.Controls.Add(this.labelOther);
            this.Controls.Add(this.labelYour);
            this.Controls.Add(this.labelFieldName);
            this.MinimumSize = new System.Drawing.Size(300, 300);
            this.Name = "FormTaskMerge";
            this.Text = "FormTaskMerge";
            this.Load += new System.EventHandler(this.FormTaskMerge_Load);
            this.Resize += new System.EventHandler(this.FormTaskMerge_Resize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelFieldName;
        private System.Windows.Forms.Label labelYour;
        private System.Windows.Forms.Label labelOther;
        private System.Windows.Forms.Label labelMerged;
        private System.Windows.Forms.Integration.ElementHost elementHostMainArea;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
    }
}