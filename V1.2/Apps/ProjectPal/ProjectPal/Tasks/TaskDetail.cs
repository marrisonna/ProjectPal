using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CustomGUIControls;
using Utils;



namespace ProjectPal.Tasks
{
    public partial class TaskDetail : Form, IView
    {

        static private Dictionary<GUITask, TaskDetail> s_windows = new Dictionary<GUITask, TaskDetail>();
        ViewImpl m_viewImplementation = null;


        static public void ShowDetailWindow(IDisplayItem objectToDisplay)
        {
            GetAndShowDetailWindow(objectToDisplay);
        }

        static public TaskDetail GetAndShowDetailWindow(IDisplayItem objectToDisplay)
        {

            GUITask underlyingObject = objectToDisplay as GUITask;
            if (underlyingObject != null)
            {
                TaskDetail window = null;
                s_windows.TryGetValue(underlyingObject, out window);

                if (window != null && window.IsDisposed)
                {
                    s_windows.Remove(underlyingObject);
                    window = null;
                }

                if (window == null)
                {
                    window = new TaskDetail(underlyingObject);
                    s_windows.Add(underlyingObject, window);
                    try
                    {
                        window.Show();
                    }
                    catch (Exception)
                    {
                        // If we are trying to show a window for a private task
                        // (e.g., if it was listed in a Find window result before it was private,
                        //  then another user made it private and saved it, but the Find window is still open
                        //  and is still showing the task)
                        // then the 'window' will already have been 'Closed' before the 'Show' is called.
                        // Solution: Ignore and move on.
                    }
                }
                else
                {
                    window.Redisplay();
                    window.Activate();
                }
                return window;
            }
            return null;
        }

        void SetPermisions()
        {
            GUITask underlyingObject = m_viewImplementation.FirstItemToDisplay as GUITask;

            bool fullEditAllowed = Permissions.IsAllowed(underlyingObject.Owner, Permissions.EntityType.Task, Permissions.ChangeType.Edit);
            if (!fullEditAllowed)
            {
                textBoxDescription.ReadOnly = true;
                textBoxDescription.BackColor = Utils.Colours.InactiveColour;


                checkedListBoxResources.Enabled = false;
                checkedListBoxResources.BackColor = Utils.Colours.InactiveColour;
                checkedListBoxResources.ForeColor = Color.Black;



                if (comboBoxStatus.Items.Contains(GUITaskColumns.s_statusCancelled) &&
                    comboBoxStatus.Text != GUITaskColumns.s_statusCancelled)
                    comboBoxStatus.Items.Remove(GUITaskColumns.s_statusCancelled);

                if (comboBoxStatus.Items.Contains(GUITaskColumns.s_statusClosed) &&
                    comboBoxStatus.Text != GUITaskColumns.s_statusClosed)
                    comboBoxStatus.Items.Remove(GUITaskColumns.s_statusClosed);

                if (Permissions.IsReadOnly || !underlyingObject.DBTask.Resources.Contains(DBProjectPal.Person.CurrentUser))
                {
                    comboBoxStatus.Enabled = false;
                    comboBoxStatus.BackColor = Utils.Colours.InactiveColour;
                    comboBoxStatus.ForeColor = Color.Black;
                    textBoxDetailedDescription.ReadOnly = true;
                }


                comboBoxNewOwner.Visible = false;

                comboBoxPriority.Enabled = false;
                comboBoxPriority.BackColor = Utils.Colours.InactiveColour;
                comboBoxPriority.ForeColor = Color.Black;

                buttonDueDate.Enabled = false;
                dateTimePickerEndDate.Enabled = false;
                dateTimePickerEndDate.BackColor = Utils.Colours.InactiveColour;



                dateTimePickerRequestedStartDate.Enabled = false;
                dateTimePickerRequestedStartDate.BackColor = Utils.Colours.InactiveColour;



                textBoxEffort.ReadOnly = true;
                textBoxEffort.BackColor = Utils.Colours.InactiveColour;


                textBoxPercentageAllocation.ReadOnly = true;
                textBoxPercentageAllocation.BackColor = Utils.Colours.InactiveColour;

                textBoxComponent.ReadOnly = true;
                textBoxComponent.BackColor = Utils.Colours.InactiveColour;

                buttonComponents.Enabled = false;

                comboBoxTaskType.Enabled = false;
                comboBoxTaskType.BackColor = Utils.Colours.InactiveColour;

                textBoxProjects.ReadOnly = true;
                textBoxProjects.BackColor = Utils.Colours.InactiveColour;

                buttonProject.Enabled = false;

                comboBoxRequestedBy.Enabled = false;
                comboBoxRequestedBy.BackColor = Utils.Colours.InactiveColour;

                radioButtonDuration.Enabled = false;
                radioButtonManDays.Enabled = false;

                if (Permissions.IsPowerUser)
                {
                    checkBoxTentative.Visible = true;
                    checkBoxTentative.Enabled = false;
                }

                if (underlyingObject.Owner == DBProjectPal.Person.DBUser)
                    checkBoxPrivate.Visible = true;

            }
            else
            {
                // Full edit is allowed
                checkBoxTentative.Visible = true;
                checkBoxPrivate.Visible = true;


                comboBoxNewOwner.Visible = true;
                Functions.PopulateComboWithCurrentUsers(comboBoxNewOwner, false);
                if (string.IsNullOrEmpty(underlyingObject.Owner))
                    comboBoxNewOwner.SelectedIndex = 0;
                else
                {
                    string currentUser = DBProjectPal.Person.FindPersonFromDBLogin(underlyingObject.Owner).Name;

                    if (!comboBoxNewOwner.Items.Contains(currentUser))
                        comboBoxNewOwner.Items.Add(currentUser);
                    comboBoxNewOwner.Text = currentUser;
                }
            }

            //
            buttonDelete.Enabled = (Permissions.IsAllowed(underlyingObject.Owner, Permissions.EntityType.Task, Permissions.ChangeType.Delete));

            if (m_firstRedisplay)
                buttonOK.Enabled = false;
        }

        private void Init()
        {
            Init(null);
        }


        private void Init(GUITask underlyingObject)
        {
            m_ignoreEvents = true;
            m_viewImplementation = new ViewImpl(this);
            InitializeComponent();
            treeViewSelector.Visible = false;

            buttonComponentsClose.Location = buttonComponents.Location;
            buttonComponentsClose.Visible = false;

            buttonProjectsClose.Location = buttonProject.Location;
            buttonProjectsClose.Visible = false;

            comboBoxPriority.Items.AddRange(GUITaskColumns.GetComboValues_static(GUITaskColumns.s_Priority).ToArray());
            IList<string> allPossibleRequestors = GUITaskColumns.GetComboValues_static(GUITaskColumns.s_RequestedBy);
            comboBoxRequestedBy.Items.AddRange(allPossibleRequestors.ToArray());

            comboBoxTaskType.Items.AddRange(GUITaskColumns.GetComboValues_static(GUITaskColumns.s_TaskType).ToArray());

            comboBoxStatus.Items.AddRange(GUITaskColumns.GetComboValues_static(GUITaskColumns.s_Status).ToArray());


            HashSet<string> orginalOrderedResources = new HashSet<string>(GUITaskColumns.GetComboValues_static(GUITaskColumns.s_Resources));


            HashSet<string> otherPeopleHash = new HashSet<string>();

            foreach (string person in allPossibleRequestors)
            {
                if (!orginalOrderedResources.Contains(person))
                    otherPeopleHash.Add(person);
            }


            if (underlyingObject != null)
            {
                foreach (string person in underlyingObject.Resources)
                {
                    if (!orginalOrderedResources.Contains(person))
                        otherPeopleHash.Add(person);
                }
            }

            List<string> otherPeople = new List<string>(otherPeopleHash);
            otherPeople.Sort();
            m_orginalOrderedResources = new List<string>(orginalOrderedResources);
            m_orginalOrderedResources.Sort();
            m_orginalOrderedResources.AddRange(otherPeople);


            checkedListBoxResources.Items.AddRange(m_orginalOrderedResources.ToArray());

            gridControlAttachments.SetDoubleClickFunction(Attachments.GUIAttachment.OpenAttachment);
            gridControlAttachments.SetCellDeleteFunction(DeleteAttachment);
            gridControlAttachments.SetCheckCellDeleteFunction(Attachments.GUIAttachment.ConfirmDeleteAttachment);
            gridControlAttachments.SetDefaultSort(Attachments.GUIAttachmentColumns.s_CreateTime, ListSortDirection.Descending);

            gridControlRemarks.SetDoubleClickFunction(Remarks.GUIRemark.OpenRemark);
            gridControlRemarks.SetCellDeleteFunction(DeleteRemark);
            gridControlRemarks.SetCheckCellDeleteFunction(Remarks.GUIRemark.ConfirmDeleteRemark);



        }

