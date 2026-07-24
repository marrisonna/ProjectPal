using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using Utils;
using System.Windows.Forms;

namespace TaskMan.Components
{
    public partial class ComponentWindow : Form
    {

        static List<ComponentWindow> m_allTopLevelComponentWindows = new List<ComponentWindow>();


        private static Dictionary<DBTaskMan.Component, ComponentWindow> s_allInstances = new Dictionary<DBTaskMan.Component, ComponentWindow>();

        static public ComponentWindow GetInstanceFromGUIComponent(GUIComponent rootComponent)
        {
            ComponentWindow window;
            if (rootComponent == null || !s_allInstances.TryGetValue(rootComponent.DBComponent, out window))
            {
                return new ComponentWindow(rootComponent);
            }
            return window;

        }



        private ComponentWindow(GUIComponent rootComponent)
        {
            InitializeComponent();

            if (rootComponent != null)
                s_allInstances.Add(rootComponent.DBComponent, this);


            m_theComponentsStackControl = new ComponentStackControl(TaskMan.Tasks.TaskDetail.ShowDetailWindow);
            m_theComponentsStackControl.Width = elementHost1.Width;
            elementHost1.Child = m_theComponentsStackControl;
            m_rootComponent = rootComponent != null ? rootComponent.DBComponent : null;

            gridControlAttachments.SetDoubleClickFunction(Attachments.GUIAttachment.OpenAttachment);
            gridControlAttachments.SetCellDeleteFunction(Attachments.GUIAttachment.DeleteAttachment);
            gridControlAttachments.SetCheckCellDeleteFunction(Attachments.GUIAttachment.ConfirmDeleteAttachment);

            buttonNewComponent.Enabled = Permissions.IsAllowed(RootOwner, Permissions.EntityType.Component, Permissions.ChangeType.Create);

            Redisplay();
            if (m_rootComponent == null)
            {
                m_allTopLevelComponentWindows.Add(this);
                radioButtonNone.Checked = true;
            }
        }

        private string RootOwner
        {
            get
            {
                return m_rootComponent == null ? null : m_rootComponent.Owner;
            }
        }

        public static void RedisplayAll()
        {
            List<ComponentWindow> windows = new List<ComponentWindow>();
            windows.AddRange(m_allTopLevelComponentWindows);
            windows.AddRange(s_allInstances.Values);

            foreach (ComponentWindow currentWindow in windows)
                currentWindow.Redisplay();
        }

