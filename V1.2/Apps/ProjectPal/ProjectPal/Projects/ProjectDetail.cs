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

namespace ProjectPal.Projects
{
    public partial class ProjectDetail : Form, IView
    {


        ViewImpl m_viewImplementation = null;

        private static Dictionary<DBProjectPal.Project, ProjectDetail> s_allInstances = new Dictionary<DBProjectPal.Project, ProjectDetail>();


        private static ProjectDetail m_topLevelWindow = null;

        static public ProjectDetail GetAndShowDetailWindow(GUIProject rootComponent)
        {
            bool newWindow = false;
            ProjectDetail window;
            if (rootComponent == null)
            {
                if (m_topLevelWindow == null)
                {
                    m_topLevelWindow = new ProjectDetail(null);
                    window = m_topLevelWindow;
                    newWindow = true;
                }
                else
                    window = m_topLevelWindow;
            }
            else if (!s_allInstances.TryGetValue(rootComponent.DBProject, out window))
            {
                newWindow = true;
                window = new ProjectDetail(rootComponent);
            }

            if (newWindow)
            {
                window.Show();
            }
            else
            {
                window.Redisplay();
                window.Activate();
            }
            return window;


        }



        private bool m_duringInit = false;
        private bool m_duringRedisplay = false;


        private ProjectDetail(GUIProject rootProject)
        {
            m_duringInit = true;

            InitializeComponent();


            if (rootProject != null)
            {
                s_allInstances.Add(rootProject.DBProject, this);
                m_viewImplementation = new ViewImpl(this);
                m_viewImplementation.AddDisplayItem(rootProject);
                rootProject.AddView(this);
            }


            m_theProjectsStackControl = new ProjectStackControl(ProjectPal.Tasks.TaskDetail.ShowDetailWindow);
            m_theProjectsStackControl.Width = elementHost1.Width;
            elementHost1.Child = m_theProjectsStackControl;
            m_rootProject = rootProject != null ? rootProject.DBProject : null;

            gridControlAttachments.SetDoubleClickFunction(Attachments.GUIAttachment.OpenAttachment);
            gridControlAttachments.SetCellDeleteFunction(Attachments.GUIAttachment.DeleteAttachment);
            gridControlAttachments.SetCheckCellDeleteFunction(Attachments.GUIAttachment.ConfirmDeleteAttachment);

            toolStripButton1.Enabled = Permissions.IsAllowed(RootOwner, Permissions.EntityType.Project, Permissions.ChangeType.Create);


            comboBoxPriority.Items.AddRange(GUIProjectColumns.GetComboValues_static(GUIProjectColumns.s_Priority).ToArray());
            if (rootProject != null)
            {
                comboBoxPriority.Text = rootProject.Priority;


            }



            dateTimePickerDueDate.CustomFormat = " ";

            bool isEditAllowed = Permissions.IsAllowed(RootOwner, Permissions.EntityType.Project, Permissions.ChangeType.Edit);
            if (!isEditAllowed)
            {
                comboBoxPriority.Enabled = false;
                comboBoxNewOwner.Enabled = false;

                //textBoxDetailedDescription.Enabled = false;
                textBoxDetailedDescription.ReadOnly = true;
                dateTimePickerDueDate.Enabled = false;
                buttonDueDate.Visible = false;
            }

            Redisplay();
            if (m_rootProject == null)
            {
                radioButtonNone.Checked = true;
                buttonResetStartDate.Visible = false;
                panelDependencies.Visible = false;

                int extraSize = panelDependencies.Height;
                panel1.Height += extraSize;

                panel1.Location = new Point(panel1.Location.X, panel1.Location.Y - extraSize);


            }


            Functions.PopulateComboWithCurrentUsers(comboBoxNewOwner, false);
            if (RootOwner == null)
            {
                if (comboBoxNewOwner.Items.Count > 0)
                    comboBoxNewOwner.SelectedIndex = 0;
            }
            else
            {
                string currentUser = DBProjectPal.Person.FindPersonFromDBLogin(RootOwner).Name;
                if (!comboBoxNewOwner.Items.Contains(currentUser))
                    comboBoxNewOwner.Items.Add(currentUser);
                comboBoxNewOwner.Text = currentUser;
            }


            checkBoxOnlyActiveTasks.Select();

            m_duringInit = false;
        }