        List<string> m_orginalOrderedResources = new List<string>();

        public void DeleteAttachment(CustomGUIControls.IDisplayItem attachment)
        {
            Attachments.GUIAttachment.DeleteAttachment(attachment);
            controlValueChanged(null, null);
            RedisplayAttachments();
        }

        public void DeleteRemark(CustomGUIControls.IDisplayItem remark)
        {
            Remarks.GUIRemark.DeleteRemark(remark);
            ProjectPal.Remarks.GUIRemark guiRemark = remark as ProjectPal.Remarks.GUIRemark;
            if (guiRemark != null)
            {
                guiRemark.SaveToDB();
                Functions.ClearDisplayCaches();
                GUITask underlyingObject = m_viewImplementation.FirstItemToDisplay as GUITask;
                underlyingObject.Redisplay();
            }
        }



        enum WindowMode { Edit, New, Saved };
        private WindowMode m_mode = WindowMode.Edit;

        public TaskDetail(DBProjectPal.Project ownerProject)
        {
            Init();

            DBProjectPal.Task newTask = DBProjectPal.Task.AddNewInstance("", m_affectedComponent);
            newTask.DateAdded = DBAccess.DBObjectBase.DBTime.Date;
            GUITask underlyingObject = GUITask.GetInstanceFromDBTask(newTask);

            m_viewImplementation.AddDisplayItem(underlyingObject);
            underlyingObject.AddView(this);

            m_mode = WindowMode.New;

            textBoxDescription.Visible = labelDescription.Visible = true;
            labelTask.Visible = labelTitle.Visible = false;

            textBoxProjects.Text = ownerProject.FullName;

            comboBoxStatus.Text = ProjectPal.Tasks.GUITaskColumns.s_statusNotStarted;

            labelDateAdded.Text = newTask.DateAdded.ToString("dd-MMM-yyyy");
            labelStatusDate.Text = newTask.StatusDate.ToString("dd-MMM-yyyy");
            string ownerName = DBProjectPal.Person.FindPersonFromDBLogin(newTask.Owner).Name;
            labelOwner.Text = ownerName;
            dateTimePickerEndDate.CustomFormat = " ";
            dateTimePickerRequestedStartDate.CustomFormat = " ";

            comboBoxRequestedBy.Text = !string.IsNullOrEmpty(ownerName) ? ownerName : "Neil";
            comboBoxPriority.Text = ProjectPal.Tasks.GUITaskColumns.s_priortyMed;
            comboBoxTaskType.Text = null;

            gridControlAttachments.SetFilters();
            gridControlRemarks.SetFilters();

            Redisplay();

            m_ignoreEvents = false;

        }


        public TaskDetail(DBProjectPal.Component ownerComponent)
        {
            Init();
            m_affectedComponent = ownerComponent;

            DBProjectPal.Task newTask = DBProjectPal.Task.AddNewInstance("", m_affectedComponent);
            newTask.DateAdded = DBAccess.DBObjectBase.DBTime.Date;
            GUITask underlyingObject = GUITask.GetInstanceFromDBTask(newTask);

            m_viewImplementation.AddDisplayItem(underlyingObject);
            underlyingObject.AddView(this);
            m_mode = WindowMode.New;
            textBoxDescription.Visible = labelDescription.Visible = true;
            labelTask.Visible = labelTitle.Visible = false;

            textBoxComponent.Text = m_affectedComponent.FullName;

            comboBoxStatus.Text = ProjectPal.Tasks.GUITaskColumns.s_statusNotStarted;

            labelDateAdded.Text = newTask.DateAdded.ToString("dd-MMM-yyyy");
            labelStatusDate.Text = newTask.StatusDate.ToString("dd-MMM-yyyy");
            string ownerName = DBProjectPal.Person.FindPersonFromDBLogin(newTask.Owner).Name;
            labelOwner.Text = ownerName;

            dateTimePickerEndDate.CustomFormat = " ";
            dateTimePickerRequestedStartDate.CustomFormat = " ";

            comboBoxRequestedBy.Text = !string.IsNullOrEmpty(ownerName) ? ownerName : "Neil";
            comboBoxPriority.Text = ProjectPal.Tasks.GUITaskColumns.s_priortyMed;
            comboBoxTaskType.Text = null;

            gridControlAttachments.SetFilters();
            gridControlRemarks.SetFilters();


            Redisplay();

            m_ignoreEvents = false;

        }

        public TaskDetail(GUITask underlyingObject)
        {
            Init(underlyingObject);
            m_viewImplementation.AddDisplayItem(underlyingObject);
            underlyingObject.AddView(this);
            Redisplay();
            m_ignoreEvents = false;

        }

        public void WindowClosed()
        {
            m_viewImplementation.WindowClosed();
        }

        public void AddDisplayItem(IDisplayItem itemToDisplay)
        {
            throw new Exception("AddDisplayItem doesn't make sense for TaskDetail");
        }

        public void RemoveDisplayItem(IDisplayItem itemToDisplay)
        {
            m_viewImplementation.RemoveDisplayItem(itemToDisplay);
            WindowClosed();
            Close();

            //throw new Exception("RemoveDisplayItem doesn't make sense for TaskDetail");
        }

        //private GUITask m_underlyingObject = null;

        void SetObject(ref object item, object requiredItem, ref object origItem)
        {
            if (m_firstRedisplay)
                item = requiredItem;
            if (!EditingInProgress || item == origItem)
            {
                item = origItem = requiredItem;
            }
        }

        void SetText(Control control, string requiredText, ref string origTex)
        {
            if (m_firstRedisplay)
                origTex = requiredText;

            if (!EditingInProgress || control.Text == origTex)
            {
                control.Text = origTex = requiredText;

                if(control is LinkLabel)
                {
                    LinkLabel linkControl = control as LinkLabel;

                    if (!string.IsNullOrEmpty(requiredText))
                    {
                        linkControl.Links.Clear();

                        LinkLabel.Link link = new LinkLabel.Link();
                        link.LinkData = requiredText;
                        linkControl.Links.Clear();
                        linkControl.Links.Add(link);
                        linkControl.Text = requiredText;

                        textBoxRefUrl.Visible = false;
                        linkLabelRefUrl.Visible = true;

                    }
                    else
                    {
                        textBoxRefUrl.Visible = !Permissions.IsReadOnly;
                        linkLabelRefUrl.Visible = false;
                        linkLabelRefUrl.Links.Clear();
                        linkControl.Text = null;
                    }
                }
            }
        }

        void SetChecked(CheckBox control, bool requiredState, ref bool? origState)
        {
            if (m_firstRedisplay)
                origState = requiredState;
            if (!EditingInProgress || control.Checked == origState)
            {
                origState = control.Checked = requiredState;
            }
        }


     
        void SetDate(DateTimePicker control, DateTime? requireDate, ref DateTime? origDate)
        {
            if (m_firstRedisplay)
                origDate = requireDate;

            DateTime? controlDate = null;
            if (control.CustomFormat != " ")
                controlDate = control.Value;

            if (!EditingInProgress || controlDate == origDate)
            {
                origDate = requireDate;
                if (!requireDate.HasValue)
                {
                    control.CustomFormat = " ";
                }
                else
                {
                    control.Value = requireDate.Value;
                    control.CustomFormat = "dd-MMM-yyyy";
                }
            }
        }