        private void Redisplay()
        {
            if (CustomGUIControls.RedisplayManager.Instance.HasItemAlreadyBeenRedisplayed(this))
                return;

            List<DBTaskMan.Component> components = null;
            bool openSubComponents = false;
            if (m_rootComponent == null)
            {
                components = new List<DBTaskMan.Component>(DBTaskMan.Component.TopLevelComponents);
                labelComponentTitle.Text = "Top Level Components";
                Text = "ProjectPal: Component - " + "Top Level Components";
                labelOwner.Visible = labelParentText.Visible = labelComponentText.Visible = labelParent.Visible = false;

                splitContainer1.Panel1MinSize = 0;
                splitContainer1.SplitterDistance = 0;
                labelAttachments.Visible = false;
            }
            else
            {
                if (m_rootComponent.IsDeleted || m_rootComponent.IsPrivateToAnotherAndHidden)
                {
                    Close();
                    return;
                }

                DBTaskMan.Person owner = DBTaskMan.Person.FindPersonFromDBLogin(m_rootComponent.Owner);
                string personName = owner == null ? "" : owner.Name;
                labelOwner.Text = "Owner: " + personName;

                components = new List<DBTaskMan.Component>();

                components.Add(m_rootComponent);
                labelComponentTitle.Text = m_rootComponent.Name;
                Text = "ProjectPal: Component - " + m_rootComponent.Name;
                labelParent.Text = "(" + (m_rootComponent.Parent != null ? m_rootComponent.FullParentName : "Top Level Components") + ")";
                labelOwner.Visible = labelParentText.Visible = labelComponentText.Visible = labelParent.Visible = true;

                openSubComponents = true;


                gridControlAttachments.WindowClosed();
                gridControlAttachments.SetColumns(TaskMan.Attachments.GUIAttachmentColumns.Instance);
                foreach (DBTaskMan.Attachment currentAttachement in m_rootComponent.Attachments)
                {
                    gridControlAttachments.AddDisplayItem(TaskMan.Attachments.GUIAttachment.GetInstanceFromDBAttachment(currentAttachement));
                }
                gridControlAttachments.SetFilters();
            }
            components.Sort(SortComponents);

            int itemCount = Math.Max(components.Count, m_theComponentsStackControl.ChildCount);

            for (int index = 0; index < itemCount; index++)
            {
                DBTaskMan.Component currentComponentToBeDisplayed = index < components.Count ? components[index] : null;
                GUIComponent currentComponentOnDisplay = index < m_theComponentsStackControl.ChildCount ? m_theComponentsStackControl.ComponentAt(index).ComponentItemOnDisplay : null;

                // Delete on display item
                if (currentComponentToBeDisplayed == null || (currentComponentOnDisplay != null && string.Compare(currentComponentToBeDisplayed.Name, currentComponentOnDisplay.Name) > 0))
                {
                    m_theComponentsStackControl.RemoveComponentAt(index);
                    index--;
                    itemCount--;
                    continue;
                }

                // Add on display item
                if (currentComponentOnDisplay == null || (currentComponentToBeDisplayed != null && string.Compare(currentComponentToBeDisplayed.Name, currentComponentOnDisplay.Name) < 0))
                {
                    GUIComponent component = GUIComponent.GetInstanceFromDBComponent(currentComponentToBeDisplayed);
                    m_theComponentsStackControl.InsertComponent(index, component, openSubComponents, TasksToDisplay);
                    continue;
                }

                ComponentControl control = m_theComponentsStackControl.ComponentAt(index);
                if (control != null)
                    control.Redisplay();
            }

        }

        ComponentStackControl m_theComponentsStackControl = null;


        public GUIComponent.TasksDisplayValues TasksToDisplay()
        {
            if (radioButtonNone.Checked) return GUIComponent.TasksDisplayValues.None;
            if (radioButtonOpen.Checked) return GUIComponent.TasksDisplayValues.Open;
            return GUIComponent.TasksDisplayValues.All;
        }

        DBTaskMan.Component m_rootComponent = null;

        private static int SortComponents(DBTaskMan.Component a, DBTaskMan.Component b)
        {
            return string.Compare(a.Name, b.Name);
        }



        private void ComponentWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            m_allTopLevelComponentWindows.Remove(this);
            if (m_rootComponent != null)
                s_allInstances.Remove(m_rootComponent);
        }

        private void checkBoxOnlyOpenTasks_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton_CheckedChanged(object sender, EventArgs e)
        {
            Functions.ClearDisplayCaches();
            RadioButton rb = sender as RadioButton;
            if (rb != null && rb.Checked)
                Redisplay();
        }

        private void labelComponentTitle_DragDrop(object sender, DragEventArgs e)
        {
            labelComponentTitle.ForeColor = Color.Black;

            string[] formats = e.Data.GetFormats();
            object draggedObject = Functions.ToGUIObjectIfPossible(Utils.DragDrop.DragHelper.DraggedObject);

            GUIComponent draggedComponent = draggedObject as GUIComponent;

            if (draggedComponent != null)
            {
                if (draggedComponent.DBComponent != m_rootComponent &&
                    (m_rootComponent == null || !m_rootComponent.IsDescendantOf(draggedComponent.DBComponent)) &&
                    Permissions.IsAllowed(draggedComponent.DBComponent.Owner, Permissions.EntityType.Component, Permissions.ChangeType.Edit) &&
                    (m_rootComponent == null || Permissions.IsAllowed(RootOwner, Permissions.EntityType.Component, Permissions.ChangeType.Edit)))
                {
                    draggedComponent.SetNewParent(m_rootComponent);
                }

            }

        }

