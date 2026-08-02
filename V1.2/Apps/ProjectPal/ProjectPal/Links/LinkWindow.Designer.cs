namespace ProjectPal.Links
{
    partial class LinkWindow
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
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.textBoxLink = new System.Windows.Forms.TextBox();
            this.textBoxOn = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxMadeBy = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(173, 235);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 9;
            this.buttonCancel.Text = "&Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(38, 235);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 8;
            this.buttonOK.Text = "&OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            // 
            // textBoxLink
            // 
            this.textBoxLink.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxLink.Location = new System.Drawing.Point(5, 38);
            this.textBoxLink.Multiline = true;
            this.textBoxLink.Name = "textBoxLink";
            this.textBoxLink.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxLink.Size = new System.Drawing.Size(277, 185);
            this.textBoxLink.TabIndex = 7;
            // 
            // textBoxOn
            // 
            this.textBoxOn.BackColor = System.Drawing.Color.WhiteSmoke;
            this.textBoxOn.Location = new System.Drawing.Point(185, 8);
            this.textBoxOn.Name = "textBoxOn";
            this.textBoxOn.ReadOnly = true;
            this.textBoxOn.Size = new System.Drawing.Size(100, 20);
            this.textBoxOn.TabIndex = 12;
            this.textBoxOn.Text = "dd-MMM-yyyy";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(160, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(19, 13);
            this.label2.TabIndex = 10;
            this.label2.Text = "on";
            // 
            // textBoxMadeBy
            // 
            this.textBoxMadeBy.BackColor = System.Drawing.Color.WhiteSmoke;
            this.textBoxMadeBy.Location = new System.Drawing.Point(54, 8);
            this.textBoxMadeBy.Name = "textBoxMadeBy";
            this.textBoxMadeBy.ReadOnly = true;
            this.textBoxMadeBy.Size = new System.Drawing.Size(100, 20);
            this.textBoxMadeBy.TabIndex = 11;
            this.textBoxMadeBy.Text = "Fred";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(0, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(48, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Made by";
            // 
            // LinkWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(302, 266);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.textBoxLink);
            this.Controls.Add(this.textBoxOn);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBoxMadeBy);
            this.Controls.Add(this.label1);
            this.Name = "LinkWindow";
            this.Text = "ProjectPal: Link";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.TextBox textBoxLink;
        private System.Windows.Forms.TextBox textBoxOn;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxMadeBy;
        private System.Windows.Forms.Label label1;
    }
}