        private bool m_ignoreEvents = false;

        bool m_firstRedisplay = true;
        string m_origDescription;
        string m_origDetailedDescription;
        string m_origPriority;
        string m_origEffort;
        string m_origPercentageAllocation;
        string m_origRequestedBy;
        string m_origExternalReferenceURL;
        DateTime? m_origEndDate = null;
        string m_origStartDate = null;
        DateTime? m_origEarliestStartDate = null;
        string m_origDateAdded;
        string m_origOwnerName;
        object m_origAffectedComponent = null;
        string m_origTaskType;
        string m_origStatus;
        string m_origUrgency;
        string m_origStatusDate;
        string m_origResourcesCombined = null;
        bool? m_origTentative = null;
        bool? m_origPrivate = null;

        private void Redisplay()
        {
            if (CustomGUIControls.RedisplayManager.Instance.HasItemAlreadyBeenRedisplayed(this))
                return;

            GUITask underlyingObject = null;
            if (m_viewImplementation.ItemsToDisplay.Count > 0)
                underlyingObject = m_viewImplementation.FirstItemToDisplay as GUITask;

            if (underlyingObject == null || underlyingObject.IsDeleted || underlyingObject.DBTask.IsPrivateToAnotherAndHidden)
            {
                Close();
                return;
            }

            bool currentIgnoreEvents = m_ignoreEvents;
            m_ignoreEvents = true;

            if (underlyingObject != null && m_mode != WindowMode.New)
            {
                SetText(textBoxDescription, underlyingObject.Description, ref m_origDescription);
                labelTitle.Text = textBoxDescription.Text;

                SetText(textBoxDetailedDescription, underlyingObject.DetailedDescription, ref m_origDetailedDescription);
                SetText(comboBoxPriority, underlyingObject.Priority, ref m_origPriority);
                SetText(textBoxEffort, underlyingObject.EffortInDays.HasValue ? underlyingObject.EffortInDays.Value.ToString() : null, ref m_origEffort);
                SetText(textBoxPercentageAllocation, underlyingObject.PercentageAllocation.ToString("P0"), ref m_origPercentageAllocation);
                SetText(comboBoxRequestedBy, underlyingObject.RequestedBy, ref m_origRequestedBy);
                SetText(linkLabelRefUrl, underlyingObject.ExternalReferenceURL, ref m_origExternalReferenceURL);

                if (Permissions.IsReadOnly)
                {
                    linkLabelRefUrl.ContextMenu = null;
                    labelRefUrl.ContextMenu = null;
                }

                if (string.IsNullOrEmpty(linkLabelRefUrl.Text))
                {
                    textBoxRefUrl.Visible = !Permissions.IsReadOnly;
                    linkLabelRefUrl.Visible = false;
                }
                else
                {
                    textBoxRefUrl.Visible = false;
                    linkLabelRefUrl.Visible = true;
                }



                SetDate(dateTimePickerEndDate, underlyingObject.EndDate, ref m_origEndDate);
                string startDate = underlyingObject.StartDate.HasValue ?
                    underlyingObject.StartDate.Value.ToString("dd-MMM-yyyy") :
                    "";
                SetText(dateTimePickerStartDate, startDate, ref m_origStartDate);
                SetDate(dateTimePickerRequestedStartDate, underlyingObject.DBTask.EarliestStartDate, ref m_origEarliestStartDate);

                if ((startDate == "" && dateTimePickerRequestedStartDate.CustomFormat == " ") ||
                    (underlyingObject.StartDate == dateTimePickerRequestedStartDate.Value))
                {
                    dateTimePickerStartDate.BackColor = Color.White;
                    dateTimePickerStartDate.ForeColor = Color.Black;
                }
                else
                {
                    dateTimePickerStartDate.BackColor = Color.White;
                    dateTimePickerStartDate.ForeColor = Color.Red;
                }


                SetText(labelDateAdded, underlyingObject.DateAdded.ToString("dd-MMM-yyyy"), ref m_origDateAdded);
                string ownerName = DBProjectPal.Person.FindPersonFromDBLogin(underlyingObject.Owner).Name;
                SetText(labelOwner, ownerName, ref m_origOwnerName);

                {
                    object affectedCompObj = m_affectedComponent;
                    SetObject(ref affectedCompObj, underlyingObject.AffectedComponent, ref m_origAffectedComponent);
                    m_affectedComponent = affectedCompObj as DBProjectPal.Component;
                    textBoxComponent.Text = m_affectedComponent != null ? m_affectedComponent.FullName : "";
                }

                SetText(comboBoxTaskType, underlyingObject.TaskType, ref m_origTaskType);
                SetText(comboBoxStatus, underlyingObject.Status, ref m_origStatus);
                SetText(textBoxUrgency, underlyingObject.Urgency.ToString(), ref m_origUrgency);
                SetText(labelStatusDate, underlyingObject.StatusDate.HasValue ? underlyingObject.StatusDate.Value.ToString("dd-MMM-yyyy") : null, ref m_origStatusDate);

                SetChecked(checkBoxTentative, underlyingObject.DBTask.ResourceAssignmentIsTentative, ref m_origTentative);
                SetChecked(checkBoxPrivate, underlyingObject.DBTask.Private, ref m_origPrivate);


                radioButtonManDays.Checked = underlyingObject.DBTask.EffortType == DBProjectPal.EffortTypeValue.ManDays;
                radioButtonDuration.Checked = underlyingObject.DBTask.EffortType == DBProjectPal.EffortTypeValue.Duration;


                // Do resources
                List<string> currentResourcesSorted = new List<string>(underlyingObject.Resources);
                currentResourcesSorted.Sort();
                string currentResourcesCombined = "";
                foreach (string resourceName in currentResourcesSorted)
                    currentResourcesCombined += resourceName;

                if (currentResourcesCombined != m_origResourcesCombined)
                {
                    m_origResourcesCombined = currentResourcesCombined;
                    IList<string> resources = underlyingObject.Resources;
                    for (int i = 0; i < checkedListBoxResources.Items.Count; i++)
                    {
                        checkedListBoxResources.SetItemChecked(i, resources.Contains(checkedListBoxResources.Items[i]));
                    }
                    LayOutCheckBox(checkedListBoxResources, m_orginalOrderedResources);
                }

                // These cannot be edited
                labelOrigId.Text = underlyingObject.OrigTaskId;
                textBoxId.Text = underlyingObject.TaskId.HasValue ? underlyingObject.TaskId.Value.ToString() : "not set";


                // These will be updated, not updating them is not appropriate
                string project = underlyingObject.Project;
                textBoxProjects.Text = project;




            }


            this.buttonAdd.Enabled = (underlyingObject != null && underlyingObject.DBTask.TaskId >= 0);

            RedisplayAttachments();
            RedisplayRemarks();
            RedisplayDependencies();

            SetPermisions();
            m_ignoreEvents = currentIgnoreEvents;
            m_firstRedisplay = false;
        }

        private void RedisplayAttachments()
        {
            GUITask underlyingObject = m_viewImplementation.FirstItemToDisplay as GUITask;
            if (underlyingObject != null)
            {
                gridControlAttachments.WindowClosed();
                gridControlAttachments.SetColumns(ProjectPal.Attachments.GUIAttachmentColumns.Instance);


                tabPageAttachments.Text = "Attachments (" + underlyingObject.AttachmentCount + ")";


                foreach (DBProjectPal.Attachment currentAttachement in underlyingObject.Attachments)
                {
                    gridControlAttachments.AddDisplayItem(ProjectPal.Attachments.GUIAttachment.GetInstanceFromDBAttachment(currentAttachement));
                }

                gridControlAttachments.ColumnWidth(ProjectPal.Attachments.GUIAttachmentColumns.s_Name, -1, 250);
                gridControlAttachments.ColumnWidth(ProjectPal.Attachments.GUIAttachmentColumns.s_DataType, -1, 60);
                gridControlAttachments.ColumnWidth(ProjectPal.Attachments.GUIAttachmentColumns.s_CreateTime, -1);
                gridControlAttachments.ColumnWidth(ProjectPal.Attachments.GUIAttachmentColumns.s_Size, 60);
                gridControlAttachments.ColumnWidth(ProjectPal.Attachments.GUIAttachmentColumns.s_Owner, -1);

                gridControlAttachments.SetFilters();

                gridControlAttachments.SetDefaultSort(ProjectPal.Attachments.GUIAttachmentColumns.s_CreateTime, ListSortDirection.Descending);
            }
        }