        private void labelComponentTitle_DragEnter(object sender, DragEventArgs e)
        {
            string[] formats = e.Data.GetFormats();
            object draggedObject = Functions.ToGUIObjectIfPossible(Utils.DragDrop.DragHelper.DraggedObject);

            GUIComponent draggedComponent = draggedObject as GUIComponent;

            if (draggedComponent != null)
            {
                if (draggedComponent.DBComponent != m_rootComponent &&
                    (m_rootComponent == null || !m_rootComponent.IsDescendantOf(draggedComponent.DBComponent)) &&
                    Permissions.IsAllowed(draggedComponent.DBComponent.Owner, Permissions.EntityType.Component, Permissions.ChangeType.Edit) &&
                    (m_rootComponent == null || Permissions.IsAllowed(RootOwner, Permissions.EntityType.Component, Permissions.ChangeType.Edit)))
                {
                    e.Effect = DragDropEffects.Move;
                    labelComponentTitle.ForeColor = Color.DarkOrchid;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }

        }

        private void labelComponentTitle_DragLeave(object sender, EventArgs e)
        {
            labelComponentTitle.ForeColor = Color.Black;
        }

        private void labelComponentTitle_DragOver(object sender, DragEventArgs e)
        {
            labelComponentTitle_DragEnter(sender, e);
        }

        private void gridControlAttachments_DragDrop(object sender, DragEventArgs e)
        {
            if (Permissions.IsAllowed(RootOwner, Permissions.EntityType.Component, Permissions.ChangeType.Edit))
            {
                Utils.DragDrop.DroppedFiles files = new Utils.DragDrop.DroppedFiles(e);

                foreach (Utils.DragDrop.DroppedFile file in files.Files)
                {
                    DBTaskMan.Attachment.AddNewInstance(m_rootComponent, file.Title, file.Type, file.From, file.TimeStamp, file.FileContents);
                    Functions.ClearDisplayCaches();
                    GUIComponent.Redisplay(m_rootComponent);
                    // TODO - Really, the above call should do this, but this window is not a view !
                    Redisplay();
                }
            }
        }

        private void gridControlAttachments_DragEnter(object sender, DragEventArgs e)
        {
            if (Utils.DragDrop.DroppedFiles.IsDropable(e) &&
                Permissions.IsAllowed(RootOwner, Permissions.EntityType.Component, Permissions.ChangeType.Edit))
                e.Effect = DragDropEffects.Copy;
        }

        private void gridControlAttachments_DragOver(object sender, DragEventArgs e)
        {
            if (Utils.DragDrop.DroppedFiles.IsDropable(e) &&
                Permissions.IsAllowed(RootOwner, Permissions.EntityType.Component, Permissions.ChangeType.Edit))
                e.Effect = DragDropEffects.Copy;
        }

        private void labelParent_Click(object sender, EventArgs e)
        {
            DBTaskMan.Component parentComponent = m_rootComponent.Parent;

            ComponentWindow newWindow = null;
            if (parentComponent != null)
            {
                GUIComponent componentItemOnDisplay = GUIComponent.GetInstanceFromDBComponent(parentComponent);
                newWindow = ComponentWindow.GetInstanceFromGUIComponent(componentItemOnDisplay);
            }
            else
            {
                if (m_allTopLevelComponentWindows.Count > 0)
                    newWindow = m_allTopLevelComponentWindows[m_allTopLevelComponentWindows.Count - 1];
                else
                    newWindow = ComponentWindow.GetInstanceFromGUIComponent(null);
            }
            newWindow.Show();
            newWindow.Focus();
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

        private void tabPage2_Enter(object sender, EventArgs e)
        {
            Functions.ClearDisplayCaches();
            RedisplayGantt();
            if (!s_ganttDisplays.Contains(this))
                s_ganttDisplays.Add(this);
        }

        public static void RedisplayAllGantts()
        {
            foreach (ComponentWindow currentWindow in s_ganttDisplays)
            {
                currentWindow.RedisplayGantt();
            }
        }


        PlanDisplay.PlanControl m_thePlanControl = null;

        private static HashSet<ComponentWindow> s_ganttDisplays = new HashSet<ComponentWindow>();

        private void RedisplayGantt()
        {
            if (CustomGUIControls.RedisplayManager.Instance.HasItemAlreadyBeenRedisplayed(this.tabPage2))
                return;

            bool newPlanControl = false;

            if (m_thePlanControl == null)
            {
                newPlanControl = true;
                m_thePlanControl = new PlanDisplay.PlanControl();

            }



            HashSet<DBTaskMan.Task> topLevelTasks = new HashSet<DBTaskMan.Task>();
            PlanDisplay.Project displayProject = null;
            HashSet<string> allDistinctResources = null;
            GanttDisplayHelper helper = new GanttDisplayHelper();

            if (m_rootComponent == null)
            {
                List<DBTaskMan.Component> components = new List<DBTaskMan.Component>(DBTaskMan.Component.TopLevelComponents);
                components.Sort(SortComponents);
                displayProject = new PlanDisplay.Project(m_rootComponent, m_thePlanControl, null, PlanDisplay.Project.EventType.Component);
                displayProject.SetEventFunction(helper.HandleEvent);

                allDistinctResources = GanttDisplayHelper.AddPlanDetail(m_thePlanControl,
                                               displayProject, topLevelTasks, components,
                                               null, null);
            }
            else
            {
                DBTaskMan.Component component = m_rootComponent;
                foreach (DBTaskMan.Task componentTask in m_rootComponent.Tasks)
                    topLevelTasks.Add(componentTask);

                displayProject = new PlanDisplay.Project(m_rootComponent, m_thePlanControl, null, PlanDisplay.Project.EventType.Component);
                displayProject.SetEventFunction(helper.HandleEvent);

                allDistinctResources = GanttDisplayHelper.AddPlanDetail(m_thePlanControl,
                                              displayProject, topLevelTasks, component.SubComponents,
                                              null, null);
            }



            if (newPlanControl)
            {
                helper.SetPlanControl(m_thePlanControl);

                displayProject.SetDateLine(DateTime.Now.Date);

                m_thePlanControl.RedisplayProject(displayProject, allDistinctResources, TaskMan.GanttDisplayHelper.ResourceColorMap);
                m_thePlanControl.ReScaleToAreaData(this.elementHostArea.Width, this.elementHostArea.Height);
                elementHostArea.Child = m_thePlanControl;
            }
            else
            {
                helper.SetPlanControl(m_thePlanControl);
                m_thePlanControl.RedisplayProject(displayProject, allDistinctResources, TaskMan.GanttDisplayHelper.ResourceColorMap);
            }
        }



        private void tabPage2_Leave(object sender, EventArgs e)
        {
            // elementHostArea.Child = null;
            s_ganttDisplays.Remove(this);
        }

        private void buttonNewComponent_Click(object sender, EventArgs e)
        {
            NewComponent window = new NewComponent(m_rootComponent, NewComponent.Mode.New);
            DialogResult result = window.ShowDialog(this);
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string newComponentName = window.ComponentName;
                if (string.IsNullOrEmpty(newComponentName))
                {
                    System.Windows.MessageBox.Show("A name for a component must be specified", "Component name not specified", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                DBTaskMan.Component theNewComponent = DBTaskMan.Component.AddNewInstance(newComponentName, m_rootComponent);
                Functions.ClearDisplayCaches();
                if (m_rootComponent != null)
                    TaskMan.Components.GUIComponent.Redisplay(m_rootComponent);
                else
                {
                    Redisplay();
                }
            }
        }



    }
}
