namespace TaskMan
{
    partial class MainWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.buttonShowTasks = new System.Windows.Forms.Button();
            this.buttonComponents = new System.Windows.Forms.Button();
            this.buttonProjects = new System.Windows.Forms.Button();
            this.buttonSave = new System.Windows.Forms.Button();
            this.buttonFind = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripVersion = new System.Windows.Forms.ToolStripDropDownButton();
            this.ToolStripMenuItemVersion = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItemCopyright = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripStatusLabelUserRole = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripSplitButton1 = new System.Windows.Forms.ToolStripSplitButton();
            this.adminToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.manageUsersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.buttonRefresh = new System.Windows.Forms.Button();
            this.gridControl = new CustomGUIControls.Grid.GridControl();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonShowTasks
            // 
            this.buttonShowTasks.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonShowTasks.Location = new System.Drawing.Point(1, 1);
            this.buttonShowTasks.Name = "buttonShowTasks";
            this.buttonShowTasks.Size = new System.Drawing.Size(84, 23);
            this.buttonShowTasks.TabIndex = 6;
            this.buttonShowTasks.Text = "Task List";
            this.buttonShowTasks.UseVisualStyleBackColor = true;
            this.buttonShowTasks.Click += new System.EventHandler(this.buttonShowTasks_Click);
            // 
            // buttonComponents
            // 
            this.buttonComponents.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonComponents.Location = new System.Drawing.Point(218, 1);
            this.buttonComponents.Name = "buttonComponents";
            this.buttonComponents.Size = new System.Drawing.Size(121, 23);
            this.buttonComponents.TabIndex = 7;
            this.buttonComponents.Text = "Show Components";
            this.buttonComponents.UseVisualStyleBackColor = true;
            this.buttonComponents.Click += new System.EventHandler(this.buttonComponents_Click);
            // 
            // buttonProjects
            // 
            this.buttonProjects.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonProjects.Location = new System.Drawing.Point(91, 1);
            this.buttonProjects.Name = "buttonProjects";
            this.buttonProjects.Size = new System.Drawing.Size(121, 23);
            this.buttonProjects.TabIndex = 8;
            this.buttonProjects.Text = "Show Projects";
            this.buttonProjects.UseVisualStyleBackColor = true;
            this.buttonProjects.Click += new System.EventHandler(this.buttonProjects_Click);
            // 
            // buttonSave
            // 
            this.buttonSave.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonSave.Location = new System.Drawing.Point(516, 1);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(75, 23);
            this.buttonSave.TabIndex = 10;
            this.buttonSave.Text = "SaveAll";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            // 
            // buttonFind
            // 
            this.buttonFind.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonFind.Location = new System.Drawing.Point(435, 1);
            this.buttonFind.Name = "buttonFind";
            this.buttonFind.Size = new System.Drawing.Size(75, 23);
            this.buttonFind.TabIndex = 12;
            this.buttonFind.Text = "Find";
            this.buttonFind.UseVisualStyleBackColor = true;
            this.buttonFind.Click += new System.EventHandler(this.buttonFind_Click);
            // 
            // button2
            // 
            this.button2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button2.Location = new System.Drawing.Point(345, 1);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(84, 23);
            this.button2.TabIndex = 13;
            this.button2.Text = "Project List";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripVersion,
            this.toolStripStatusLabelUserRole,
            this.toolStripSplitButton1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 339);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(596, 22);
            this.statusStrip1.TabIndex = 14;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripVersion
            // 
            this.toolStripVersion.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripVersion.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItemVersion,
            this.ToolStripMenuItemCopyright});
            this.toolStripVersion.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F);
            this.toolStripVersion.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripVersion.Name = "toolStripVersion";
            this.toolStripVersion.Size = new System.Drawing.Size(58, 20);
            this.toolStripVersion.Text = "Copyright";
            // 
            // ToolStripMenuItemVersion
            // 
            this.ToolStripMenuItemVersion.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.ToolStripMenuItemVersion.Name = "ToolStripMenuItemVersion";
            this.ToolStripMenuItemVersion.Size = new System.Drawing.Size(123, 22);
            this.ToolStripMenuItemVersion.Text = "Ver X.Y.Z";
            // 
            // ToolStripMenuItemCopyright
            // 
            this.ToolStripMenuItemCopyright.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.ToolStripMenuItemCopyright.Name = "ToolStripMenuItemCopyright";
            this.ToolStripMenuItemCopyright.Size = new System.Drawing.Size(123, 22);
            this.ToolStripMenuItemCopyright.Text = "Copyright";
            // 
            // toolStripStatusLabelUserRole
            // 
            this.toolStripStatusLabelUserRole.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.toolStripStatusLabelUserRole.Name = "toolStripStatusLabelUserRole";
            this.toolStripStatusLabelUserRole.Size = new System.Drawing.Size(475, 17);
            this.toolStripStatusLabelUserRole.Spring = true;
            this.toolStripStatusLabelUserRole.Text = "UserRole";
            this.toolStripStatusLabelUserRole.TextAlign = System.Drawing.ContentAlignment.BottomRight;
            // 
            // toolStripSplitButton1
            // 
            this.toolStripSplitButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripSplitButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.adminToolStripMenuItem,
            this.manageUsersToolStripMenuItem});
            this.toolStripSplitButton1.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.toolStripSplitButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripSplitButton1.Image")));
            this.toolStripSplitButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripSplitButton1.Name = "toolStripSplitButton1";
            this.toolStripSplitButton1.Size = new System.Drawing.Size(48, 20);
            this.toolStripSplitButton1.Text = "Config";
            this.toolStripSplitButton1.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.toolStripSplitButton1.ButtonClick += new System.EventHandler(this.toolStripSplitButton1_ButtonClick);
            // 
            // adminToolStripMenuItem
            // 
            this.adminToolStripMenuItem.Name = "adminToolStripMenuItem";
            this.adminToolStripMenuItem.Size = new System.Drawing.Size(131, 22);
            this.adminToolStripMenuItem.Text = "Admin";
            this.adminToolStripMenuItem.Click += new System.EventHandler(this.adminToolStripMenuItem_Click);
            // 
            // manageUsersToolStripMenuItem
            // 
            this.manageUsersToolStripMenuItem.Name = "manageUsersToolStripMenuItem";
            this.manageUsersToolStripMenuItem.Size = new System.Drawing.Size(131, 22);
            this.manageUsersToolStripMenuItem.Text = "Manage Users";
            this.manageUsersToolStripMenuItem.Click += new System.EventHandler(this.manageUsersToolStripMenuItem_Click);
            // 
            // buttonRefresh
            // 
            this.buttonRefresh.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonRefresh.Location = new System.Drawing.Point(516, 30);
            this.buttonRefresh.Name = "buttonRefresh";
            this.buttonRefresh.Size = new System.Drawing.Size(75, 23);
            this.buttonRefresh.TabIndex = 15;
            this.buttonRefresh.Text = "Refresh";
            this.buttonRefresh.UseVisualStyleBackColor = true;
            this.buttonRefresh.Visible = false;
            this.buttonRefresh.Click += new System.EventHandler(this.buttonRefresh_Click);
            // 
            // gridControl
            // 
            this.gridControl.AllowCellDrop = false;
            this.gridControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridControl.FilterIsVisible = false;
            this.gridControl.Location = new System.Drawing.Point(1, 27);
            this.gridControl.Margin = new System.Windows.Forms.Padding(0);
            this.gridControl.Name = "gridControl";
            this.gridControl.Size = new System.Drawing.Size(590, 303);
            this.gridControl.TabIndex = 16;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(596, 361);
            this.Controls.Add(this.gridControl);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.buttonFind);
            this.Controls.Add(this.buttonSave);
            this.Controls.Add(this.buttonProjects);
            this.Controls.Add(this.buttonComponents);
            this.Controls.Add(this.buttonShowTasks);
            this.Controls.Add(this.buttonRefresh);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(612, 88);
            this.Name = "MainWindow";
            this.Text = "Project Pal";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainWindow_FormClosing);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion


        private System.Windows.Forms.Button buttonShowTasks;
        private System.Windows.Forms.Button buttonComponents;
        private System.Windows.Forms.Button buttonProjects;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Button buttonFind;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelUserRole;
        private System.Windows.Forms.ToolStripSplitButton toolStripSplitButton1;
        private System.Windows.Forms.ToolStripMenuItem adminToolStripMenuItem;
        private System.Windows.Forms.Button buttonRefresh;
        private System.Windows.Forms.ToolStripDropDownButton toolStripVersion;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItemVersion;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItemCopyright;
        private System.Windows.Forms.ToolStripMenuItem manageUsersToolStripMenuItem;
        private CustomGUIControls.Grid.GridControl gridControl;
    }
}