        private void RedisplayDependencies()
        {
            GUITask underlyingObject = m_viewImplementation.FirstItemToDisplay as GUITask;
            if (underlyingObject != null)
            {
                listBoxDependsUpon.Items.Clear();
                List<DBProjectPal.ITaskOrProject> preDependants = new List<DBProjectPal.ITaskOrProject>(underlyingObject.DBTask.ImmediatePreDependencies);
                preDependants.Sort(SortDependenciesByEndDate);

                foreach (DBProjectPal.ITaskOrProject preDependant in preDependants)
                {
                    listBoxDependsUpon.Items.Add(preDependant);//.ObjectDescription);
                }

                ////////

                listBoxDependants.Items.Clear();
                List<DBProjectPal.ITaskOrProject> postDependants = new List<DBProjectPal.ITaskOrProject>(underlyingObject.DBTask.ImmediatePostDependants);
                postDependants.Sort(SortDependenciesByDescription);

                foreach (DBProjectPal.ITaskOrProject postDependant in postDependants)
                {
                    int index = listBoxDependants.Items.Add(postDependant);//.ObjectDescription);
                }
            }
        }

        private int SortDependenciesByDescription(DBProjectPal.ITaskOrProject a, DBProjectPal.ITaskOrProject b)
        {
            return string.Compare(a.ObjectDescription, b.ObjectDescription);
        }

        private int SortDependenciesByEndDate(DBProjectPal.ITaskOrProject a, DBProjectPal.ITaskOrProject b)
        {
            if (b.ExpectedEndDate == a.ExpectedEndDate)
                return 0;
            if (!b.ExpectedEndDate.HasValue)
                return 1;
            if (!a.ExpectedEndDate.HasValue)
                return -1;
            return DateTime.Compare(b.ExpectedEndDate.Value, a.ExpectedEndDate.Value);
        }

        private void RedisplayRemarks()
        {
            GUITask underlyingObject = m_viewImplementation.FirstItemToDisplay as GUITask;
            if (underlyingObject != null)
            {
                gridControlRemarks.WindowClosed();
                gridControlRemarks.SetColumns(ProjectPal.Remarks.GUIRemarkColumns.Instance);

                tabPageRemark.Text = "Remarks (" + underlyingObject.Remarks.Count() + ")";

                foreach (DBProjectPal.Remark currentRemark in underlyingObject.Remarks)
                {
                    gridControlRemarks.AddDisplayItem(ProjectPal.Remarks.GUIRemark.GetInstanceFromDBRemark(currentRemark));
                }

                gridControlRemarks.ColumnWidth(ProjectPal.Remarks.GUIRemarkColumns.s_remark, -1);
                gridControlRemarks.ColumnWidth(ProjectPal.Remarks.GUIRemarkColumns.s_owner, -1, 50);
                gridControlRemarks.ColumnWidth(ProjectPal.Remarks.GUIRemarkColumns.s_createTime, -1);

                gridControlRemarks.SetFilters();

                gridControlRemarks.SetDefaultSort(ProjectPal.Remarks.GUIRemarkColumns.s_createTime, ListSortDirection.Descending);
            }
        }

        public void ShowRemark(ProjectPal.Remarks.GUIRemark guiRemark)
        {
            tabControl1.SelectTab(tabPageRemark);
            gridControlRemarks.Select(guiRemark);
        }

        private bool m_preventRedisplay = false;
        public void Redisplay(IDisplayItem itemToDisplay)
        {

            if (!m_preventRedisplay)
            {
                bool currentIgnoreEvents = m_ignoreEvents;
                m_ignoreEvents = true;

                GUITask underlyingObject = itemToDisplay as GUITask;

                if (m_viewImplementation.ItemsToDisplay.Contains(underlyingObject))
                {
                    if (underlyingObject.IsDeleted || underlyingObject.DBTask.IsPrivateToAnotherAndHidden)
                    {
                        buttonOK.Enabled = false;
                        Close();
                    }
                    else
                        Redisplay();
                }
                m_ignoreEvents = currentIgnoreEvents;
            }

        }

        private void TaskDetail_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (m_mode == WindowMode.Edit)
            {
                if (m_viewImplementation.ItemsToDisplay.Count > 0)
                    s_windows.Remove(m_viewImplementation.FirstItemToDisplay as GUITask);
                WindowClosed();
            }
        }


        private void button3_Click(object sender, EventArgs e)
        {
            dateTimePickerEndDate.CustomFormat = " ";
            m_lastDateChosen = LastDateChosen.EndDate;
            controlValueChanged(sender, e);
        }

        private void dateTimePickerEndDate_CloseUp(object sender, EventArgs e)
        {
            dateTimePickerEndDate.CustomFormat = "dd-MMM-yyyy";
            m_lastDateChosen = LastDateChosen.EndDate;
            controlValueChanged(sender, e);
        }

        private void LayOutCheckBox(object sender, List<string> originalOrder)
        {
            CheckedListBox theCheckBox = sender as CheckedListBox;
            if (theCheckBox != null)
            {
                List<string> checkedItems = new List<string>();
                List<string> uncheckedItems = new List<string>();

                if (originalOrder == null)
                {
                    foreach (string item in theCheckBox.CheckedItems)
                        checkedItems.Add(item);

                    foreach (string item in theCheckBox.Items)
                        if (!checkedItems.Contains(item))
                            uncheckedItems.Add(item);

                    checkedItems.Sort();
                    uncheckedItems.Sort();
                }
                else
                {
                    foreach (string item in originalOrder)
                    {
                        if (theCheckBox.CheckedItems.Contains(item))
                            checkedItems.Add(item);
                        else
                            uncheckedItems.Add(item);
                    }
                }
                theCheckBox.Items.Clear();

                foreach (string checkedItem in checkedItems)
                    theCheckBox.Items.Add(checkedItem, true);
                foreach (string uncheckedItem in uncheckedItems)
                    theCheckBox.Items.Add(uncheckedItem, false);
            }
        }