        private string RootOwner
        {
            get
            {
                return m_rootProject == null ? null : m_rootProject.Owner;
            }
        }

        public static void RedisplayAll()
        {
            List<ProjectDetail> windows = new List<ProjectDetail>();
            if (m_topLevelWindow != null)
                windows.Add(m_topLevelWindow);
            windows.AddRange(s_allInstances.Values);

            foreach (ProjectDetail currentWindow in windows)
                currentWindow.Redisplay();
        }


        public bool ActiveProjectsOnlyFn()
        {
            return checkBoxOnlyActiveTasks.Checked;
        }

        public void WindowClosed()
        {
            m_viewImplementation.WindowClosed();
        }

        public void AddDisplayItem(IDisplayItem itemToDisplay)
        {
            throw new Exception("AddDisplayItem doesn't make sense for ProjectDetail");
        }

        public void RemoveDisplayItem(IDisplayItem itemToDisplay)
        {
            m_viewImplementation.RemoveDisplayItem(itemToDisplay);
            WindowClosed();
            Close();
        }

        public void Redisplay(IDisplayItem itemToDisplay)
        {
            if (CustomGUIControls.RedisplayManager.Instance.HasItemAlreadyBeenRedisplayed(this, itemToDisplay))
                return;

            GUIProject underlyingObject = itemToDisplay as GUIProject;

            if (m_viewImplementation.ItemsToDisplay.Contains(underlyingObject))
            {
                if (m_rootProject != null)
                {
                    comboBoxPriority.Text = underlyingObject.Priority;
                }
                Redisplay();
            }
        }

