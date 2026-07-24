namespace TaskMan
{
    partial class FormFind
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormFind));
            this.textBoxSearchText = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.checkBoxTasks = new System.Windows.Forms.CheckBox();
            this.checkBoxComponents = new System.Windows.Forms.CheckBox();
            this.checkBoxProjects = new System.Windows.Forms.CheckBox();
            this.checkBoxAttachments = new System.Windows.Forms.CheckBox();
            this.buttonFind = new System.Windows.Forms.Button();
            this.gridControlResults = new CustomGUIControls.Grid.GridControl();
            this.checkBoxRemarks = new System.Windows.Forms.CheckBox();
            this.checkBoxClosedItems = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // textBoxSearchText
            // 
            this.textBoxSearchText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxSearchText.Location = new System.Drawing.Point(15, 25);
            this.textBoxSearchText.Name = "textBoxSearchText";
            this.textBoxSearchText.Size = new System.Drawing.Size(613, 20);
            this.textBoxSearchText.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(82, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Enter search list";
            // 
            // checkBoxTasks
            // 
            this.checkBoxTasks.AutoSize = true;
            this.checkBoxTasks.Checked = true;
            this.checkBoxTasks.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxTasks.Location = new System.Drawing.Point(95, 55);
            this.checkBoxTasks.Name = "checkBoxTasks";
            this.checkBoxTasks.Size = new System.Drawing.Size(55, 17);
            this.checkBoxTasks.TabIndex = 2;
            this.checkBoxTasks.Text = "Tasks";
            this.checkBoxTasks.UseVisualStyleBackColor = true;
            // 
            // checkBoxComponents
            // 
            this.checkBoxComponents.AutoSize = true;
            this.checkBoxComponents.Checked = true;
            this.checkBoxComponents.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxComponents.Location = new System.Drawing.Point(156, 55);
            this.checkBoxComponents.Name = "checkBoxComponents";
            this.checkBoxComponents.Size = new System.Drawing.Size(85, 17);
            this.checkBoxComponents.TabIndex = 3;
            this.checkBoxComponents.Text = "Components";
            this.checkBoxComponents.UseVisualStyleBackColor = true;
            // 
            // checkBoxProjects
            // 
            this.checkBoxProjects.AutoSize = true;
            this.checkBoxProjects.Checked = true;
            this.checkBoxProjects.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxProjects.Location = new System.Drawing.Point(247, 55);
            this.checkBoxProjects.Name = "checkBoxProjects";
            this.checkBoxProjects.Size = new System.Drawing.Size(64, 17);
            this.checkBoxProjects.TabIndex = 4;
            this.checkBoxProjects.Text = "Projects";
            this.checkBoxProjects.UseVisualStyleBackColor = true;
            // 
            // checkBoxAttachments
            // 
            this.checkBoxAttachments.AutoSize = true;
            this.checkBoxAttachments.Location = new System.Drawing.Point(387, 55);
            this.checkBoxAttachments.Name = "checkBoxAttachments";
            this.checkBoxAttachments.Size = new System.Drawing.Size(122, 17);
            this.checkBoxAttachments.TabIndex = 7;
            this.checkBoxAttachments.Text = "Search Attachments";
            this.checkBoxAttachments.UseVisualStyleBackColor = true;
            // 
            // buttonFind
            // 
            this.buttonFind.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonFind.Location = new System.Drawing.Point(15, 51);
            this.buttonFind.Name = "buttonFind";
            this.buttonFind.Size = new System.Drawing.Size(75, 23);
            this.buttonFind.TabIndex = 1;
            this.buttonFind.Text = "&Find";
            this.buttonFind.UseVisualStyleBackColor = true;
            this.buttonFind.Click += new System.EventHandler(this.buttonFind_Click);
            // 
            // gridControlResults
            // 
            this.gridControlResults.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridControlResults.FilterIsVisible = false;
            this.gridControlResults.Location = new System.Drawing.Point(13, 81);
            this.gridControlResults.Margin = new System.Windows.Forms.Padding(0);
            this.gridControlResults.Name = "gridControlResults";
            this.gridControlResults.Size = new System.Drawing.Size(618, 0);
            this.gridControlResults.TabIndex = 8;
            // 
            // checkBoxRemarks
            // 
            this.checkBoxRemarks.AutoSize = true;
            this.checkBoxRemarks.Checked = true;
            this.checkBoxRemarks.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxRemarks.Location = new System.Drawing.Point(317, 55);
            this.checkBoxRemarks.Name = "checkBoxRemarks";
            this.checkBoxRemarks.Size = new System.Drawing.Size(68, 17);
            this.checkBoxRemarks.TabIndex = 9;
            this.checkBoxRemarks.Text = "Remarks";
            this.checkBoxRemarks.UseVisualStyleBackColor = true;
            // 
            // checkBoxClosedItems
            // 
            this.checkBoxClosedItems.AutoSize = true;
            this.checkBoxClosedItems.Location = new System.Drawing.Point(515, 55);
            this.checkBoxClosedItems.Name = "checkBoxClosedItems";
            this.checkBoxClosedItems.Size = new System.Drawing.Size(124, 17);
            this.checkBoxClosedItems.TabIndex = 10;
            this.checkBoxClosedItems.Text = "Include Closed Items";
            this.checkBoxClosedItems.UseVisualStyleBackColor = true;
            // 
            // FormFind
            // 
            this.AcceptButton = this.buttonFind;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(640, 86);
            this.Controls.Add(this.checkBoxClosedItems);
            this.Controls.Add(this.checkBoxRemarks);
            this.Controls.Add(this.gridControlResults);
            this.Controls.Add(this.buttonFind);
            this.Controls.Add(this.checkBoxAttachments);
            this.Controls.Add(this.checkBoxProjects);
            this.Controls.Add(this.checkBoxComponents);
            this.Controls.Add(this.checkBoxTasks);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxSearchText);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormFind";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Find";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxSearchText;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox checkBoxTasks;
        private System.Windows.Forms.CheckBox checkBoxComponents;
        private System.Windows.Forms.CheckBox checkBoxProjects;
        private System.Windows.Forms.CheckBox checkBoxAttachments;
        private System.Windows.Forms.Button buttonFind;
        private CustomGUIControls.Grid.GridControl gridControlResults;
        private System.Windows.Forms.CheckBox checkBoxRemarks;
        private System.Windows.Forms.CheckBox checkBoxClosedItems;
    }
}