        private void buttonCancel_Click(object sender, EventArgs e)
        {
            GUITask underlyingObject = m_viewImplementation.FirstItemToDisplay as GUITask;

            if (m_mode == WindowMode.New)
            {
                if (m_viewImplementation.ItemsToDisplay.Count > 0)
                {
                    underlyingObject.DeleteInstance();
                }
            }
            foreach (DBProjectPal.ITaskOrProject item in m_preDependenciesDeleted)
            {
                underlyingObject.DBTask.AddPreDependency(item);
            }
            m_preDependenciesDeleted.Clear();
            foreach (DBProjectPal.ITaskOrProject item in m_postDependenciesDeleted)
            {
                underlyingObject.DBTask.AddPostDependency(item);
            }
            m_postDependenciesDeleted.Clear();

            Functions.ClearDisplayCaches();
            ApplicationProjectPal.Instance.RefreshAllWindows();
            buttonOK.Enabled = false;
            this.Close();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Functions.ClearDisplayCaches();

            GUITask underlyingObject = m_viewImplementation.FirstItemToDisplay as GUITask;
            if (underlyingObject == null)
                throw new Exception("No task underneath TaskDetail Window!");

            if (m_mode == WindowMode.New)
            {
                string title = "Task detail not complete";

                if (m_affectedComponent == null)
                {
                    System.Windows.MessageBox.Show("A Component must be specified", title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrEmpty(textBoxProjects.Text))
                {
                    System.Windows.MessageBox.Show("A Project must be specified", title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrEmpty(textBoxDescription.Text))
                {
                    System.Windows.MessageBox.Show("The task must have a Description", title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrEmpty(comboBoxPriority.Text))
                {
                    System.Windows.MessageBox.Show("The task must have a Priority", title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrEmpty(comboBoxTaskType.Text))
                {
                    System.Windows.MessageBox.Show("The task must have a Task Type", title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrEmpty(comboBoxRequestedBy.Text))
                {
                    System.Windows.MessageBox.Show("The task must have a Requestor", title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

            }

            m_preventRedisplay = true;

            textBoxDescription.Visible = labelDescription.Visible = false;
            labelTask.Visible = labelTitle.Visible = true;

            underlyingObject.Description = textBoxDescription.Text;
            underlyingObject.DetailedDescription = textBoxDetailedDescription.Text;

            // Priority and Status have a dependancy
            bool setStatus = underlyingObject.Status != comboBoxStatus.Text;
            bool setPriority = underlyingObject.Priority != comboBoxPriority.Text;
            if (setStatus)
                underlyingObject.Status = comboBoxStatus.Text;
            if (setPriority)
                underlyingObject.Priority = comboBoxPriority.Text;


            if (radioButtonDuration.Checked)
                underlyingObject.DBTask.EffortType = DBProjectPal.EffortTypeValue.Duration;
            else
                underlyingObject.DBTask.EffortType = DBProjectPal.EffortTypeValue.ManDays;


            underlyingObject.TaskType = comboBoxTaskType.Text;
            underlyingObject.RequestedBy = comboBoxRequestedBy.Text;
            underlyingObject.ExternalReferenceURL = linkLabelRefUrl.Text;

            { // Effort
                double? effort = null;
                try
                {
                    effort = Convert.ToDouble(textBoxEffort.Text);

                }
                catch (Exception)
                {
                    textBoxEffort.Text = underlyingObject.EffortInDays.HasValue ? underlyingObject.EffortInDays.Value.ToString() : null;
                }
                underlyingObject.EffortInDays = effort;
            }

            { // Percentage Allocation
                double percentageAllocation = 1;
                try
                {
                    percentageAllocation = Convert.ToDouble(textBoxPercentageAllocation.Text.Replace("%", "")) / 100;

                }
                catch (Exception)
                {
                    textBoxPercentageAllocation.Text = underlyingObject.PercentageAllocation.ToString("P0");
                }
                underlyingObject.PercentageAllocation = percentageAllocation;
            }

            {  //Resources
                List<string> resources = new List<string>();
                foreach (var item in checkedListBoxResources.CheckedItems)
                {
                    resources.Add(item.ToString());
                }
                underlyingObject.Resources = resources;
            }

            { // Projects
                underlyingObject.Project = textBoxProjects.Text;
            }

            { // Due Date - Requested Start Date
                if (m_lastDateChosen == LastDateChosen.EndDate)
                {
                    if (dateTimePickerEndDate.CustomFormat == " ")
                        underlyingObject.EndDate = null;
                    else
                        underlyingObject.EndDate = dateTimePickerEndDate.Value;
                }
                else if (m_lastDateChosen == LastDateChosen.RequestedStartDate)
                {
                    if (dateTimePickerRequestedStartDate.CustomFormat == " ")
                        underlyingObject.DBTask.StartDate = null;
                    else
                        underlyingObject.DBTask.StartDate = dateTimePickerRequestedStartDate.Value;
                }
                m_lastDateChosen = LastDateChosen.None;
            }

            { // Owner

                DBProjectPal.Person userPerson = DBProjectPal.Person.FindPerson(comboBoxNewOwner.Text);
                if (userPerson != null)
                {
                    string user = userPerson.DBLogin.Trim();

                    if (underlyingObject.Owner != user)
                        underlyingObject.Owner = user;
                }
            }

            { // Tentative
                if (underlyingObject.DBTask.ResourceAssignmentIsTentative != checkBoxTentative.Checked)
                    underlyingObject.DBTask.ResourceAssignmentIsTentative = checkBoxTentative.Checked;
            }

            { // Private
                if (underlyingObject.DBTask.Private != checkBoxPrivate.Checked)
                    underlyingObject.DBTask.Private = checkBoxPrivate.Checked;
            }
            //{ // Start Date
            //    if (dateTimePickerStartDate.CustomFormat == " ")
            //        underlyingObject.StartDate = null;
            //    else
            //        underlyingObject.StartDate = dateTimePickerStartDate.Value;
            //}


            underlyingObject.AffectedComponent = m_affectedComponent;



            //if (m_mode == WindowMode.New)
            //{
            //    m_mode = WindowMode.Saved;
            //    underlyingObject.AffectedComponent = m_affectedComponent;

            //    ProjectPal.Components.GUIComponent.Redisplay(underlyingObject.AffectedComponent);

            //    foreach (string projectName in underlyingObject.Projects)
            //    {
            //        DBProjectPal.Project newProject = DBProjectPal.Project.FindProject(projectName);

            //        ProjectPal.Projects.GUIProject.Redisplay(newProject);
            //    }
            //    Close();
            //}
            //else
            //{

            //    DBProjectPal.Component origAffectedComponent = underlyingObject.AffectedComponent;
            //    underlyingObject.AffectedComponent = m_affectedComponent;


            //    underlyingObject.Redisplay();

            //    if (origAffectedComponent != underlyingObject.AffectedComponent)
            //    {
            //        ProjectPal.Components.GUIComponent.Redisplay(origAffectedComponent);
            //        ProjectPal.Components.GUIComponent.Redisplay(underlyingObject.AffectedComponent);
            //    }


            //}
            buttonOK.Enabled = false;
            m_preventRedisplay = false;

            Functions.ClearDisplayCaches();

            underlyingObject.Redisplay();
            Redisplay();

            ApplicationProjectPal.Instance.RefreshAllWindows();

            if (m_mode == WindowMode.New)
            {
                m_mode = WindowMode.Saved;
                underlyingObject.AffectedComponent = m_affectedComponent;
                buttonOK.Enabled = false;
                Close();
            }



        }

        private void checkedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!m_ignoreEvents)
            {
                LayOutCheckBox(sender, m_orginalOrderedResources);
                controlValueChanged(sender, e);
            }
        }

        private bool EditingInProgress
        {
            get
            {
                return buttonOK.Enabled;
            }
        }


        private void controlValueChanged(object sender, EventArgs e)
        {
            if (!m_ignoreEvents)
                buttonOK.Enabled = true;
        }

        private void buttonComponents_Click(object sender, EventArgs e)
        {
            TreeNode selectedNode = null;
            List<TreeNode> nodes = new List<TreeNode>();
            foreach (DBProjectPal.Component aComponent in DBProjectPal.Component.TopLevelComponents)
            {
                TreeNode thisRoot = new TreeNode(aComponent.Name);
                thisRoot.Tag = aComponent;

                if (aComponent == m_affectedComponent)
                    selectedNode = thisRoot;
                AddComponentToTreeView(thisRoot, aComponent, ref selectedNode);
                nodes.Add(thisRoot);
            }
            nodes.Sort(NodeSort);
            treeViewSelector.Nodes.Clear();
            treeViewSelector.Nodes.AddRange(nodes.ToArray());
            treeViewSelector.Visible = true;
            int extraWidth = 20;
            treeViewSelector.Width = textBoxComponent.Width + extraWidth;
            treeViewSelector.BringToFront();
            //int x = textBoxComponent.Location.X - extraWidth;
            int x = buttonComponents.Location.X - treeViewSelector.Width;
            int y = textBoxComponent.Location.Y;
            treeViewSelector.Location = new Point(x, y);
            buttonComponents.Visible = false;
            buttonComponentsClose.Visible = true;
            buttonComponentsClose.Location = buttonComponents.Location;
            if (selectedNode != null)
            {
                m_ignoreSelect = true;
                treeViewSelector.SelectedNode = selectedNode;
                treeViewSelector.Select();
                m_ignoreSelect = false;
            }

        }
        bool m_ignoreSelect = false;

        private void AddComponentToTreeView(TreeNode root, DBProjectPal.Component theComponent, ref TreeNode selectedNode)
        {
            List<TreeNode> nodes = new List<TreeNode>();
            foreach (DBProjectPal.Component aComponent in theComponent.SubComponents)
            {
                TreeNode thisRoot = new TreeNode(aComponent.Name);
                thisRoot.Tag = aComponent;

                if (aComponent == m_affectedComponent)
                    selectedNode = thisRoot;
                AddComponentToTreeView(thisRoot, aComponent, ref selectedNode);
                nodes.Add(thisRoot);
            }
            nodes.Sort(NodeSort);
            root.Nodes.AddRange(nodes.ToArray());
        }

        private int NodeSort(TreeNode a, TreeNode b)
        {
            return string.Compare(a.Text, b.Text);

        }

        private void treeViewComponents_AfterSelect(object sender, TreeViewEventArgs e)
        {
            //e.Node.Parent
            if (!m_ignoreSelect)
            {

                if (e.Node.Tag as DBProjectPal.Component != null)
                {
                    DBProjectPal.Component selectedComponent = e.Node.Tag as DBProjectPal.Component;

                    m_affectedComponent = selectedComponent;
                    textBoxComponent.Text = m_affectedComponent.FullName;

                    buttonComponents.Visible = true;
                    buttonComponentsClose.Visible = false;

                }
                if (e.Node.Tag as DBProjectPal.Project != null)
                {
                    DBProjectPal.Project selectedProject = e.Node.Tag as DBProjectPal.Project;
                    if (Permissions.IsAllowed(selectedProject.Owner, Permissions.EntityType.Project, Permissions.ChangeType.Edit))
                    {
                        textBoxProjects.Text = selectedProject.FullName;
                    }
                    buttonProject.Visible = true;
                    buttonProjectsClose.Visible = false;

                }
                treeViewSelector.Visible = false;
                controlValueChanged(sender, e);
            }
        }

        DBProjectPal.Component m_affectedComponent;

        private void treeViewComponents_Leave(object sender, EventArgs e)
        {
            treeViewSelector.Visible = false;
            buttonComponents.Visible = true;
            buttonComponentsClose.Visible = false;
        }

        private void buttonComponentsClose_Click(object sender, EventArgs e)
        {
            buttonComponents.Visible = true;
            buttonComponentsClose.Visible = false;

        }

        private void buttonProjects_Click(object sender, EventArgs e)
        {
            List<TreeNode> nodes = new List<TreeNode>();
            foreach (DBProjectPal.Project aProject in DBProjectPal.Project.TopLevelProjects)
            {
                if (aProject.IsHidden)
                    continue;

                if (HasNoPermissionOnThisAndAllSubProjects(aProject))
                    continue;

                TreeNode thisRoot = new TreeNode(aProject.Name);
                thisRoot.Tag = aProject;

                if (!Permissions.IsAllowed(aProject.Owner, Permissions.EntityType.Project, Permissions.ChangeType.Edit))
                    thisRoot.ForeColor = Utils.Colours.InactiveForeGround;

                AddProjectToTreeView(thisRoot, aProject);
                nodes.Add(thisRoot);
            }
            nodes.Sort(NodeSort);
            treeViewSelector.Nodes.Clear();
            treeViewSelector.Nodes.AddRange(nodes.ToArray());
            treeViewSelector.Visible = true;

            int extraWidth = 20;
            treeViewSelector.Width = textBoxProjects.Width + extraWidth;
            //int x = listBoxProjects.Location.X - extraWidth;
            int x = buttonProject.Location.X - treeViewSelector.Width;
            int y = textBoxProjects.Location.Y;
            treeViewSelector.Location = new Point(x, y);
            treeViewSelector.BringToFront();
            buttonProject.Visible = false;
            buttonProjectsClose.Visible = true;
            buttonProjectsClose.Location = buttonProject.Location;
            m_ignoreSelect = true;
            treeViewSelector.Select();
            treeViewSelector.SelectedNode = null;
            m_ignoreSelect = false;
        }

        private bool HasNoPermissionOnThisAndAllSubProjects(DBProjectPal.Project theProject)
        {
            List<DBProjectPal.Project> projectsToCheck = new List<DBProjectPal.Project>(theProject.AllActiveSubProjects);
            projectsToCheck.Add(theProject);
            foreach (DBProjectPal.Project projectToCheck in projectsToCheck)
            {
                if (Permissions.IsAllowed(projectToCheck.Owner, Permissions.EntityType.Project, Permissions.ChangeType.Edit))
                    return false;
            }
            return true;
        }

        private void AddProjectToTreeView(TreeNode root, DBProjectPal.Project theProject)
        {
            List<TreeNode> nodes = new List<TreeNode>();
            foreach (DBProjectPal.Project aProject in theProject.SubProjects)
            {
                if (aProject.IsHidden)
                    continue;

                if (HasNoPermissionOnThisAndAllSubProjects(aProject))
                    continue;

                TreeNode thisRoot = new TreeNode(aProject.Name);
                thisRoot.Tag = aProject;

                if (!Permissions.IsAllowed(aProject.Owner, Permissions.EntityType.Project, Permissions.ChangeType.Edit))
                    thisRoot.ForeColor = Utils.Colours.InactiveForeGround;

                AddProjectToTreeView(thisRoot, aProject);
                nodes.Add(thisRoot);
            }
            nodes.Sort(NodeSort);
            root.Nodes.Clear();
            root.Nodes.AddRange(nodes.ToArray());
        }

        private void buttonProjectsClose_Click(object sender, EventArgs e)
        {
            buttonProject.Visible = true;
            buttonProjectsClose.Visible = false;
        }



        private void TaskDetail_DragEnter(object sender, DragEventArgs e)
        {
            if (m_viewImplementation.ItemsToDisplay.Count > 0)
            {
                GUITask underlyingObject = m_viewImplementation.FirstItemToDisplay as GUITask;

                if ((Permissions.IsAllowed(underlyingObject.DBTask.Owner, Permissions.EntityType.Task, Permissions.ChangeType.Edit) ||
                    (underlyingObject.DBTask.Resources.Contains(DBProjectPal.Person.CurrentUser))) &&
                    Utils.DragDrop.DroppedFiles.IsDropable(e))
                    e.Effect = DragDropEffects.Copy;
            }
        }

        private void TaskDetail_DragDrop(object sender, DragEventArgs e)
        {
            if (m_viewImplementation.ItemsToDisplay.Count > 0)
            {
                Utils.DragDrop.DroppedFiles files = new Utils.DragDrop.DroppedFiles(e);

                bool attachementAdded = false;
                foreach (Utils.DragDrop.DroppedFile file in files.Files)
                {
                    string a = file.FileName;

                    if (DBProjectPal.Attachment.AddNewInstance((m_viewImplementation.FirstItemToDisplay as GUITask).DBTask, file.Title, file.Type, file.From, file.TimeStamp, file.FileContents) != null)
                    {
                        attachementAdded = true;

                        controlValueChanged(null, null);

                        //m_viewImplementation.FirstItemToDisplay.Redisplay();
                        tabControl1.SelectedTab = tabPageAttachments;
                    }
                }
                if (attachementAdded)
                {
                    Functions.ClearDisplayCaches();
                    RedisplayAttachments();
                }
            }
        }

        private void textBoxComponent_DoubleClick(object sender, EventArgs e)
        {
            if (m_affectedComponent != null)
            {
                Components.GUIComponent guiComponent = Components.GUIComponent.GetInstanceFromDBComponent(m_affectedComponent);
                Components.ComponentWindow newWindow = Components.ComponentWindow.GetInstanceFromGUIComponent(guiComponent);
                newWindow.Show();
                newWindow.Focus();

            }
        }

        private void listBoxProjects_DoubleClick(object sender, EventArgs e)
        {
            string selectedItem = textBoxProjects.Text;
            if (selectedItem != null)
            {
                DBProjectPal.Project dbProject = DBProjectPal.Project.FindProject(selectedItem);
                if (dbProject != null)
                {
                    Projects.GUIProject guiProject = Projects.GUIProject.GetInstanceFromDBProject(dbProject);

                    Projects.ProjectDetail.GetAndShowDetailWindow(guiProject);
                }
            }

        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            GUITask underlyingObject = m_viewImplementation.FirstItemToDisplay as GUITask;


            //buttonDelete
            if (Permissions.IsAllowed(underlyingObject.Owner, Permissions.EntityType.Task, Permissions.ChangeType.Delete))
            {
                if (System.Windows.Forms.MessageBox.Show("Are you sure you want to delete this task?", "Delete Task?",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.OK)
                {
                    underlyingObject.DeleteInstance();
                }
            }
            Functions.ClearDisplayCaches();
            ApplicationProjectPal.Instance.RefreshAllWindows();
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            //Point mousePosition = MousePosition;
            ProjectPal.Remarks.RemarkWindow window = new Remarks.RemarkWindow();
            if (window.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                GUITask underlyingObject = m_viewImplementation.FirstItemToDisplay as GUITask;
                if (underlyingObject != null && underlyingObject.DBTask.TaskId >= 0)
                {
                    DBProjectPal.Remark newRemark = DBProjectPal.Remark.AddNewInstance(underlyingObject.DBTask, window.RemarkText);
                    ProjectPal.Remarks.GUIRemark guiRemark = ProjectPal.Remarks.GUIRemark.GetInstanceFromDBRemark(newRemark);
                    gridControlRemarks.AddDisplayItem(guiRemark);
                    guiRemark.SaveToDB();
                    Functions.ClearDisplayCaches();
                    RedisplayRemarks();

                    underlyingObject.Redisplay();

                }
            }
        }

        private void TaskDetail_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_viewImplementation.ItemsToDisplay.Count > 0)
            {
                if (buttonOK.Enabled)
                {
                    if (MessageBox.Show("There are changes to this Task.  Do you want to keep them?", "Change have been made", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                    {
                        buttonOK_Click(sender, null);
                        if (buttonOK.Enabled)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                }

                if (m_mode == WindowMode.New)
                {
                    GUITask underlyingObject = m_viewImplementation.FirstItemToDisplay as GUITask;
                    underlyingObject.DeleteInstance();
                }
            }
        }

        private void checkBoxTentative_CheckedChanged(object sender, EventArgs e)
        {

            if (!m_ignoreEvents)
                buttonOK.Enabled = true;

        }

        private void comboBoxNewOwner_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!m_ignoreEvents)
                buttonOK.Enabled = true;
        }

        public static void RedisplayAll()
        {
            List<TaskDetail> windows = new List<TaskDetail>();
            windows.AddRange(s_windows.Values);

            foreach (TaskDetail currentWindow in windows)
                currentWindow.Redisplay();
        }



        private void checkBoxPrivate_CheckedChanged(object sender, EventArgs e)
        {

            if (!m_ignoreEvents)
                buttonOK.Enabled = true;
        }

        private void buttonDependencies_Click(object sender, EventArgs e)
        {
            buttonDependencies.Visible = false;
            buttonDependenciesClose.Visible = true;
            buttonDependenciesClose.Location = buttonDependencies.Location;

            int extraSize = 143;
            panelMainControls.Height -= extraSize;

            panelMainControls.Location = new Point(panelMainControls.Location.X, panelMainControls.Location.Y + extraSize);

            panelDependencies.Height += extraSize;

        }

        private void buttonDependenciesClose_Click(object sender, EventArgs e)
        {
            buttonDependenciesClose.Visible = false;
            buttonDependencies.Visible = true;

            int extraSize = 143;
            panelMainControls.Height += extraSize;

            panelMainControls.Location = new Point(panelMainControls.Location.X, panelMainControls.Location.Y - extraSize);

            panelDependencies.Height -= extraSize;
        }

        private void listBoxDependency_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                ListBox listboxUsed = (sender as ListBox);
                DBProjectPal.ITaskOrProject itemSelected = listboxUsed.SelectedItem as DBProjectPal.ITaskOrProject;
                if (itemSelected != null)
                {
                    GUITask underlyingObject = m_viewImplementation.FirstItemToDisplay as GUITask;

                    if (listboxUsed == listBoxDependsUpon)
                    {
                        if (Permissions.IsAllowed(underlyingObject.DBTask.Owner, Permissions.EntityType.Task, Permissions.ChangeType.Edit))
                        {
                            underlyingObject.DBTask.RemovePreDependency(itemSelected);
                            m_preDependenciesDeleted.Add(itemSelected);
                        }
                    }
                    else
                    {
                        if ((itemSelected is DBProjectPal.Task &&
                               Permissions.IsAllowed((itemSelected as DBProjectPal.Task).Owner, Permissions.EntityType.Task, Permissions.ChangeType.Edit))
                             ||
                            (itemSelected is DBProjectPal.Project &&
                               Permissions.IsAllowed((itemSelected as DBProjectPal.Project).Owner, Permissions.EntityType.Project, Permissions.ChangeType.Edit))
                            )
                        {
                            underlyingObject.DBTask.RemovePostDependency(itemSelected);
                            m_postDependenciesDeleted.Add(itemSelected);
                        }
                    }

                    if (!m_ignoreEvents)
                        buttonOK.Enabled = true;

                    Functions.ClearDisplayCaches();
                    ApplicationProjectPal.Instance.RefreshAllWindows();
                }
            }
        }

        List<DBProjectPal.ITaskOrProject> m_preDependenciesDeleted = new List<DBProjectPal.ITaskOrProject>();
        List<DBProjectPal.ITaskOrProject> m_postDependenciesDeleted = new List<DBProjectPal.ITaskOrProject>();

        private void listBoxDependency_DoubleClick(object sender, EventArgs e)
        {
            ListBox listboxUsed = (sender as ListBox);
            DBProjectPal.ITaskOrProject itemSelected = listboxUsed.SelectedItem as DBProjectPal.ITaskOrProject;
            if (itemSelected != null)
            {
                if (itemSelected is DBProjectPal.Task)
                {
                    GUITask theGuiTask = GUITask.GetInstanceFromDBTask((DBProjectPal.Task)itemSelected);
                    ProjectPal.Tasks.TaskDetail.ShowDetailWindow(theGuiTask);
                }

                if (itemSelected is DBProjectPal.Project)
                {
                    Projects.GUIProject guiProject = Projects.GUIProject.GetInstanceFromDBProject((DBProjectPal.Project)itemSelected);
                    Projects.ProjectDetail.GetAndShowDetailWindow(guiProject);
                }

            }
        }


        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBoxDescription.Visible = labelDescription.Visible = true;
            labelTask.Visible = labelTitle.Visible = false;
        }

        private void labelTitle_MouseDown(object sender, MouseEventArgs e)
        {
            bool ctrlButtonDown =
                        System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) ||
                        System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl);


            DragDropEffects titleDragEffects = ctrlButtonDown ?
                                        DragDropEffects.Link :
                                        DragDropEffects.Move;

            GUITask underlyingObject = m_viewImplementation.FirstItemToDisplay as GUITask;

            if (e.Button == System.Windows.Forms.MouseButtons.Left &&
                Permissions.IsAllowed(underlyingObject.DBTask.Owner, Permissions.EntityType.Task, Permissions.ChangeType.Edit))
            {
                DataObject data = Utils.DragDrop.DragHelper.SetDraggedObject(underlyingObject);



                DoDragDrop(data, titleDragEffects);
            }
        }