        private void Redisplay()
        {
            //m_theProjectsStackControl.RemoveChildren();

            if (CustomGUIControls.RedisplayManager.Instance.HasItemAlreadyBeenRedisplayed(this))
                return;

            try
            {
                m_duringRedisplay = true;
                List<DBProjectPal.Project> projects = null;
                bool openSubProjects = false;
                bool activeProjectsOnly = OnlyOpenActiveTasks;
                if (m_rootProject == null)
                {
                    List<DBProjectPal.Project> allProjects = new List<DBProjectPal.Project>(DBProjectPal.Project.TopLevelProjects);

                    projects = new List<DBProjectPal.Project>();
                    foreach (DBProjectPal.Project currentProject in allProjects)
                    {
                        if (currentProject.IsHidden)
                            continue;

                        if (!activeProjectsOnly || currentProject.IsActive || currentProject.HasNeverBeenSaved)
                            projects.Add(currentProject);
                    }

                    labelProjectTitle.Text = "Top Level Projects";

                    this.Text = "ProjectPal: Project - " + "Top Level Projects";
                    labelParentText.Visible = labelProjectText.Visible = labelParent.Visible = false;
                    splitContainer1.Panel1MinSize = 0;
                    splitContainer1.SplitterDistance = 0;

                    splitContainer2.Panel1MinSize = 0;
                    splitContainer2.SplitterDistance = 0;

                    comboBoxPriority.Visible = false;
                    comboBoxNewOwner.Visible = false;

                    dateTimePickerDueDate.Visible = false;
                    labelDueDate.Visible = false;
                    buttonDueDate.Visible = false;

                    labelStartDate.Visible = false;

                    labelPriority.Visible = false;
                    labelOwner.Visible = false;

                    labelDescription.Visible = false;
                    labelAttachments.Visible = false;

                    labelStartDate.Visible = false;
                    dateTimePickerStartDate.Visible = false;

                    labelEndDate.Visible = false;
                    textBoxEnd.Visible = false;

                    checkBoxPrivate.Visible = false;

                    textBoxId.Visible = false;
                    labelID.Visible = false;
                }
                else
                {
                    textBoxId.Text = m_rootProject.ProjectId >= 0 ?
                        m_rootProject.ProjectId.ToString() :
                        "Not set";


                    if (m_rootProject.IsDeleted || m_rootProject.IsPrivateToAnotherAndHidden)
                    {
                        Close();
                        return;
                    }

                    GUIProject rootProject = GUIProject.GetInstanceFromDBProject(m_rootProject);

                    DBProjectPal.Person owner = DBProjectPal.Person.FindPersonFromDBLogin(m_rootProject.Owner);
                    string personName = owner == null ? "" : owner.Name;

                    labelOwner.Text = "Owner: " + personName;


                    if (m_rootProject.DueDate.HasValue)
                    {
                        dateTimePickerDueDate.Value = m_rootProject.DueDate.Value;
                        dateTimePickerDueDate.CustomFormat = "dd-MMM-yyyy";
                    }
                    else
                        dateTimePickerDueDate.CustomFormat = " ";

                    dateTimePickerStartDate.Text = m_rootProject.StartDate.ToString("dd-MMM-yyyy");
                    textBoxEnd.Text = m_rootProject.EndDate.HasValue ? m_rootProject.EndDate.Value.ToString("dd-MMM-yyyy") : null;



                    textBoxDetailedDescription.Text = m_rootProject.DetailedDescription;
                    projects = new List<DBProjectPal.Project>();
                    projects.Add(m_rootProject);
                    labelProjectTitle.Text = m_rootProject.Name;
                    this.Text = "ProjectPal: Project - " + m_rootProject.Name;
                    labelParent.Text = "(" + (m_rootProject.Parent != null ? m_rootProject.FullParentName : "Top Level Projects") + ")";
                    labelParentText.Visible = labelProjectText.Visible = labelParent.Visible = true;
                    openSubProjects = true;

                    gridControlAttachments.WindowClosed();
                    gridControlAttachments.SetColumns(ProjectPal.Attachments.GUIAttachmentColumns.Instance);
                    foreach (DBProjectPal.Attachment currentAttachement in m_rootProject.Attachments)
                    {
                        gridControlAttachments.AddDisplayItem(ProjectPal.Attachments.GUIAttachment.GetInstanceFromDBAttachment(currentAttachement));
                    }
                    gridControlAttachments.SetFilters(true);


                    gridControlAttachments.SetDefaultSort(ProjectPal.Attachments.GUIAttachmentColumns.s_CreateTime, ListSortDirection.Descending);



                    comboBoxPriority.Text = rootProject.Priority;

                    checkBoxPrivate.Checked = m_rootProject.Private;

                    if (m_rootProject.Owner == DBProjectPal.Person.DBUser)
                        checkBoxPrivate.Visible = true;
                    else
                        checkBoxPrivate.Visible = false;

                    RedisplayDependencies();

                }
                projects.Sort(GUIProject.SortProjectsByPriority);
                //projects.Sort(GUIProject.SortProjectName);

                int itemCount = Math.Max(projects.Count, m_theProjectsStackControl.ChildCount);

                for (int index = 0; index < itemCount; index++)
                {
                    DBProjectPal.Project currentProjectToBeDisplayed = index < projects.Count ? projects[index] : null;
                    GUIProject currentProjectOnDisplay = index < m_theProjectsStackControl.ChildCount ? m_theProjectsStackControl.ProjectAt(index).ProjectItemOnDisplay : null;

                    // Delete on display item
                    if (currentProjectToBeDisplayed == null || (currentProjectOnDisplay != null && GUIProject.SortProjectsByPriority(currentProjectToBeDisplayed, currentProjectOnDisplay) > 0))
                    {
                        m_theProjectsStackControl.RemoveProjectAt(index);
                        index--;
                        itemCount = Math.Max(projects.Count, m_theProjectsStackControl.ChildCount);
                        continue;
                    }

                    // Add on display item
                    if (currentProjectOnDisplay == null || (currentProjectToBeDisplayed != null && GUIProject.SortProjectsByPriority(currentProjectToBeDisplayed, currentProjectOnDisplay) < 0))
                    {
                        GUIProject project = GUIProject.GetInstanceFromDBProject(currentProjectToBeDisplayed);
                        m_theProjectsStackControl.InsertProject(index, project, openSubProjects, ActiveProjectsOnlyFn, TasksToDisplay);
                        itemCount = Math.Max(projects.Count, m_theProjectsStackControl.ChildCount);
                        continue;
                    }

                    ProjectControl control = m_theProjectsStackControl.ProjectAt(index);
                    if (control != null)
                        control.Redisplay();
                }

                //List<string> columnOrder = new List<string>();
                //columnOrder.Add(ProjectPal.Tasks.GUITask.s_Description);
                //columnOrder.Add(ProjectPal.Tasks.GUITask.s_Priority);
                //columnOrder.Add(ProjectPal.Tasks.GUITask.s_Urgency);
                //columnOrder.Add(ProjectPal.Tasks.GUITask.s_DueDate);
                //columnOrder.Add(ProjectPal.Tasks.GUITask.s_Status);
                //columnOrder.Add(ProjectPal.Tasks.GUITask.s_Resources);
                //columnOrder.Add(ProjectPal.Tasks.GUITask.s_Attachments);

                //CustomGUIControls.Component.ComponentControl ctrl = new CustomGUIControls.Component.ComponentControl(component);
                //foreach (DBProjectPal.Project currentProject in projects)
                //{
                //    GUIProject project = GUIProject.GetInstanceFromDBProject(currentProject, this);
                //    m_theProjectsStackControl.AddProject(project, openSubProjects, activeProjectsOnly, columnOrder);
                //}
                //m_theProjectsStackControl.Width = elementHost1.Width;

                //elementHost1.Child = m_theProjectsStackControl;
            }
            catch (Exception)
            {
            }
            finally
            {
                m_duringRedisplay = false;
            }
        }



