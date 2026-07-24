namespace CustomGUIControls.Grid
{
    partial class GridFilterSelect
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonNone = new System.Windows.Forms.Button();
            this.buttonAll = new System.Windows.Forms.Button();
            this.checkedListBoxSeachItems = new System.Windows.Forms.CheckedListBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BackColor = System.Drawing.SystemColors.Window;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.buttonCancel);
            this.panel1.Controls.Add(this.buttonOK);
            this.panel1.Controls.Add(this.buttonNone);
            this.panel1.Controls.Add(this.buttonAll);
            this.panel1.Controls.Add(this.checkedListBoxSeachItems);
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(132, 203);
            this.panel1.TabIndex = 0;
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(73, 177);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(50, 21);
            this.buttonCancel.TabIndex = 1;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(3, 177);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(44, 21);
            this.buttonOK.TabIndex = 2;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonNone
            // 
            this.buttonNone.Location = new System.Drawing.Point(79, 3);
            this.buttonNone.Name = "buttonNone";
            this.buttonNone.Size = new System.Drawing.Size(44, 20);
            this.buttonNone.TabIndex = 5;
            this.buttonNone.Text = "None";
            this.buttonNone.UseVisualStyleBackColor = true;
            this.buttonNone.Click += new System.EventHandler(this.buttonNone_Click);
            // 
            // buttonAll
            // 
            this.buttonAll.Location = new System.Drawing.Point(3, 3);
            this.buttonAll.Name = "buttonAll";
            this.buttonAll.Size = new System.Drawing.Size(44, 20);
            this.buttonAll.TabIndex = 4;
            this.buttonAll.Text = "All";
            this.buttonAll.UseVisualStyleBackColor = true;
            this.buttonAll.Click += new System.EventHandler(this.buttonAll_Click);
            // 
            // checkedListBoxSeachItems
            // 
            this.checkedListBoxSeachItems.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.checkedListBoxSeachItems.CheckOnClick = true;
            this.checkedListBoxSeachItems.FormattingEnabled = true;
            this.checkedListBoxSeachItems.Location = new System.Drawing.Point(3, 29);
            this.checkedListBoxSeachItems.Name = "checkedListBoxSeachItems";
            this.checkedListBoxSeachItems.ScrollAlwaysVisible = true;
            this.checkedListBoxSeachItems.Size = new System.Drawing.Size(120, 139);
            this.checkedListBoxSeachItems.TabIndex = 3;
            // 
            // GridFilterSelect
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(132, 203);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "GridFilterSelect";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "GridFilterSelect";
            this.Deactivate += new System.EventHandler(this.GridFilterSelect_Deactivate);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckedListBox checkedListBoxSeachItems;
        private System.Windows.Forms.Button buttonNone;
        private System.Windows.Forms.Button buttonAll;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonOK;

    }
}