        private void listBoxDependsUpon_DragDrop(object sender, DragEventArgs e)
        {
            object draggedObject = Functions.ToGUIObjectIfPossible(Utils.DragDrop.DragHelper.DraggedObject);

            ProjectPal.Tasks.GUITask draggedTask = draggedObject as ProjectPal.Tasks.GUITask;
            if (draggedTask != null)
            {
                GUITask underlyingObject = m_viewImplementation.FirstItemToDisplay as GUITask;

                DBProjectPal.Task postTask = underlyingObject.DBTask;
                DBProjectPal.Task preTask = draggedTask.DBTask;
                if (preTask == postTask)
                    return;

                if (postTask.HasPostDependency(preTask))
                {
                    MessageBox.Show("Circular dependencies are not allowed", "Circular Dependency", MessageBoxButtons.OK);
                }
                else
                {
                    preTask.AddPostDependency(postTask);
                    Functions.ClearDisplayCaches();
                    ApplicationProjectPal.Instance.RefreshAllWindows();
                }

            }
            ProjectPal.Projects.GUIProject draggedProject = draggedObject as ProjectPal.Projects.GUIProject;
            if (draggedProject != null)
            {
                GUITask underlyingObject = m_viewImplementation.FirstItemToDisplay as GUITask;

                DBProjectPal.Task postTask = underlyingObject.DBTask;
                DBProjectPal.Project preProject = draggedProject.DBProject;


                if (postTask.HasPostDependency(preProject))
                {
                    MessageBox.Show("Circular dependencies are not allowed", "Circular Dependency not allowed", MessageBoxButtons.OK);
                }
                else
                {
                    if (preProject.AllActiveTasks.Contains(postTask))
                    {
                        MessageBox.Show("Cannot make a Sub Task dependant on parent Project", "Parent Project/Sub Task Dependency not allowed", MessageBoxButtons.OK);
                    }
                    else
                    {
                        preProject.AddPostDependency(postTask);
                        Functions.ClearDisplayCaches();
                        ApplicationProjectPal.Instance.RefreshAllWindows();
                    }
                }
            }
        }

