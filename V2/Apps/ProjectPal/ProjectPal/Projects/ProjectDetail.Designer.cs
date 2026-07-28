namespace ProjectPal.Projects
{
    partial class ProjectDetail
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProjectDetail));
            this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            this.checkBoxOnlyActiveTasks = new System.Windows.Forms.CheckBox();
            this.labelProjectTitle = new System.Windows.Forms.Label();
            this.toolStripProject = new System.Windows.Forms.ToolStrip();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.radioButtonNone = new System.Windows.Forms.RadioButton();
            this.radioButtonOpen = new System.Windows.Forms.RadioButton();
            this.radioButtonAll = new System.Windows.Forms.RadioButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.labelAttachments = new System.Windows.Forms.Label();
            this.gridControlAttachments = new CustomGUIControls.Grid.GridControl();
            this.checkBoxPrivate = new System.Windows.Forms.CheckBox();
            this.labelPriority = new System.Windows.Forms.Label();
            this.comboBoxPriority = new System.Windows.Forms.ComboBox();
            this.labelProjectText = new System.Windows.Forms.Label();
            this.labelParent = new System.Windows.Forms.Label();
            this.labelParentText = new System.Windows.Forms.Label();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.textBoxDetailedDescription = new System.Windows.Forms.TextBox();
            this.labelDescription = new System.Windows.Forms.Label();
            this.labelOwner = new System.Windows.Forms.Label();
            this.comboBoxNewOwner = new System.Windows.Forms.ComboBox();
            this.buttonDueDate = new System.Windows.Forms.Button();
            this.dateTimePickerDueDate = new System.Windows.Forms.DateTimePicker();
            this.labelDueDate = new System.Windows.Forms.Label();
            this.labelStartDate = new System.Windows.Forms.Label();
            this.dateTimePickerStartDate = new System.Windows.Forms.DateTimePicker();
            this.textBoxEnd = new System.Windows.Forms.TextBox();
            this.labelEndDate = new System.Windows.Forms.Label();
            this.buttonResetStartDate = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            this.panelDependencies = new System.Windows.Forms.Panel();
            this.splitContainerDependencies = new System.Windows.Forms.SplitContainer();
            this.listBoxDependsUpon = new System.Windows.Forms.ListBox();
            this.labeDependUpon = new System.Windows.Forms.Label();
            this.listBoxDependants = new System.Windows.Forms.ListBox();
            this.buttonDependenciesClose = new System.Windows.Forms.Button();
            this.buttonDependencies = new System.Windows.Forms.Button();
            this.labelDependants = new System.Windows.Forms.Label();
            this.textBoxId = new System.Windows.Forms.TextBox();
            this.labelID = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.toolStripProject.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panelDependencies.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerDependencies)).BeginInit();
            this.splitContainerDependencies.Panel1.SuspendLayout();
            this.splitContainerDependencies.Panel2.SuspendLayout();
            this.splitContainerDependencies.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // elementHost1
            // 
            this.elementHost1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.elementHost1.Location = new System.Drawing.Point(6, 48);
            this.elementHost1.Name = "elementHost1";
            this.elementHost1.Size = new System.Drawing.Size(667, 234);
            this.elementHost1.TabIndex = 0;
            this.elementHost1.Text = "elementHost1";
            this.elementHost1.Child = null;
            // 
            // checkBoxOnlyActiveTasks
            // 
            this.checkBoxOnlyActiveTasks.AutoSize = true;
            this.checkBoxOnlyActiveTasks.Checked = true;
            this.checkBoxOnlyActiveTasks.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxOnlyActiveTasks.Location = new System.Drawing.Point(6, 11);
            this.checkBoxOnlyActiveTasks.Name = "checkBoxOnlyActiveTasks";
            this.checkBoxOnlyActiveTasks.Size = new System.Drawing.Size(121, 17);
            this.checkBoxOnlyActiveTasks.TabIndex = 1;
            this.checkBoxOnlyActiveTasks.Text = "Only Active Projects";
            this.checkBoxOnlyActiveTasks.UseVisualStyleBackColor = true;
            this.checkBoxOnlyActiveTasks.CheckedChanged += new System.EventHandler(this.checkBoxOnlyActiveTasks_CheckedChanged);
            // 
            // labelProjectTitle
            // 
            this.labelProjectTitle.AllowDrop = true;
            this.labelProjectTitle.AutoSize = true;
            this.labelProjectTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelProjectTitle.Location = new System.Drawing.Point(81, 26);
            this.labelProjectTitle.MaximumSize = new System.Drawing.Size(500, 22);
            this.labelProjectTitle.Name = "labelProjectTitle";
            this.labelProjectTitle.Size = new System.Drawing.Size(64, 22);
            this.labelProjectTitle.TabIndex = 4;
            this.labelProjectTitle.Text = "label1";
            this.labelProjectTitle.DragDrop += new System.Windows.Forms.DragEventHandler(this.labelProjectTitle_DragDrop);
            this.labelProjectTitle.DragEnter += new System.Windows.Forms.DragEventHandler(this.labelProjectTitle_DragEnter);
            this.labelProjectTitle.DragOver += new System.Windows.Forms.DragEventHandler(this.labelProjectTitle_DragOver);
            this.labelProjectTitle.DragLeave += new System.EventHandler(this.labelProjectTitle_DragLeave);
            this.labelProjectTitle.MouseDown += new System.Windows.Forms.MouseEventHandler(this.labelProjectTitle_MouseDown);
            // 
            // toolStripProject
            // 
            this.toolStripProject.Dock = System.Windows.Forms.DockStyle.None;
            this.toolStripProject.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton1,
            this.toolStripButton2});
            this.toolStripProject.Location = new System.Drawing.Point(0, 0);
            this.toolStripProject.Name = "toolStripProject";
            this.toolStripProject.Size = new System.Drawing.Size(58, 25);
            this.toolStripProject.TabIndex = 7;
            this.toolStripProject.Text = "toolStrip1";
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton1.Text = "Add New Project";
            this.toolStripButton1.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // toolStripButton2
            // 
            this.toolStripButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton2.Image")));
            this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton2.Name = "toolStripButton2";
            this.toolStripButton2.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton2.Text = "Gantt Display";
            this.toolStripButton2.Click += new System.EventHandler(this.toolStripButton2_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.radioButtonNone);
            this.groupBox2.Controls.Add(this.radioButtonOpen);
            this.groupBox2.Controls.Add(this.radioButtonAll);
            this.groupBox2.Location = new System.Drawing.Point(133, 3);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(159, 39);
            this.groupBox2.TabIndex = 8;
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
            this.radioButtonOpen.Location = new System.Drawing.Point(63, 15);
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
            this.radioButtonAll.Location = new System.Drawing.Point(120, 15);
            this.radioButtonAll.Name = "radioButtonAll";
            this.radioButtonAll.Size = new System.Drawing.Size(36, 17);
            this.radioButtonAll.TabIndex = 0;
            this.radioButtonAll.Text = "All";
            this.radioButtonAll.UseVisualStyleBackColor = true;
            this.radioButtonAll.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(3, 3);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.labelAttachments);
            this.splitContainer1.Panel1.Controls.Add(this.gridControlAttachments);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.checkBoxPrivate);
            this.splitContainer1.Panel2.Controls.Add(this.elementHost1);
            this.splitContainer1.Panel2.Controls.Add(this.checkBoxOnlyActiveTasks);
            this.splitContainer1.Panel2.Controls.Add(this.groupBox2);
            this.splitContainer1.Size = new System.Drawing.Size(676, 371);
            this.splitContainer1.SplitterDistance = 70;
            this.splitContainer1.TabIndex = 9;
            // 
            // labelAttachments
            // 
            this.labelAttachments.AllowDrop = true;
            this.labelAttachments.AutoSize = true;
            this.labelAttachments.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.labelAttachments.Location = new System.Drawing.Point(3, -2);
            this.labelAttachments.Margin = new System.Windows.Forms.Padding(0);
            this.labelAttachments.Name = "labelAttachments";
            this.labelAttachments.Size = new System.Drawing.Size(66, 13);
            this.labelAttachments.TabIndex = 10;
            this.labelAttachments.Text = "Attachments";
            // 
            // gridControlAttachments
            // 
            this.gridControlAttachments.AllowDrop = true;
            this.gridControlAttachments.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridControlAttachments.FilterIsVisible = false;
            this.gridControlAttachments.Location = new System.Drawing.Point(6, 11);
            this.gridControlAttachments.Margin = new System.Windows.Forms.Padding(0);
            this.gridControlAttachments.Name = "gridControlAttachments";
            this.gridControlAttachments.Size = new System.Drawing.Size(670, 59);
            this.gridControlAttachments.TabIndex = 0;
            this.gridControlAttachments.DragDrop += new System.Windows.Forms.DragEventHandler(this.gridControlAttachments_DragDrop);
            this.gridControlAttachments.DragEnter += new System.Windows.Forms.DragEventHandler(this.gridControlAttachments_DragEnter);
            this.gridControlAttachments.DragOver += new System.Windows.Forms.DragEventHandler(this.gridControlAttachments_DragOver);
            // 
            // checkBoxPrivate
            // 
            this.checkBoxPrivate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxPrivate.AutoSize = true;
            this.checkBoxPrivate.Checked = true;
            this.checkBoxPrivate.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxPrivate.Location = new System.Drawing.Point(614, 11);
            this.checkBoxPrivate.Name = "checkBoxPrivate";
            this.checkBoxPrivate.Size = new System.Drawing.Size(59, 17);
            this.checkBoxPrivate.TabIndex = 9;
            this.checkBoxPrivate.Text = "Private";
            this.checkBoxPrivate.UseVisualStyleBackColor = true;
            this.checkBoxPrivate.CheckedChanged += new System.EventHandler(this.checkBoxPrivate_CheckedChanged);
            // 
            // labelPriority
            // 
            this.labelPriority.AutoSize = true;
            this.labelPriority.Location = new System.Drawing.Point(72, 8);
            this.labelPriority.Name = "labelPriority";
            this.labelPriority.Size = new System.Drawing.Size(38, 13);
            this.labelPriority.TabIndex = 10;
            this.labelPriority.Text = "Priority";
            // 
            // comboBoxPriority
            // 
            this.comboBoxPriority.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPriority.FormattingEnabled = true;
            this.comboBoxPriority.Location = new System.Drawing.Point(111, 5);
            this.comboBoxPriority.Name = "comboBoxPriority";
            this.comboBoxPriority.Size = new System.Drawing.Size(89, 21);
            this.comboBoxPriority.TabIndex = 9;
            this.comboBoxPriority.SelectedIndexChanged += new System.EventHandler(this.comboBoxPriority_SelectedIndexChanged);
            // 
            // labelProjectText
            // 
            this.labelProjectText.AllowDrop = true;
            this.labelProjectText.AutoSize = true;
            this.labelProjectText.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelProjectText.Location = new System.Drawing.Point(12, 26);
            this.labelProjectText.Name = "labelProjectText";
            this.labelProjectText.Size = new System.Drawing.Size(62, 20);
            this.labelProjectText.TabIndex = 10;
            this.labelProjectText.Text = "Project:";
            // 
            // labelParent
            // 
            this.labelParent.AllowDrop = true;
            this.labelParent.AutoSize = true;
            this.labelParent.Cursor = System.Windows.Forms.Cursors.Hand;
            this.labelParent.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelParent.Location = new System.Drawing.Point(81, 46);
            this.labelParent.Name = "labelParent";
            this.labelParent.Size = new System.Drawing.Size(56, 20);
            this.labelParent.TabIndex = 11;
            this.labelParent.Text = "Parent";
            this.labelParent.Click += new System.EventHandler(this.labelParent_Click);
            this.labelParent.MouseEnter += new System.EventHandler(this.labelParent_MouseEnter);
            this.labelParent.MouseLeave += new System.EventHandler(this.labelParent_MouseLeave);
            // 
            // labelParentText
            // 
            this.labelParentText.AllowDrop = true;
            this.labelParentText.AutoSize = true;
            this.labelParentText.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelParentText.Location = new System.Drawing.Point(12, 46);
            this.labelParentText.Name = "labelParentText";
            this.labelParentText.Size = new System.Drawing.Size(60, 20);
            this.labelParentText.TabIndex = 12;
            this.labelParentText.Text = "Parent:";
            // 
            // splitContainer2
            // 
            this.splitContainer2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer2.Location = new System.Drawing.Point(0, 30);
            this.splitContainer2.Margin = new System.Windows.Forms.Padding(1);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.textBoxDetailedDescription);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.splitContainer1);
            this.splitContainer2.Size = new System.Drawing.Size(682, 382);
            this.splitContainer2.SplitterDistance = 27;
            this.splitContainer2.SplitterWidth = 2;
            this.splitContainer2.TabIndex = 13;
            // 
            // textBoxDetailedDescription
            // 
            this.textBoxDetailedDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxDetailedDescription.Location = new System.Drawing.Point(3, 0);
            this.textBoxDetailedDescription.Multiline = true;
            this.textBoxDetailedDescription.Name = "textBoxDetailedDescription";
            this.textBoxDetailedDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxDetailedDescription.Size = new System.Drawing.Size(679, 27);
            this.textBoxDetailedDescription.TabIndex = 0;
            this.textBoxDetailedDescription.KeyUp += new System.Windows.Forms.KeyEventHandler(this.textBoxDetailedDescription_KeyUp);
            // 
            // labelDescription
            // 
            this.labelDescription.AutoSize = true;
            this.labelDescription.Location = new System.Drawing.Point(2, 10);
            this.labelDescription.Name = "labelDescription";
            this.labelDescription.Size = new System.Drawing.Size(60, 13);
            this.labelDescription.TabIndex = 14;
            this.labelDescription.Text = "Description";
            // 
            // labelOwner
            // 
            this.labelOwner.AutoSize = true;
            this.labelOwner.Location = new System.Drawing.Point(439, 6);
            this.labelOwner.Name = "labelOwner";
            this.labelOwner.Size = new System.Drawing.Size(38, 13);
            this.labelOwner.TabIndex = 15;
            this.labelOwner.Text = "Owner";
            // 
            // comboBoxNewOwner
            // 
            this.comboBoxNewOwner.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxNewOwner.FormattingEnabled = true;
            this.comboBoxNewOwner.Location = new System.Drawing.Point(479, 3);
            this.comboBoxNewOwner.Name = "comboBoxNewOwner";
            this.comboBoxNewOwner.Size = new System.Drawing.Size(83, 21);
            this.comboBoxNewOwner.TabIndex = 16;
            this.comboBoxNewOwner.SelectedIndexChanged += new System.EventHandler(this.comboBoxNewOwner_SelectedIndexChanged);
            // 
            // buttonDueDate
            // 
            this.buttonDueDate.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonDueDate.Location = new System.Drawing.Point(661, 7);
            this.buttonDueDate.Margin = new System.Windows.Forms.Padding(0);
            this.buttonDueDate.Name = "buttonDueDate";
            this.buttonDueDate.Size = new System.Drawing.Size(20, 20);
            this.buttonDueDate.TabIndex = 27;
            this.buttonDueDate.Text = "X";
            this.buttonDueDate.UseVisualStyleBackColor = true;
            this.buttonDueDate.Click += new System.EventHandler(this.button3_Click);
            // 
            // dateTimePickerDueDate
            // 
            this.dateTimePickerDueDate.CustomFormat = "dd-MMM-yyyy";
            this.dateTimePickerDueDate.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePickerDueDate.Location = new System.Drawing.Point(553, 7);
            this.dateTimePickerDueDate.Name = "dateTimePickerDueDate";
            this.dateTimePickerDueDate.Size = new System.Drawing.Size(110, 20);
            this.dateTimePickerDueDate.TabIndex = 26;
            this.dateTimePickerDueDate.CloseUp += new System.EventHandler(this.dateTimePickerDueDate_CloseUp);
            // 
            // labelDueDate
            // 
            this.labelDueDate.AutoSize = true;
            this.labelDueDate.Location = new System.Drawing.Point(527, 9);
            this.labelDueDate.Name = "labelDueDate";
            this.labelDueDate.Size = new System.Drawing.Size(27, 13);
            this.labelDueDate.TabIndex = 25;
            this.labelDueDate.Text = "Due";
            // 
            // labelStartDate
            // 
            this.labelStartDate.AutoSize = true;
            this.labelStartDate.Location = new System.Drawing.Point(214, 9);
            this.labelStartDate.Name = "labelStartDate";
            this.labelStartDate.Size = new System.Drawing.Size(29, 13);
            this.labelStartDate.TabIndex = 28;
            this.labelStartDate.Text = "Start";
            // 
            // dateTimePickerStartDate
            // 
            this.dateTimePickerStartDate.CustomFormat = "dd-MMM-yyyy";
            this.dateTimePickerStartDate.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePickerStartDate.Location = new System.Drawing.Point(245, 7);
            this.dateTimePickerStartDate.Name = "dateTimePickerStartDate";
            this.dateTimePickerStartDate.Size = new System.Drawing.Size(110, 20);
            this.dateTimePickerStartDate.TabIndex = 29;
            this.dateTimePickerStartDate.CloseUp += new System.EventHandler(this.dateTimePickerStartDate_CloseUp);
            // 
            // textBoxEnd
            // 
            this.textBoxEnd.Location = new System.Drawing.Point(409, 7);
            this.textBoxEnd.Name = "textBoxEnd";
            this.textBoxEnd.ReadOnly = true;
            this.textBoxEnd.Size = new System.Drawing.Size(110, 20);
            this.textBoxEnd.TabIndex = 31;
            // 
            // labelEndDate
            // 
            this.labelEndDate.AutoSize = true;
            this.labelEndDate.Location = new System.Drawing.Point(381, 9);
            this.labelEndDate.Name = "labelEndDate";
            this.labelEndDate.Size = new System.Drawing.Size(26, 13);
            this.labelEndDate.TabIndex = 30;
            this.labelEndDate.Text = "End";
            // 
            // buttonResetStartDate
            // 
            this.buttonResetStartDate.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonResetStartDate.Location = new System.Drawing.Point(353, 7);
            this.buttonResetStartDate.Margin = new System.Windows.Forms.Padding(0);
            this.buttonResetStartDate.Name = "buttonResetStartDate";
            this.buttonResetStartDate.Size = new System.Drawing.Size(20, 20);
            this.buttonResetStartDate.TabIndex = 32;
            this.buttonResetStartDate.Text = "R";
            this.toolTip1.SetToolTip(this.buttonResetStartDate, "Reset Start Date to match earliest active task.\r\nPress Ctrl to do this for all ch" +
        "ild projects too.");
            this.buttonResetStartDate.UseVisualStyleBackColor = true;
            this.buttonResetStartDate.Click += new System.EventHandler(this.buttonResetStartDate_Click);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.buttonResetStartDate);
            this.panel1.Controls.Add(this.splitContainer2);
            this.panel1.Controls.Add(this.labelDescription);
            this.panel1.Controls.Add(this.textBoxEnd);
            this.panel1.Controls.Add(this.comboBoxPriority);
            this.panel1.Controls.Add(this.labelEndDate);
            this.panel1.Controls.Add(this.labelPriority);
            this.panel1.Controls.Add(this.dateTimePickerStartDate);
            this.panel1.Controls.Add(this.dateTimePickerDueDate);
            this.panel1.Controls.Add(this.labelStartDate);
            this.panel1.Controls.Add(this.labelDueDate);
            this.panel1.Controls.Add(this.buttonDueDate);
            this.panel1.Location = new System.Drawing.Point(0, 117);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(687, 425);
            this.panel1.TabIndex = 33;
            // 
            // panelDependencies
            // 
            this.panelDependencies.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelDependencies.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelDependencies.Controls.Add(this.splitContainerDependencies);
            this.panelDependencies.Location = new System.Drawing.Point(3, 66);
            this.panelDependencies.Margin = new System.Windows.Forms.Padding(0);
            this.panelDependencies.Name = "panelDependencies";
            this.panelDependencies.Size = new System.Drawing.Size(684, 47);
            this.panelDependencies.TabIndex = 60;
            // 
            // splitContainerDependencies
            // 
            this.splitContainerDependencies.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainerDependencies.Location = new System.Drawing.Point(0, 0);
            this.splitContainerDependencies.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainerDependencies.Name = "splitContainerDependencies";
            // 
            // splitContainerDependencies.Panel1
            // 
            this.splitContainerDependencies.Panel1.Controls.Add(this.listBoxDependsUpon);
            this.splitContainerDependencies.Panel1.Controls.Add(this.labeDependUpon);
            // 
            // splitContainerDependencies.Panel2
            // 
            this.splitContainerDependencies.Panel2.Controls.Add(this.listBoxDependants);
            this.splitContainerDependencies.Panel2.Controls.Add(this.buttonDependenciesClose);
            this.splitContainerDependencies.Panel2.Controls.Add(this.buttonDependencies);
            this.splitContainerDependencies.Panel2.Controls.Add(this.labelDependants);
            this.splitContainerDependencies.Size = new System.Drawing.Size(684, 46);
            this.splitContainerDependencies.SplitterDistance = 339;
            this.splitContainerDependencies.TabIndex = 61;
            // 
            // listBoxDependsUpon
            // 
            this.listBoxDependsUpon.AllowDrop = true;
            this.listBoxDependsUpon.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxDependsUpon.FormattingEnabled = true;
            this.listBoxDependsUpon.Location = new System.Drawing.Point(2, 13);
            this.listBoxDependsUpon.Name = "listBoxDependsUpon";
            this.listBoxDependsUpon.Size = new System.Drawing.Size(312, 30);
            this.listBoxDependsUpon.TabIndex = 63;
            this.listBoxDependsUpon.DragDrop += new System.Windows.Forms.DragEventHandler(this.listBoxDependsUpon_DragDrop);
            this.listBoxDependsUpon.DragEnter += new System.Windows.Forms.DragEventHandler(this.listBoxDependsUpon_DragEnter);
            this.listBoxDependsUpon.DoubleClick += new System.EventHandler(this.listBoxDependency_DoubleClick);
            this.listBoxDependsUpon.KeyUp += new System.Windows.Forms.KeyEventHandler(this.listBoxDependency_KeyUp);
            // 
            // labeDependUpon
            // 
            this.labeDependUpon.AutoSize = true;
            this.labeDependUpon.Location = new System.Drawing.Point(1, 0);
            this.labeDependUpon.Name = "labeDependUpon";
            this.labeDependUpon.Size = new System.Drawing.Size(77, 13);
            this.labeDependUpon.TabIndex = 60;
            this.labeDependUpon.Text = "Deponds upon";
            // 
            // listBoxDependants
            // 
            this.listBoxDependants.AllowDrop = true;
            this.listBoxDependants.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxDependants.FormattingEnabled = true;
            this.listBoxDependants.Location = new System.Drawing.Point(0, 13);
            this.listBoxDependants.Name = "listBoxDependants";
            this.listBoxDependants.Size = new System.Drawing.Size(314, 30);
            this.listBoxDependants.TabIndex = 62;
            this.listBoxDependants.DragDrop += new System.Windows.Forms.DragEventHandler(this.listBoxDependants_DragDrop);
            this.listBoxDependants.DragEnter += new System.Windows.Forms.DragEventHandler(this.listBoxDependants_DragEnter);
            this.listBoxDependants.DoubleClick += new System.EventHandler(this.listBoxDependency_DoubleClick);
            this.listBoxDependants.KeyUp += new System.Windows.Forms.KeyEventHandler(this.listBoxDependency_KeyUp);
            // 
            // buttonDependenciesClose
            // 
            this.buttonDependenciesClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDependenciesClose.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonDependenciesClose.Location = new System.Drawing.Point(301, 12);
            this.buttonDependenciesClose.Margin = new System.Windows.Forms.Padding(0);
            this.buttonDependenciesClose.Name = "buttonDependenciesClose";
            this.buttonDependenciesClose.Size = new System.Drawing.Size(20, 20);
            this.buttonDependenciesClose.TabIndex = 58;
            this.buttonDependenciesClose.Text = "-";
            this.buttonDependenciesClose.UseVisualStyleBackColor = true;
            this.buttonDependenciesClose.Visible = false;
            this.buttonDependenciesClose.Click += new System.EventHandler(this.buttonDependenciesClose_Click);
            // 
            // buttonDependencies
            // 
            this.buttonDependencies.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDependencies.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonDependencies.Location = new System.Drawing.Point(321, -1);
            this.buttonDependencies.Margin = new System.Windows.Forms.Padding(0);
            this.buttonDependencies.Name = "buttonDependencies";
            this.buttonDependencies.Size = new System.Drawing.Size(20, 20);
            this.buttonDependencies.TabIndex = 58;
            this.buttonDependencies.Text = "+";
            this.buttonDependencies.UseVisualStyleBackColor = true;
            this.buttonDependencies.Click += new System.EventHandler(this.buttonDependencies_Click);
            // 
            // labelDependants
            // 
            this.labelDependants.AutoSize = true;
            this.labelDependants.Location = new System.Drawing.Point(1, 0);
            this.labelDependants.Name = "labelDependants";
            this.labelDependants.Size = new System.Drawing.Size(65, 13);
            this.labelDependants.TabIndex = 61;
            this.labelDependants.Text = "Dependants";
            // 
            // textBoxId
            // 
            this.textBoxId.BackColor = System.Drawing.SystemColors.Control;
            this.textBoxId.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBoxId.Location = new System.Drawing.Point(62, 6);
            this.textBoxId.Name = "textBoxId";
            this.textBoxId.ReadOnly = true;
            this.textBoxId.Size = new System.Drawing.Size(46, 13);
            this.textBoxId.TabIndex = 62;
            this.textBoxId.Text = "-";
            // 
            // labelID
            // 
            this.labelID.AutoSize = true;
            this.labelID.Location = new System.Drawing.Point(6, 6);
            this.labelID.Name = "labelID";
            this.labelID.Size = new System.Drawing.Size(54, 13);
            this.labelID.TabIndex = 61;
            this.labelID.Text = "Project ID";
            // 
            // panel2
            // 
            this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.panel2.Controls.Add(this.labelID);
            this.panel2.Controls.Add(this.textBoxId);
            this.panel2.Location = new System.Drawing.Point(580, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(114, 25);
            this.panel2.TabIndex = 63;
            // 
            // ProjectDetail
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(694, 537);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panelDependencies);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.labelParentText);
            this.Controls.Add(this.labelParent);
            this.Controls.Add(this.labelProjectText);
            this.Controls.Add(this.toolStripProject);
            this.Controls.Add(this.labelProjectTitle);
            this.Controls.Add(this.labelOwner);
            this.Controls.Add(this.comboBoxNewOwner);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ProjectDetail";
            this.Text = "ProjectPal: Projects";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ProjectWindow_FormClosed);
            this.toolStripProject.ResumeLayout(false);
            this.toolStripProject.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel1.PerformLayout();
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panelDependencies.ResumeLayout(false);
            this.splitContainerDependencies.Panel1.ResumeLayout(false);
            this.splitContainerDependencies.Panel1.PerformLayout();
            this.splitContainerDependencies.Panel2.ResumeLayout(false);
            this.splitContainerDependencies.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerDependencies)).EndInit();
            this.splitContainerDependencies.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Integration.ElementHost elementHost1;
        private System.Windows.Forms.CheckBox checkBoxOnlyActiveTasks;
        private System.Windows.Forms.Label labelProjectTitle;
        private System.Windows.Forms.ToolStrip toolStripProject;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton radioButtonNone;
        private System.Windows.Forms.RadioButton radioButtonOpen;
        private System.Windows.Forms.RadioButton radioButtonAll;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private CustomGUIControls.Grid.GridControl gridControlAttachments;
        private System.Windows.Forms.Label labelAttachments;
        private System.Windows.Forms.ComboBox comboBoxPriority;
        private System.Windows.Forms.Label labelPriority;
        private System.Windows.Forms.Label labelProjectText;
        private System.Windows.Forms.Label labelParent;
        private System.Windows.Forms.Label labelParentText;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.TextBox textBoxDetailedDescription;
        private System.Windows.Forms.Label labelDescription;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.Label labelOwner;
        private System.Windows.Forms.ComboBox comboBoxNewOwner;
        private System.Windows.Forms.Button buttonDueDate;
        private System.Windows.Forms.DateTimePicker dateTimePickerDueDate;
        private System.Windows.Forms.Label labelDueDate;
        private System.Windows.Forms.Label labelStartDate;
        private System.Windows.Forms.ToolStripButton toolStripButton2;
        private System.Windows.Forms.DateTimePicker dateTimePickerStartDate;
        private System.Windows.Forms.TextBox textBoxEnd;
        private System.Windows.Forms.Label labelEndDate;
        private System.Windows.Forms.CheckBox checkBoxPrivate;
        private System.Windows.Forms.Button buttonResetStartDate;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panelDependencies;
        private System.Windows.Forms.SplitContainer splitContainerDependencies;
        private System.Windows.Forms.ListBox listBoxDependsUpon;
        private System.Windows.Forms.Label labeDependUpon;
        private System.Windows.Forms.ListBox listBoxDependants;
        private System.Windows.Forms.Button buttonDependenciesClose;
        private System.Windows.Forms.Button buttonDependencies;
        private System.Windows.Forms.Label labelDependants;
        private System.Windows.Forms.TextBox textBoxId;
        private System.Windows.Forms.Label labelID;
        private System.Windows.Forms.Panel panel2;
    }
}