        ProjectStackControl m_theProjectsStackControl = null;

        public GUIProject.TasksDisplayValues TasksToDisplay()
        {
            if (radioButtonNone.Checked) return GUIProject.TasksDisplayValues.None;
            if (radioButtonOpen.Checked) return GUIProject.TasksDisplayValues.Open;
            return GUIProject.TasksDisplayValues.All;
        }



        public bool OnlyOpenActiveTasks { get { return checkBoxOnlyActiveTasks.Checked; } }

        DBProjectPal.Project m_rootProject = null;



        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            NewProject window = new NewProject(m_rootProject, NewProject.Mode.New);
            DialogResult result = window.ShowDialog(this);
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string newProjectName = window.ProjectName;
                if (string.IsNullOrEmpty(newProjectName))
                {
                    System.Windows.MessageBox.Show("A name for a project must be specified", "Project name not specified", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                DBProjectPal.Project theNewProject = DBProjectPal.Project.AddNewInstance(newProjectName, m_rootProject);
                Functions.ClearDisplayCaches();
                if (m_rootProject != null)
                    ProjectPal.Projects.GUIProject.Redisplay(m_rootProject);
                else
                {
                    Redisplay();
                }
            }
        }

        private void checkBoxOnlyActiveTasks_CheckedChanged(object sender, EventArgs e)
        {
            Functions.ClearDisplayCaches();
            Redisplay();
        }

        private void ProjectWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (m_rootProject != null)
                s_allInstances.Remove(m_rootProject);
            else
                m_topLevelWindow = null;
        }

