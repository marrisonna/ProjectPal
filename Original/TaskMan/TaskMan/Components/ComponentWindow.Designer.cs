namespace TaskMan.Components
{
    partial class ComponentWindow
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ComponentWindow));
            this.labelComponentTitle = new System.Windows.Forms.Label();
            this.labelAttachments = new System.Windows.Forms.Label();
            this.labelParentText = new System.Windows.Forms.Label();
            this.labelParent = new System.Windows.Forms.Label();
            this.labelComponentText = new System.Windows.Forms.Label();
            this.labelOwner = new System.Windows.Forms.Label();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.buttonNewComponent = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.gridControlAttachments = new CustomGUIControls.Grid.GridControl();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.radioButtonNone = new System.Windows.Forms.RadioButton();
            this.radioButtonOpen = new System.Windows.Forms.RadioButton();
            this.radioButtonAll = new System.Windows.Forms.RadioButton();
            this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.elementHostArea = new System.Windows.Forms.Integration.ElementHost();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelComponentTitle
            // 
            this.labelComponentTitle.AllowDrop = true;
            this.labelComponentTitle.AutoSize = true;
            this.labelComponentTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelComponentTitle.Location = new System.Drawing.Point(114, 6);
            this.labelComponentTitle.Name = "labelComponentTitle";
            this.labelComponentTitle.Size = new System.Drawing.Size(64, 22);
            this.labelComponentTitle.TabIndex = 5;
            this.labelComponentTitle.Text = "label1";
            this.labelComponentTitle.DragDrop += new System.Windows.Forms.DragEventHandler(this.labelComponentTitle_DragDrop);
            this.labelComponentTitle.DragEnter += new System.Windows.Forms.DragEventHandler(this.labelComponentTitle_DragEnter);
            this.labelComponentTitle.DragOver += new System.Windows.Forms.DragEventHandler(this.labelComponentTitle_DragOver);
            this.labelComponentTitle.DragLeave += new System.EventHandler(this.labelComponentTitle_DragLeave);
            // 
            // labelAttachments
            // 
            this.labelAttachments.AllowDrop = true;
            this.labelAttachments.AutoSize = true;
            this.labelAttachments.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAttachments.Location = new System.Drawing.Point(180, 47);
            this.labelAttachments.Name = "labelAttachments";
            this.labelAttachments.Size = new System.Drawing.Size(97, 17);
            this.labelAttachments.TabIndex = 9;
            this.labelAttachments.Text = "Attachments";
            // 
            // labelParentText
            // 
            this.labelParentText.AllowDrop = true;
            this.labelParentText.AutoSize = true;
            this.labelParentText.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelParentText.Location = new System.Drawing.Point(53, 26);
            this.labelParentText.Name = "labelParentText";
            this.labelParentText.Size = new System.Drawing.Size(60, 20);
            this.labelParentText.TabIndex = 15;
            this.labelParentText.Text = "Parent:";
            // 
            // labelParent
            // 
            this.labelParent.AllowDrop = true;
            this.labelParent.AutoSize = true;
            this.labelParent.Cursor = System.Windows.Forms.Cursors.Hand;
            this.labelParent.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelParent.Location = new System.Drawing.Point(114, 26);
            this.labelParent.Name = "labelParent";
            this.labelParent.Size = new System.Drawing.Size(56, 20);
            this.labelParent.TabIndex = 14;
            this.labelParent.Text = "Parent";
            this.labelParent.Click += new System.EventHandler(this.labelParent_Click);
            this.labelParent.MouseEnter += new System.EventHandler(this.labelParent_MouseEnter);
            this.labelParent.MouseLeave += new System.EventHandler(this.labelParent_MouseLeave);
            // 
            // labelComponentText
            // 
            this.labelComponentText.AllowDrop = true;
            this.labelComponentText.AutoSize = true;
            this.labelComponentText.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelComponentText.Location = new System.Drawing.Point(7, 6);
            this.labelComponentText.Name = "labelComponentText";
            this.labelComponentText.Size = new System.Drawing.Size(106, 20);
            this.labelComponentText.TabIndex = 13;
            this.labelComponentText.Text = "Component:";
            // 
            // labelOwner
            // 
            this.labelOwner.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelOwner.AutoSize = true;
            this.labelOwner.Location = new System.Drawing.Point(54, 49);
            this.labelOwner.Name = "labelOwner";
            this.labelOwner.Size = new System.Drawing.Size(44, 13);
            this.labelOwner.TabIndex = 16;
            this.labelOwner.Text = "Owner: ";
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(923, 519);
            this.tabControl1.TabIndex = 17;
            // 
            // tabPage1
            // 
            this.tabPage1.BackColor = System.Drawing.SystemColors.Control;
            this.tabPage1.Controls.Add(this.buttonNewComponent);
            this.tabPage1.Controls.Add(this.labelComponentText);
            this.tabPage1.Controls.Add(this.labelComponentTitle);
            this.tabPage1.Controls.Add(this.labelParent);
            this.tabPage1.Controls.Add(this.labelOwner);
            this.tabPage1.Controls.Add(this.labelAttachments);
            this.tabPage1.Controls.Add(this.labelParentText);
            this.tabPage1.Controls.Add(this.splitContainer1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(915, 493);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Components";
            // 
            // buttonNewComponent
            // 
            this.buttonNewComponent.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("buttonNewComponent.BackgroundImage")));
            this.buttonNewComponent.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.buttonNewComponent.ImageKey = "(none)";
            this.buttonNewComponent.Location = new System.Drawing.Point(11, 29);
            this.buttonNewComponent.Name = "buttonNewComponent";
            this.buttonNewComponent.Size = new System.Drawing.Size(29, 29);
            this.buttonNewComponent.TabIndex = 17;
            this.toolTip1.SetToolTip(this.buttonNewComponent, "Add New Component");
            this.buttonNewComponent.UseVisualStyleBackColor = true;
            this.buttonNewComponent.Click += new System.EventHandler(this.buttonNewComponent_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(7, 67);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.gridControlAttachments);
            this.splitContainer1.Panel1.Controls.Add(this.groupBox2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.elementHost1);
            this.splitContainer1.Size = new System.Drawing.Size(912, 426);
            this.splitContainer1.SplitterDistance = 92;
            this.splitContainer1.TabIndex = 8;
            // 
            // gridControlAttachments
            // 
            this.gridControlAttachments.AllowDrop = true;
            this.gridControlAttachments.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridControlAttachments.FilterIsVisible = false;
            this.gridControlAttachments.Location = new System.Drawing.Point(94, 3);
            this.gridControlAttachments.Margin = new System.Windows.Forms.Padding(0);
            this.gridControlAttachments.Name = "gridControlAttachments";
            this.gridControlAttachments.Size = new System.Drawing.Size(815, 89);
            this.gridControlAttachments.TabIndex = 0;
            this.gridControlAttachments.DragDrop += new System.Windows.Forms.DragEventHandler(this.gridControlAttachments_DragDrop);
            this.gridControlAttachments.DragEnter += new System.Windows.Forms.DragEventHandler(this.gridControlAttachments_DragEnter);
            this.gridControlAttachments.DragOver += new System.Windows.Forms.DragEventHandler(this.gridControlAttachments_DragOver);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.radioButtonNone);
            this.groupBox2.Controls.Add(this.radioButtonOpen);
            this.groupBox2.Controls.Add(this.radioButtonAll);
            this.groupBox2.Location = new System.Drawing.Point(4, 3);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(83, 81);
            this.groupBox2.TabIndex = 7;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Show Tasks";
            // 
            // radioButtonNone
            // 
            this.radioButtonNone.AutoSize = true;
            this.radioButtonNone.Location = new System.Drawing.Point(6, 15);
            this.radioButtonNone.Name = "radioButtonNone";
            this.radioButtonNone.Size = new System.Drawing.Size(51, 17);
            this.radioButtonNone.TabIndex = 2;
            this.radioButtonNone.Text = "None";
            this.radioButtonNone.UseVisualStyleBackColor = true;
            this.radioButtonNone.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // radioButtonOpen
            // 
            this.radioButtonOpen.AutoSize = true;
            this.radioButtonOpen.Checked = true;
            this.radioButtonOpen.Location = new System.Drawing.Point(6, 38);
            this.radioButtonOpen.Name = "radioButtonOpen";
            this.radioButtonOpen.Size = new System.Drawing.Size(51, 17);
            this.radioButtonOpen.TabIndex = 1;
            this.radioButtonOpen.TabStop = true;
            this.radioButtonOpen.Text = "Open";
            this.radioButtonOpen.UseVisualStyleBackColor = true;
            this.radioButtonOpen.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // radioButtonAll
            // 
            this.radioButtonAll.AutoSize = true;
            this.radioButtonAll.Location = new System.Drawing.Point(6, 61);
            this.radioButtonAll.Name = "radioButtonAll";
            this.radioButtonAll.Size = new System.Drawing.Size(36, 17);
            this.radioButtonAll.TabIndex = 0;
            this.radioButtonAll.Text = "All";
            this.radioButtonAll.UseVisualStyleBackColor = true;
            this.radioButtonAll.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // elementHost1
            // 
            this.elementHost1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.elementHost1.Location = new System.Drawing.Point(3, 3);
            this.elementHost1.Name = "elementHost1";
            this.elementHost1.Size = new System.Drawing.Size(906, 324);
            this.elementHost1.TabIndex = 0;
            this.elementHost1.Text = "elementHost1";
            this.elementHost1.Child = null;
            // 
            // tabPage2
            // 
            this.tabPage2.BackColor = System.Drawing.SystemColors.Control;
            this.tabPage2.Controls.Add(this.elementHostArea);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(915, 493);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Gantt Display";
            this.tabPage2.Enter += new System.EventHandler(this.tabPage2_Enter);
            this.tabPage2.Leave += new System.EventHandler(this.tabPage2_Leave);
            // 
            // elementHostArea
            // 
            this.elementHostArea.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.elementHostArea.Location = new System.Drawing.Point(0, 0);
            this.elementHostArea.Name = "elementHostArea";
            this.elementHostArea.Size = new System.Drawing.Size(915, 493);
            this.elementHostArea.TabIndex = 2;
            this.elementHostArea.Text = "elementHost1";
            this.elementHostArea.Child = null;
            // 
            // ComponentWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(922, 518);
            this.Controls.Add(this.tabControl1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ComponentWindow";
            this.Text = "ProjectPal: Components";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ComponentWindow_FormClosed);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Integration.ElementHost elementHost1;
        private System.Windows.Forms.Label labelComponentTitle;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton radioButtonNone;
        private System.Windows.Forms.RadioButton radioButtonOpen;
        private System.Windows.Forms.RadioButton radioButtonAll;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private CustomGUIControls.Grid.GridControl gridControlAttachments;
        private System.Windows.Forms.Label labelAttachments;
        private System.Windows.Forms.Label labelParentText;
        private System.Windows.Forms.Label labelParent;
        private System.Windows.Forms.Label labelComponentText;
        private System.Windows.Forms.Label labelOwner;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Integration.ElementHost elementHostArea;
        private System.Windows.Forms.Button buttonNewComponent;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}