        private void listBoxDependsUpon_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;
            object draggedObject = Functions.ToGUIObjectIfPossible(Utils.DragDrop.DragHelper.DraggedObject);

            ProjectPal.Tasks.GUITask draggedTask = draggedObject as ProjectPal.Tasks.GUITask;
            ProjectPal.Projects.GUIProject draggedProject = draggedObject as ProjectPal.Projects.GUIProject;
            if (draggedTask != null || draggedProject != null)
            {

                GUITask underlyingObject = m_viewImplementation.FirstItemToDisplay as GUITask;

                if (Permissions.IsAllowed(underlyingObject.Owner, Permissions.EntityType.Task, Permissions.ChangeType.Edit))
                {
                    if ((e.AllowedEffect | DragDropEffects.Link) != 0)
                        e.Effect = DragDropEffects.Link;
                }
            }
        }

        private void listBoxDependants_DragDrop(object sender, DragEventArgs e)
        {
            object draggedObject = Functions.ToGUIObjectIfPossible(Utils.DragDrop.DragHelper.DraggedObject);

            ProjectPal.Tasks.GUITask draggedTask = draggedObject as ProjectPal.Tasks.GUITask;
            if (draggedTask != null)
            {
                GUITask underlyingObject = m_viewImplementation.FirstItemToDisplay as GUITask;

                DBProjectPal.Task preTask = underlyingObject.DBTask;
                DBProjectPal.Task postTask = draggedTask.DBTask;
                if (preTask == postTask)
                    return;

                if (postTask.HasPostDependency(preTask))
                {
                    MessageBox.Show("Circular dependencies are not allowed", "Circular Dependency", MessageBoxButtons.OK);
                }
                else
                {
                    preTask.AddPostDependency(postTask);
                    Functions.ClearDisplayCaches();
                    ApplicationProjectPal.Instance.RefreshAllWindows();
                }

            }

            ProjectPal.Projects.GUIProject draggedProject = draggedObject as ProjectPal.Projects.GUIProject;
            if (draggedProject != null)
            {
                GUITask underlyingObject = m_viewImplementation.FirstItemToDisplay as GUITask;

                DBProjectPal.Task preTask = underlyingObject.DBTask;
                DBProjectPal.Project postProject = draggedProject.DBProject;


                if (postProject.HasPostDependency(preTask))
                {
                    MessageBox.Show("Circular dependencies are not allowed", "Circular Dependency", MessageBoxButtons.OK);
                }
                else
                {
                    if (postProject.AllActiveTasks.Contains(preTask))
                    {
                        MessageBox.Show("Cannot make a parent Project dependant on a Sub Task", "Parent Project/Sub Task Dependency not allowed", MessageBoxButtons.OK);

                    }
                    else
                    {
                        preTask.AddPostDependency(postProject);
                        Functions.ClearDisplayCaches();
                        ApplicationProjectPal.Instance.RefreshAllWindows();
                    }
                }
            }
        }

        private void listBoxDependants_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;
            object draggedObject = Functions.ToGUIObjectIfPossible(Utils.DragDrop.DragHelper.DraggedObject);

            ProjectPal.Tasks.GUITask draggedTask = draggedObject as ProjectPal.Tasks.GUITask;
            if (draggedTask != null)
            {
                if (Permissions.IsAllowed(draggedTask.Owner, Permissions.EntityType.Task, Permissions.ChangeType.Edit))
                {
                    if ((e.AllowedEffect | DragDropEffects.Link) != 0)
                        e.Effect = DragDropEffects.Link;
                }
            }
            ProjectPal.Projects.GUIProject draggedProject = draggedObject as ProjectPal.Projects.GUIProject;
            if (draggedProject != null)
            {
                if (Permissions.IsAllowed(draggedProject.Owner, Permissions.EntityType.Project, Permissions.ChangeType.Edit))
                {
                    if ((e.AllowedEffect | DragDropEffects.Link) != 0)
                        e.Effect = DragDropEffects.Link;
                }
            }
        }

        private void dateTimePickerRequestedStartDate_CloseUp(object sender, EventArgs e)
        {
            dateTimePickerRequestedStartDate.CustomFormat = "dd-MMM-yyyy";
            m_lastDateChosen = LastDateChosen.RequestedStartDate;
            controlValueChanged(sender, e);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            dateTimePickerRequestedStartDate.CustomFormat = " ";
            m_lastDateChosen = LastDateChosen.RequestedStartDate;
            controlValueChanged(sender, e);


        }

        private enum LastDateChosen { None, EndDate, RequestedStartDate }
        LastDateChosen m_lastDateChosen = LastDateChosen.None;



        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Label theLabel = this.contextMenuStripTitle.SourceControl as Label;
            if (theLabel != null)
                Clipboard.SetText(theLabel.Text);
        }

        private void dateTimePickerStartDate_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
        }

        private void linkLabelRefUrl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // System.Diagnostics.Process.Start(e.Link.LinkData as string);
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            // Edit
            textBoxRefUrl.Visible = true;
            linkLabelRefUrl.Visible = false;

            textBoxRefUrl.Text = linkLabelRefUrl.Text;
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            // Copy

            if (!string.IsNullOrEmpty(linkLabelRefUrl.Text))
                Clipboard.SetText(linkLabelRefUrl.Text);
        }

        private void textBoxRefUrl_Leave(object sender, EventArgs e)
        {
            if (textBoxRefUrl.Text.Length > 0)
            {
                textBoxRefUrl.Visible = false;
                linkLabelRefUrl.Visible = true;

                LinkLabel.Link link = new LinkLabel.Link();
                link.LinkData = textBoxRefUrl.Text;
                linkLabelRefUrl.Links.Clear();
                linkLabelRefUrl.Links.Add(link);
                linkLabelRefUrl.Text = textBoxRefUrl.Text;

            }
            else
            {
                textBoxRefUrl.Visible = !Permissions.IsReadOnly;
                linkLabelRefUrl.Visible = false;
                linkLabelRefUrl.Links.Clear();
                linkLabelRefUrl.Text = null;
            }

            if (!m_ignoreEvents)
                buttonOK.Enabled = true;
        }

        private void linkLabelRefUrl_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (linkLabelRefUrl.Links.Count > 0)
                    System.Diagnostics.Process.Start(linkLabelRefUrl.Links[0].LinkData as string);
            }
        }



    }
}
