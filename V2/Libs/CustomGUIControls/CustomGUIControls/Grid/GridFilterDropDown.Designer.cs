namespace CustomGUIControls.Grid
{
    partial class GridFilterDropDown
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
            this.checkedListBoxSeachItems = new System.Windows.Forms.CheckedListBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonNone = new System.Windows.Forms.Button();
            this.buttonAll = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // checkedListBoxSeachItems
            // 
            this.checkedListBoxSeachItems.FormattingEnabled = true;
            this.checkedListBoxSeachItems.Location = new System.Drawing.Point(0, 20);
            this.checkedListBoxSeachItems.Name = "checkedListBoxSeachItems";
            this.checkedListBoxSeachItems.ScrollAlwaysVisible = true;
            this.checkedListBoxSeachItems.Size = new System.Drawing.Size(120, 94);
            this.checkedListBoxSeachItems.TabIndex = 2;
            // 
            // buttonOK
            // 
            this.buttonOK.Location = new System.Drawing.Point(0, -1);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(44, 21);
            this.buttonOK.TabIndex = 3;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            // 
            // buttonCancel
            // 
            this.buttonCancel.Location = new System.Drawing.Point(44, -1);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(50, 21);
            this.buttonCancel.TabIndex = 4;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // buttonNone
            // 
            this.buttonNone.Location = new System.Drawing.Point(138, 0);
            this.buttonNone.Name = "buttonNone";
            this.buttonNone.Size = new System.Drawing.Size(44, 20);
            this.buttonNone.TabIndex = 6;
            this.buttonNone.Text = "None";
            this.buttonNone.UseVisualStyleBackColor = true;
            // 
            // buttonAll
            // 
            this.buttonAll.Location = new System.Drawing.Point(94, 0);
            this.buttonAll.Name = "buttonAll";
            this.buttonAll.Size = new System.Drawing.Size(44, 20);
            this.buttonAll.TabIndex = 5;
            this.buttonAll.Text = "All";
            this.buttonAll.UseVisualStyleBackColor = true;
            // 
            // GridFilterDropDown
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.buttonNone);
            this.Controls.Add(this.buttonAll);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.checkedListBoxSeachItems);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "GridFilterDropDown";
            this.Size = new System.Drawing.Size(183, 114);
            this.SizeChanged += new System.EventHandler(this.GridFilterDropDown_SizeChanged);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckedListBox checkedListBoxSeachItems;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonNone;
        private System.Windows.Forms.Button buttonAll;
    }
}
