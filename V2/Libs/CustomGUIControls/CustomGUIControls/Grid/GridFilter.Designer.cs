namespace CustomGUIControls.Grid
{
    partial class GridFilter
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.comboBoxSearchText = new System.Windows.Forms.TextBox();
            this.buttonFilter = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // comboBoxSearchText
            // 
            this.comboBoxSearchText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxSearchText.Location = new System.Drawing.Point(0, 0);
            this.comboBoxSearchText.Name = "comboBoxSearchText";
            this.comboBoxSearchText.Size = new System.Drawing.Size(100, 20);
            this.comboBoxSearchText.TabIndex = 2;
            this.comboBoxSearchText.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.comboBoxSearchText_KeyPress);
            this.comboBoxSearchText.Leave += new System.EventHandler(this.comboBoxSearchText_Leave);
            // 
            // buttonFilter
            // 
            this.buttonFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonFilter.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonFilter.Location = new System.Drawing.Point(101, 0);
            this.buttonFilter.Name = "buttonFilter";
            this.buttonFilter.Size = new System.Drawing.Size(21, 21);
            this.buttonFilter.TabIndex = 3;
            this.buttonFilter.Text = "+";
            this.buttonFilter.UseVisualStyleBackColor = true;
            this.buttonFilter.Click += new System.EventHandler(this.buttonFilter_Click);
            // 
            // GridFilter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.buttonFilter);
            this.Controls.Add(this.comboBoxSearchText);
            this.Name = "GridFilter";
            this.Size = new System.Drawing.Size(121, 21);
            this.SizeChanged += new System.EventHandler(this.GridFilter_SizeChanged);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox comboBoxSearchText;
        private System.Windows.Forms.Button buttonFilter;
    }
}
