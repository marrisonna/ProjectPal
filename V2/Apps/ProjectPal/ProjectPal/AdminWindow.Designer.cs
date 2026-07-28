namespace ProjectPal
{
    partial class AdminWindow
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
            this.buttonSuperUser = new System.Windows.Forms.Button();
            this.comboBoxUsers = new System.Windows.Forms.ComboBox();
            this.fileSystemWatcher1 = new System.IO.FileSystemWatcher();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.buttonRestartAsDifferentUser = new System.Windows.Forms.Button();
            this.textBoxLevel = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonDBCopy = new System.Windows.Forms.Button();
            this.buttonModified = new System.Windows.Forms.Button();
            this.checkBoxDisableEncription = new System.Windows.Forms.CheckBox();
            this.buttonLoadAttachments = new System.Windows.Forms.Button();
            this.buttonExportAttachements = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonSuperUser
            // 
            this.buttonSuperUser.Location = new System.Drawing.Point(21, 280);
            this.buttonSuperUser.Name = "buttonSuperUser";
            this.buttonSuperUser.Size = new System.Drawing.Size(138, 23);
            this.buttonSuperUser.TabIndex = 0;
            this.buttonSuperUser.Text = "Restart as SuperUser";
            this.buttonSuperUser.UseVisualStyleBackColor = true;
            this.buttonSuperUser.Click += new System.EventHandler(this.buttonSuperUser_Click);
            // 
            // comboBoxUsers
            // 
            this.comboBoxUsers.FormattingEnabled = true;
            this.comboBoxUsers.Location = new System.Drawing.Point(6, 19);
            this.comboBoxUsers.Name = "comboBoxUsers";
            this.comboBoxUsers.Size = new System.Drawing.Size(119, 21);
            this.comboBoxUsers.Sorted = true;
            this.comboBoxUsers.TabIndex = 1;
            this.comboBoxUsers.SelectedIndexChanged += new System.EventHandler(this.comboBoxUsers_SelectedIndexChanged);
            // 
            // fileSystemWatcher1
            // 
            this.fileSystemWatcher1.EnableRaisingEvents = true;
            this.fileSystemWatcher1.SynchronizingObject = this;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.buttonRestartAsDifferentUser);
            this.groupBox1.Controls.Add(this.textBoxLevel);
            this.groupBox1.Controls.Add(this.comboBoxUsers);
            this.groupBox1.Location = new System.Drawing.Point(23, 174);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(134, 100);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Restart as different user";
            // 
            // buttonRestartAsDifferentUser
            // 
            this.buttonRestartAsDifferentUser.Location = new System.Drawing.Point(6, 73);
            this.buttonRestartAsDifferentUser.Name = "buttonRestartAsDifferentUser";
            this.buttonRestartAsDifferentUser.Size = new System.Drawing.Size(119, 23);
            this.buttonRestartAsDifferentUser.TabIndex = 3;
            this.buttonRestartAsDifferentUser.Text = "Restart as User";
            this.buttonRestartAsDifferentUser.UseVisualStyleBackColor = true;
            this.buttonRestartAsDifferentUser.Click += new System.EventHandler(this.buttonRestartAsDifferentUser_Click);
            // 
            // textBoxLevel
            // 
            this.textBoxLevel.Location = new System.Drawing.Point(6, 47);
            this.textBoxLevel.Name = "textBoxLevel";
            this.textBoxLevel.ReadOnly = true;
            this.textBoxLevel.Size = new System.Drawing.Size(119, 20);
            this.textBoxLevel.TabIndex = 2;
            this.textBoxLevel.Text = "Level";
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button2.Location = new System.Drawing.Point(93, 331);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 5;
            this.button2.Text = "Cancel";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(12, 331);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 4;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonDBCopy
            // 
            this.buttonDBCopy.Location = new System.Drawing.Point(21, 12);
            this.buttonDBCopy.Name = "buttonDBCopy";
            this.buttonDBCopy.Size = new System.Drawing.Size(138, 23);
            this.buttonDBCopy.TabIndex = 6;
            this.buttonDBCopy.Text = "Copy to SqlCE";
            this.buttonDBCopy.UseVisualStyleBackColor = true;
            this.buttonDBCopy.Click += new System.EventHandler(this.buttonDBCopy_Click);
            // 
            // buttonModified
            // 
            this.buttonModified.Location = new System.Drawing.Point(21, 41);
            this.buttonModified.Name = "buttonModified";
            this.buttonModified.Size = new System.Drawing.Size(138, 23);
            this.buttonModified.TabIndex = 7;
            this.buttonModified.Text = "Mark All as Modified";
            this.buttonModified.UseVisualStyleBackColor = true;
            this.buttonModified.Click += new System.EventHandler(this.buttonModified_Click);
            // 
            // checkBoxDisableEncription
            // 
            this.checkBoxDisableEncription.AutoSize = true;
            this.checkBoxDisableEncription.Location = new System.Drawing.Point(34, 145);
            this.checkBoxDisableEncription.Name = "checkBoxDisableEncription";
            this.checkBoxDisableEncription.Size = new System.Drawing.Size(114, 17);
            this.checkBoxDisableEncription.TabIndex = 8;
            this.checkBoxDisableEncription.Text = "Disable Encryption";
            this.checkBoxDisableEncription.UseVisualStyleBackColor = true;
            this.checkBoxDisableEncription.CheckedChanged += new System.EventHandler(this.checkBoxDisableEncription_CheckedChanged);
            // 
            // buttonLoadAttachments
            // 
            this.buttonLoadAttachments.Location = new System.Drawing.Point(21, 70);
            this.buttonLoadAttachments.Name = "buttonLoadAttachments";
            this.buttonLoadAttachments.Size = new System.Drawing.Size(138, 23);
            this.buttonLoadAttachments.TabIndex = 9;
            this.buttonLoadAttachments.Text = "Load all Attachments";
            this.buttonLoadAttachments.UseVisualStyleBackColor = true;
            this.buttonLoadAttachments.Click += new System.EventHandler(this.buttonLoadAttachments_Click);
            // 
            // buttonExportAttachements
            // 
            this.buttonExportAttachements.Location = new System.Drawing.Point(23, 99);
            this.buttonExportAttachements.Name = "buttonExportAttachements";
            this.buttonExportAttachements.Size = new System.Drawing.Size(138, 23);
            this.buttonExportAttachements.TabIndex = 10;
            this.buttonExportAttachements.Text = "Export All Attachments";
            this.buttonExportAttachements.UseVisualStyleBackColor = true;
            this.buttonExportAttachements.Click += new System.EventHandler(this.buttonExportAttachements_Click);
            // 
            // AdminWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(207, 356);
            this.Controls.Add(this.buttonExportAttachements);
            this.Controls.Add(this.buttonLoadAttachments);
            this.Controls.Add(this.checkBoxDisableEncription);
            this.Controls.Add(this.buttonModified);
            this.Controls.Add(this.buttonDBCopy);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.buttonSuperUser);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "AdminWindow";
            this.Text = "AdminWindow";
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonSuperUser;
        private System.Windows.Forms.ComboBox comboBoxUsers;
        private System.IO.FileSystemWatcher fileSystemWatcher1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button buttonRestartAsDifferentUser;
        private System.Windows.Forms.TextBox textBoxLevel;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonDBCopy;
        private System.Windows.Forms.Button buttonModified;
        private System.Windows.Forms.CheckBox checkBoxDisableEncription;
        private System.Windows.Forms.Button buttonLoadAttachments;
        private System.Windows.Forms.Button buttonExportAttachements;
    }
}