namespace TaskMan.Tasks
{
    partial class TaskWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TaskWindow));
            this.tabControlTasks = new System.Windows.Forms.TabControl();
            this.tabPageTasks = new System.Windows.Forms.TabPage();
            this.gridControl = new CustomGUIControls.Grid.GridControl();
            this.tabPageGanttDisplay = new System.Windows.Forms.TabPage();
            this.elementHostArea = new System.Windows.Forms.Integration.ElementHost();
            this.tabControlTasks.SuspendLayout();
            this.tabPageTasks.SuspendLayout();
            this.tabPageGanttDisplay.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControlTasks
            // 
            this.tabControlTasks.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControlTasks.Controls.Add(this.tabPageTasks);
            this.tabControlTasks.Controls.Add(this.tabPageGanttDisplay);
            this.tabControlTasks.Location = new System.Drawing.Point(0, 0);
            this.tabControlTasks.Margin = new System.Windows.Forms.Padding(0);
            this.tabControlTasks.Name = "tabControlTasks";
            this.tabControlTasks.Padding = new System.Drawing.Point(0, 0);
            this.tabControlTasks.SelectedIndex = 0;
            this.tabControlTasks.Size = new System.Drawing.Size(1235, 730);
            this.tabControlTasks.TabIndex = 2;
            // 
            // tabPageTasks
            // 
            this.tabPageTasks.Controls.Add(this.gridControl);
            this.tabPageTasks.Location = new System.Drawing.Point(4, 22);
            this.tabPageTasks.Name = "tabPageTasks";
            this.tabPageTasks.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageTasks.Size = new System.Drawing.Size(1227, 704);
            this.tabPageTasks.TabIndex = 0;
            this.tabPageTasks.Text = "Tasks";
            this.tabPageTasks.UseVisualStyleBackColor = true;
            // 
            // gridControl
            // 
            this.gridControl.AllowCellDrop = false;
            this.gridControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridControl.FilterIsVisible = false;
            this.gridControl.Location = new System.Drawing.Point(0, 0);
            this.gridControl.Margin = new System.Windows.Forms.Padding(0);
            this.gridControl.Name = "gridControl";
            this.gridControl.Size = new System.Drawing.Size(1231, 708);
            this.gridControl.TabIndex = 0;
            // 
            // tabPageGanttDisplay
            // 
            this.tabPageGanttDisplay.Controls.Add(this.elementHostArea);
            this.tabPageGanttDisplay.ImageKey = "(none)";
            this.tabPageGanttDisplay.Location = new System.Drawing.Point(4, 22);
            this.tabPageGanttDisplay.Name = "tabPageGanttDisplay";
            this.tabPageGanttDisplay.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageGanttDisplay.Size = new System.Drawing.Size(1227, 712);
            this.tabPageGanttDisplay.TabIndex = 1;
            this.tabPageGanttDisplay.Text = "Gantt Display";
            this.tabPageGanttDisplay.UseVisualStyleBackColor = true;
            this.tabPageGanttDisplay.Enter += new System.EventHandler(this.tabPagePlanDisplay_Enter);
            this.tabPageGanttDisplay.Leave += new System.EventHandler(this.tabPageGanttDisplay_Leave);
            // 
            // elementHostArea
            // 
            this.elementHostArea.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.elementHostArea.Location = new System.Drawing.Point(0, 0);
            this.elementHostArea.Name = "elementHostArea";
            this.elementHostArea.Size = new System.Drawing.Size(1227, 725);
            this.elementHostArea.TabIndex = 1;
            this.elementHostArea.Text = "elementHost1";
            this.elementHostArea.Child = null;
            // 
            // TaskWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1235, 733);
            this.Controls.Add(this.tabControlTasks);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TaskWindow";
            this.Text = "ProjectPal: Tasks List";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.TaskWindow_FormClosed);
            this.tabControlTasks.ResumeLayout(false);
            this.tabPageTasks.ResumeLayout(false);
            this.tabPageGanttDisplay.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private CustomGUIControls.Grid.GridControl gridControl=null;
        private System.Windows.Forms.TabControl tabControlTasks;
        private System.Windows.Forms.TabPage tabPageTasks;
        private System.Windows.Forms.TabPage tabPageGanttDisplay;
        private System.Windows.Forms.Integration.ElementHost elementHostArea;
    }
}