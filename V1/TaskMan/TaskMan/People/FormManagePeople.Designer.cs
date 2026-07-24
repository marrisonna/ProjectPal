namespace TaskMan.People
{
    partial class FormManagePeople
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormManagePeople));
            this.gridControlPeople = new CustomGUIControls.Grid.GridControl();
            this.buttonNewPerson = new System.Windows.Forms.Button();
            this.buttonDeletePerson = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // gridControlPeople
            // 
            this.gridControlPeople.AllowCellDrop = false;
            this.gridControlPeople.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridControlPeople.FilterIsVisible = false;
            this.gridControlPeople.Location = new System.Drawing.Point(86, 9);
            this.gridControlPeople.Margin = new System.Windows.Forms.Padding(0);
            this.gridControlPeople.Name = "gridControlPeople";
            this.gridControlPeople.Size = new System.Drawing.Size(751, 243);
            this.gridControlPeople.TabIndex = 0;
            // 
            // buttonNewPerson
            // 
            this.buttonNewPerson.Location = new System.Drawing.Point(13, 13);
            this.buttonNewPerson.Name = "buttonNewPerson";
            this.buttonNewPerson.Size = new System.Drawing.Size(62, 23);
            this.buttonNewPerson.TabIndex = 1;
            this.buttonNewPerson.Text = "New";
            this.buttonNewPerson.UseVisualStyleBackColor = true;
            this.buttonNewPerson.Click += new System.EventHandler(this.buttonNewPerson_Click);
            // 
            // buttonDeletePerson
            // 
            this.buttonDeletePerson.Location = new System.Drawing.Point(13, 86);
            this.buttonDeletePerson.Name = "buttonDeletePerson";
            this.buttonDeletePerson.Size = new System.Drawing.Size(62, 23);
            this.buttonDeletePerson.TabIndex = 2;
            this.buttonDeletePerson.Text = "Delete";
            this.buttonDeletePerson.UseVisualStyleBackColor = true;
            this.buttonDeletePerson.Click += new System.EventHandler(this.buttonDeletePerson_Click);
            // 
            // FormManagePeople
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(846, 261);
            this.Controls.Add(this.buttonDeletePerson);
            this.Controls.Add(this.buttonNewPerson);
            this.Controls.Add(this.gridControlPeople);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormManagePeople";
            this.Text = "Manage People";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormManagePeople_FormClosed);
            this.Load += new System.EventHandler(this.FormManageUsers_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private CustomGUIControls.Grid.GridControl gridControlPeople;
        private System.Windows.Forms.Button buttonNewPerson;
        private System.Windows.Forms.Button buttonDeletePerson;
    }
}