        private void radioButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (rb != null && rb.Checked)
            {
                Functions.ClearDisplayCaches();
                Redisplay();
            }
        }

        private void labelProjectTitle_DragDrop(object sender, DragEventArgs e)
        {
            labelProjectTitle.ForeColor = Color.Black;

            string[] formats = e.Data.GetFormats();
            object draggedObject = Functions.ToGUIObjectIfPossible(Utils.DragDrop.DragHelper.DraggedObject);

            GUIProject draggedComponent = draggedObject as GUIProject;


            if (draggedComponent != null)
            {
                if (draggedComponent.DBProject != m_rootProject &&
                    (m_rootProject == null || !m_rootProject.IsDescendantOf(draggedComponent.DBProject)))
                {
                    draggedComponent.SetNewParent(m_rootProject);
                }

            }
        }

        private void labelProjectTitle_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;
            if (Permissions.IsAllowed(RootOwner, Permissions.EntityType.Project, Permissions.ChangeType.Edit))
            {

                string[] formats = e.Data.GetFormats();
                object draggedObject = Functions.ToGUIObjectIfPossible(Utils.DragDrop.DragHelper.DraggedObject);

                GUIProject draggedComponent = draggedObject as GUIProject;


                if (draggedComponent != null)
                {
                    if (draggedComponent.DBProject != m_rootProject &&
                        (m_rootProject == null || !m_rootProject.IsDescendantOf(draggedComponent.DBProject)))
                    {
                        e.Effect = DragDropEffects.Move;
                        labelProjectTitle.ForeColor = Color.DarkOrchid;
                    }

                }
            }
        }

        private void labelProjectTitle_DragLeave(object sender, EventArgs e)
        {
            labelProjectTitle.ForeColor = Color.Black;
        }

        private void labelProjectTitle_DragOver(object sender, DragEventArgs e)
        {
            labelProjectTitle_DragEnter(sender, e);
        }

        private void gridControlAttachments_DragDrop(object sender, DragEventArgs e)
        {
            Utils.DragDrop.DroppedFiles files = new Utils.DragDrop.DroppedFiles(e);

            foreach (Utils.DragDrop.DroppedFile file in files.Files)
            {
                DBProjectPal.Attachment.AddNewInstance(m_rootProject, file.Title, file.Type, file.From, file.TimeStamp, file.FileContents);
                Functions.ClearDisplayCaches();
                GUIProject.Redisplay(m_rootProject);
                // TODO - Really, the above call should do this, but this window is not a view !
                Redisplay();
            }
        }

        private void gridControlAttachments_DragEnter(object sender, DragEventArgs e)
        {
            if (Permissions.IsAllowed(RootOwner, Permissions.EntityType.Project, Permissions.ChangeType.Edit) &&
                Utils.DragDrop.DroppedFiles.IsDropable(e))
                e.Effect = DragDropEffects.Copy;
        }

        private void gridControlAttachments_DragOver(object sender, DragEventArgs e)
        {
            if (Permissions.IsAllowed(RootOwner, Permissions.EntityType.Project, Permissions.ChangeType.Edit) &&
                Utils.DragDrop.DroppedFiles.IsDropable(e))
                e.Effect = DragDropEffects.Copy;
        }



        private void labelParent_Click(object sender, EventArgs e)
        {
            GUIProject projectItemOnDisplay = null;
            DBProjectPal.Project parentProject = m_rootProject.Parent;

            if (parentProject != null)
            {
                projectItemOnDisplay = GUIProject.GetInstanceFromDBProject(parentProject);
            }

            ProjectDetail.GetAndShowDetailWindow(projectItemOnDisplay);
        }




        private void labelParent_MouseEnter(object sender, EventArgs e)
        {
            labelParent.Font = new Font(labelParent.Font, FontStyle.Underline);
            labelParent.ForeColor = Color.Blue;
        }

        private void labelParent_MouseLeave(object sender, EventArgs e)
        {
            labelParent.Font = new Font(labelParent.Font, FontStyle.Regular);
            labelParent.ForeColor = labelParentText.ForeColor;
        }

        private void comboBoxPriority_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_rootProject != null && !m_duringInit && !m_duringRedisplay)
            {
                GUIProject proj = GUIProject.GetInstanceFromDBProject(m_rootProject);
                proj.Priority = comboBoxPriority.Text;

                //proj.RedisplaySubTasks();
                Functions.ClearDisplayCaches();
                proj.Redisplay();


            }
        }

        static public void ShowDetailWindow(IDisplayItem objectToDisplay)
        {
            GUIProject underlyingObject = objectToDisplay as GUIProject;

            ProjectDetail window = GetAndShowDetailWindow(underlyingObject);


        }





        private void textBoxDetailedDescription_KeyUp(object sender, KeyEventArgs e)
        {
            if (m_rootProject != null && m_rootProject.DetailedDescription != textBoxDetailedDescription.Text)
            {
                m_rootProject.DetailedDescription = textBoxDetailedDescription.Text;
                //CustomGUIControls.RedisplayManager.Instance.Reset();
                //ProjectPal.Projects.GUIProject.Redisplay(m_rootProject);
            }
        }

        private void comboBoxNewOwner_SelectedIndexChanged(object sender, EventArgs e)
        {
            string user = DBProjectPal.Person.FindPerson(comboBoxNewOwner.Text).DBLogin.Trim();

            if (m_rootProject != null && m_rootProject.Owner != user)
            {
                m_rootProject.Owner = user;
                Functions.ClearDisplayCaches();
                ProjectPal.Projects.GUIProject.Redisplay(m_rootProject);
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            Functions.ClearDisplayCaches();
            dateTimePickerDueDate.CustomFormat = " ";
            if (m_rootProject != null && m_rootProject.DueDate != null)
            {
                m_rootProject.DueDate = null;
                Functions.ClearDisplayCaches();
                ProjectPal.Projects.GUIProject.Redisplay(m_rootProject);

                ProjectPal.Projects.FormPlanDisplay.RedisplayAll();
            }
        }

        private void dateTimePickerDueDate_CloseUp(object sender, EventArgs e)
        {
            dateTimePickerDueDate.CustomFormat = "dd-MMM-yyyy";

            if (m_rootProject != null && m_rootProject.DueDate != dateTimePickerDueDate.Value)
            {
                m_rootProject.DueDate = dateTimePickerDueDate.Value;
                Functions.ClearDisplayCaches();
                ProjectPal.Projects.GUIProject.Redisplay(m_rootProject);

                ProjectPal.Projects.FormPlanDisplay.RedisplayAll();
            }
        }

        private void dateTimePickerStartDate_CloseUp(object sender, EventArgs e)
        {
            dateTimePickerStartDate.CustomFormat = "dd-MMM-yyyy";

            if (m_rootProject != null && m_rootProject.StartDate != dateTimePickerStartDate.Value)
            {
                m_rootProject.StartDate = dateTimePickerStartDate.Value;

                Functions.ClearDisplayCaches();
                ApplicationProjectPal.Instance.RefreshAllWindows();
            }
        }

        private DateTime? DueDate
        {
            get
            {
                if (dateTimePickerDueDate.CustomFormat == " ")
                    return null;
                else
                    return dateTimePickerDueDate.Value;
            }
        }


        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            ProjectPal.Tasks.GUITask.UrgencyCachingEnabled = true;
            FormPlanDisplay.GetAndShowPlanDisplayWindow(m_rootProject);
            ProjectPal.Tasks.GUITask.UrgencyCachingEnabled = false;
        }

        private void checkBoxPrivate_CheckedChanged(object sender, EventArgs e)
        {
            if (m_rootProject != null && !m_duringRedisplay)
            {
                m_rootProject.Private = checkBoxPrivate.Checked;
                Functions.ClearDisplayCaches();
                ProjectPal.Projects.GUIProject.Redisplay(m_rootProject);

                ProjectPal.Projects.FormPlanDisplay.RedisplayAll();
            }
        }

        private void buttonResetStartDate_Click(object sender, EventArgs e)
        {
            if (m_rootProject != null)
            {
                bool ctrlButtonDown =
                     System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) ||
                     System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl);

                m_rootProject.ResetStartDate(ctrlButtonDown);
                Functions.ClearDisplayCaches();
                ApplicationProjectPal.Instance.RefreshAllWindows();


            }
        }

        private void buttonDependencies_Click(object sender, EventArgs e)
        {
            buttonDependencies.Visible = false;
            buttonDependenciesClose.Visible = true;
            buttonDependenciesClose.Location = buttonDependencies.Location;

            int extraSize = 143;
            panel1.Height -= extraSize;

            panel1.Location = new Point(panel1.Location.X, panel1.Location.Y + extraSize);

            panelDependencies.Height += extraSize;
        }

        private void buttonDependenciesClose_Click(object sender, EventArgs e)
        {
            buttonDependenciesClose.Visible = false;
            buttonDependencies.Visible = true;

            int extraSize = 143;
            panel1.Height += extraSize;

            panel1.Location = new Point(panel1.Location.X, panel1.Location.Y - extraSize);

            panelDependencies.Height -= extraSize;

        }


        private void RedisplayDependencies()
        {
            if (m_rootProject != null)
            {
                listBoxDependsUpon.Items.Clear();
                List<DBProjectPal.ITaskOrProject> preDependants = new List<DBProjectPal.ITaskOrProject>(m_rootProject.ImmediatePreDependencies);
                preDependants.Sort(SortDependenciesByEndDate);

                foreach (DBProjectPal.ITaskOrProject preDependant in preDependants)
                {
                    listBoxDependsUpon.Items.Add(preDependant);//.ObjectDescription);
                }

                ////////

                listBoxDependants.Items.Clear();
                List<DBProjectPal.ITaskOrProject> postDependants = new List<DBProjectPal.ITaskOrProject>(m_rootProject.ImmediatePostDependants);
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

        private void listBoxDependency_DoubleClick(object sender, EventArgs e)
        {
            ListBox listboxUsed = (sender as ListBox);
            DBProjectPal.ITaskOrProject itemSelected = listboxUsed.SelectedItem as DBProjectPal.ITaskOrProject;
            if (itemSelected != null)
            {
                if (itemSelected is DBProjectPal.Task)
                {
                    Tasks.GUITask theGuiTask = Tasks.GUITask.GetInstanceFromDBTask((DBProjectPal.Task)itemSelected);
                    ProjectPal.Tasks.TaskDetail.ShowDetailWindow(theGuiTask);
                }

                if (itemSelected is DBProjectPal.Project)
                {
                    Projects.GUIProject guiProject = Projects.GUIProject.GetInstanceFromDBProject((DBProjectPal.Project)itemSelected);
                    Projects.ProjectDetail.GetAndShowDetailWindow(guiProject);
                }

            }
        }

        private void listBoxDependsUpon_DragDrop(object sender, DragEventArgs e)
        {
            object draggedObject = Functions.ToGUIObjectIfPossible(Utils.DragDrop.DragHelper.DraggedObject);

            ProjectPal.Tasks.GUITask draggedTask = draggedObject as ProjectPal.Tasks.GUITask;
            if (draggedTask != null)
            {
                DBProjectPal.Project postProject = m_rootProject;
                DBProjectPal.Task preTask = draggedTask.DBTask;


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
            ProjectPal.Projects.GUIProject draggedProject = draggedObject as ProjectPal.Projects.GUIProject;
            if (draggedProject != null)
            {
                DBProjectPal.Project postProject = m_rootProject;
                DBProjectPal.Project preProject = draggedProject.DBProject;
                if (preProject == postProject)
                    return;

                if (postProject.HasPostDependency(preProject))
                {
                    MessageBox.Show("Circular dependencies are not allowed", "Circular Dependency not allowed", MessageBoxButtons.OK);
                }
                else
                {
                    preProject.AddPostDependency(postProject);
                    Functions.ClearDisplayCaches();
                    ApplicationProjectPal.Instance.RefreshAllWindows();
                }

            }
        }

        private void listBoxDependsUpon_DragEnter(object sender, DragEventArgs e)
        {
            object draggedObject = Functions.ToGUIObjectIfPossible(Utils.DragDrop.DragHelper.DraggedObject);

            ProjectPal.Tasks.GUITask draggedTask = draggedObject as ProjectPal.Tasks.GUITask;
            ProjectPal.Projects.GUIProject draggedProject = draggedObject as ProjectPal.Projects.GUIProject;
            e.Effect = DragDropEffects.None;
            if (m_rootProject != null && (draggedTask != null || draggedProject != null))
            {

                if (Permissions.IsAllowed(m_rootProject.Owner, Permissions.EntityType.Project, Permissions.ChangeType.Edit))
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
                DBProjectPal.Project preProject = m_rootProject;
                DBProjectPal.Task postTask = draggedTask.DBTask;

                if (postTask.HasPostDependency(preProject))
                {
                    MessageBox.Show("Circular dependencies are not allowed", "Circular Dependency", MessageBoxButtons.OK);
                }
                else
                {
                    if (preProject.AllActiveTasks.Contains(postTask))
                    {
                        MessageBox.Show("Cannot make a Sub Task dependant on a parent Project", "Parent Project/Sub Task Dependency not allowed", MessageBoxButtons.OK);
                    }
                    else
                    {
                        preProject.AddPostDependency(postTask);
                        Functions.ClearDisplayCaches();
                        ApplicationProjectPal.Instance.RefreshAllWindows();
                    }
                }

            }

            ProjectPal.Projects.GUIProject draggedProject = draggedObject as ProjectPal.Projects.GUIProject;
            if (draggedProject != null)
            {
                DBProjectPal.Project preProject = m_rootProject;
                DBProjectPal.Project postProject = draggedProject.DBProject;
                if (preProject == postProject)
                    return;


                if (postProject.HasPostDependency(preProject))
                {
                    MessageBox.Show("Circular dependencies are not allowed", "Circular Dependency", MessageBoxButtons.OK);
                }
                else
                {
                    preProject.AddPostDependency(postProject);
                    Functions.ClearDisplayCaches();
                    ApplicationProjectPal.Instance.RefreshAllWindows();

                }
            }
        }

        private void listBoxDependants_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;

            if (m_rootProject == null)
                return;

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

        private void listBoxDependency_KeyUp(object sender, KeyEventArgs e)
        {
            if (m_rootProject == null)
                return;

            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                ListBox listboxUsed = (sender as ListBox);
                DBProjectPal.ITaskOrProject itemSelected = listboxUsed.SelectedItem as DBProjectPal.ITaskOrProject;
                if (itemSelected != null)
                {
                    if (listboxUsed == listBoxDependsUpon)
                    {
                        if (Permissions.IsAllowed(m_rootProject.Owner, Permissions.EntityType.Project, Permissions.ChangeType.Edit))
                        {
                            m_rootProject.RemovePreDependency(itemSelected);
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

                            m_rootProject.RemovePostDependency(itemSelected);
                        }
                    }

                    Functions.ClearDisplayCaches();
                    ApplicationProjectPal.Instance.RefreshAllWindows();
                }
            }
        }

        private void labelProjectTitle_MouseDown(object sender, MouseEventArgs e)
        {
            if (m_rootProject == null)
                return;

            bool ctrlButtonDown =
                       System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) ||
                       System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl);


            DragDropEffects effects = ctrlButtonDown ?
                                        DragDropEffects.Link :
                                        DragDropEffects.Move;

            GUIProject guiProject = GUIProject.GetInstanceFromDBProject(m_rootProject);

            if (Permissions.IsAllowed(m_rootProject.Owner, Permissions.EntityType.Project, Permissions.ChangeType.Edit))
            {
                DataObject data = Utils.DragDrop.DragHelper.SetDraggedObject(guiProject);

                DoDragDrop(data, effects);
            }
        }





    }
}
