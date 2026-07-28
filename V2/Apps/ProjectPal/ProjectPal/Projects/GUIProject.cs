using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Utils;

namespace ProjectPal.Projects
{
    public class GUIProject : CustomGUIControls.Grid.IGridItem
    {
        private DBProjectPal.Project m_dbProject;
        public DBProjectPal.Project DBProject { get { return m_dbProject; } }
        public enum TasksDisplayValues { All, Open, None };

        public string Name { get { return m_dbProject.Name; } set { m_dbProject.Name = value; } }
        public string Owner { get { return m_dbProject.Owner; } set { m_dbProject.Owner = value; } }
        public string ParentName { get { return m_dbProject.FullParentName; } }
        public string IsActive { get { return m_dbProject.IsActive ? s_isActiveTrueString : "-"; } }
        public string ObjectDescription { get { return m_dbProject.ObjectDescription; } }


        public const string s_isActiveTrueString = "Active";

        public DataGridViewCellStyle GetCellStyle(string columnName, DataGridViewCellStyle defaultStyle)
        {
            return null;
        }

        public string Priority
        {
            get
            {
                if (!m_dbProject.Priority.HasValue)
                    return null;

                switch (m_dbProject.Priority.Value)
                {
                    case DBProjectPal.PriorityValue._5_High:
                        return GUIProjectColumns.s_priortyVHigh;
                    case DBProjectPal.PriorityValue._4_MedHigh:
                        return GUIProjectColumns.s_priortyHigh;
                    case DBProjectPal.PriorityValue._3_Med:
                        return GUIProjectColumns.s_priortyMed;
                    case DBProjectPal.PriorityValue._2_MedLow:
                        return GUIProjectColumns.s_priortyLow;
                    case DBProjectPal.PriorityValue._1_Low:
                        return GUIProjectColumns.s_priortyVLow;
                    case DBProjectPal.PriorityValue._0_Cancelled:
                        return GUIProjectColumns.s_priortyCancelled;
                    case DBProjectPal.PriorityValue._0_Closed:
                        return GUIProjectColumns.s_priortyClosed;
                    default:
                        return null;
                }



            }
            set
            {
                m_dbProject.Priority = null;
                switch (value)
                {

                    case GUIProjectColumns.s_priortyVHigh:
                        m_dbProject.Priority = DBProjectPal.PriorityValue._5_High;
                        break;
                    case GUIProjectColumns.s_priortyHigh:
                        m_dbProject.Priority = DBProjectPal.PriorityValue._4_MedHigh;
                        break;
                    case GUIProjectColumns.s_priortyMed:
                        m_dbProject.Priority = DBProjectPal.PriorityValue._3_Med;
                        break;
                    case GUIProjectColumns.s_priortyLow:
                        m_dbProject.Priority = DBProjectPal.PriorityValue._2_MedLow;
                        break;
                    case GUIProjectColumns.s_priortyVLow:
                        m_dbProject.Priority = DBProjectPal.PriorityValue._1_Low;
                        break;
                    case GUIProjectColumns.s_priortyCancelled:
                        m_dbProject.Priority = DBProjectPal.PriorityValue._0_Cancelled;
                        break;
                    case GUIProjectColumns.s_priortyClosed:
                        m_dbProject.Priority = DBProjectPal.PriorityValue._0_Closed;
                        break;
                    default:
                        m_dbProject.Priority = null;
                        break;
                }
            }

        }


        static public GUIProject GetInstanceFromDBProject(DBProjectPal.Project dbProject)
        {
            GUIProject results = null;
            if (m_instances.TryGetValue(dbProject, out results))
            {
                return results;
            }
            return new GUIProject(dbProject);
        }

        static Dictionary<DBProjectPal.Project, GUIProject> m_instances = new Dictionary<DBProjectPal.Project, GUIProject>();

        private GUIProject(DBProjectPal.Project dbProject)
        {
            m_displayItem = new CustomGUIControls.DisplayItemImpl(this);

            m_dbProject = dbProject;

            GUIProject results = null;
            if (!m_instances.TryGetValue(dbProject, out results))
                m_instances.Add(dbProject, this);

        }

        

        public void AddView(CustomGUIControls.IView view)
        {
            m_displayItem.AddView(view);
        }

        public void RemoveView(CustomGUIControls.IView view)
        {
            m_displayItem.RemoveView(view);
        }

        public void Redisplay()
        {
            if (CustomGUIControls.RedisplayManager.Instance.HasItemAlreadyBeenRedisplayed(this))
                return;

            m_displayItem.Redisplay();
            RedisplaySubTasks();
        }

        static public void RedisplayAll()
        {
            List<GUIProject> guis = new List<GUIProject>(m_instances.Values);
            foreach (GUIProject gui in guis)
            {
                gui.Redisplay();
            }
        }

        public void DisplayItemDeleted()
        {
            m_displayItem.DisplayItemDeleted();
        }


        public void RedisplaySubTasks()
        {
            foreach (DBProjectPal.Task aTask in m_dbProject.Tasks)
            {
                ProjectPal.Tasks.GUITask.Redisplay(aTask);
            }
            foreach (DBProjectPal.Project aProject in m_dbProject.SubProjects)
            {

                GUIProject guiProject = null;
                if (m_instances.TryGetValue(aProject, out guiProject))
                {
                    guiProject.RedisplaySubTasks();
                }
            }
        }

        static public void Redisplay(DBProjectPal.Project project)
        {
            GUIProject guiProject = null;
            if (m_instances.TryGetValue(project, out guiProject))
            {
                guiProject.Redisplay();
            }
        }

        static public void DeleateAllDisplayItems(DBProjectPal.Project theProject)
        {
            GUIProject guiProject = null;
            if (m_instances.TryGetValue(theProject, out guiProject))
            {
                guiProject.DeleteInstance();
            }
            m_instances.Remove(theProject);
        }

        CustomGUIControls.DisplayItemImpl m_displayItem = null;

        public void SetNewParent(DBProjectPal.Project newParent)
        {
            DBProjectPal.Project currentParent = m_dbProject.Parent;

            if (newParent != null && newParent.IsDescendantOf(m_dbProject))
                throw new Exception("Cannot make a parent Project a child of one of its own Project");

            m_dbProject.Parent = newParent;

            //if (currentParent == null || newParent == null)
            //    ProjectDetail.RedisplayAll();
            //if (currentParent != null)
            //    RedisplayTopParent(currentParent);
            //if (newParent != null)
            //    RedisplayTopParent(newParent);
            Functions.ClearDisplayCaches();
            ApplicationProjectPal.Instance.RefreshAllWindows();
        }



        public IList<CustomGUIControls.Grid.IGridItem> Tasks(GUIProject.TasksDisplayValues taskToDisplay)
        {
            List<CustomGUIControls.Grid.IGridItem> tasks = new List<CustomGUIControls.Grid.IGridItem>();
            foreach (DBProjectPal.Task currentTask in m_dbProject.Tasks)
            {
                if (taskToDisplay == TasksDisplayValues.All ||
                    (currentTask.Status.Value != DBProjectPal.StatusValue.Cancelled &&
                     currentTask.Status.Value != DBProjectPal.StatusValue.Closed))
                    tasks.Add(ProjectPal.Tasks.GUITask.GetInstanceFromDBTask(currentTask));
            }
            return tasks;
        }


        public IList<GUIProject> SubItems
        {
            get
            {
                List<GUIProject> subProjects = new List<GUIProject>();
                foreach (DBProjectPal.Project currentProject in m_dbProject.SubProjects)
                {
                    subProjects.Add(new GUIProject(currentProject));
                }
                return subProjects;
            }
        }




        public double TotalActiveTaskEffort
        {
            get
            {
                return m_dbProject.TotalActiveTaskEffort;
            }
        }

        public bool IsDeleted { get { return m_dbProject.IsDeleted; } }



        public bool IsPrivateToOtherUser { get { return m_dbProject.IsPrivateToAnotherAndHidden; } }


        public int TotalActiveTaskCount
        {
            get
            {
                return m_dbProject.TotalActiveTaskCount;
            }
        }

        public int ActiveTaskCount
        {
            get
            {
                return m_dbProject.ActiveTaskCount;
            }
        }

        public void DeleteInstance()
        {
            DisplayItemDeleted();
            m_dbProject.DeleteInstance();
        }

        public Color Colour
        {
            get
            {
                if (m_dbProject.IsActive)
                    return Utils.Colours.ReadWriteColour;
                return Utils.Colours.ReadOnlyColour;


            }
        }

        public bool PopulateDragDropDataObject(System.Windows.Forms.DataObject dragdropDataContainer)
        {
            return Permissions.IsAllowed(this.Owner, Permissions.EntityType.Project, Permissions.ChangeType.Edit) &&
                    (this.DBProject.Parent != null &&
                     Permissions.IsAllowed(this.DBProject.Parent.Owner, Permissions.EntityType.Project, Permissions.ChangeType.Edit));
        }

        public object GetFieldValue(string columnName)
        {
            switch (columnName)
            {
                case GUIProjectColumns.s_Name: return Name;
                case GUIProjectColumns.s_Priority: return Priority;
                case GUIProjectColumns.s_ParentName: return ParentName;
                case GUIProjectColumns.s_IsActive: return IsActive;
                case GUIProjectColumns.s_TotalActiveTaskCount: return TotalActiveTaskCount;
                case GUIProjectColumns.s_TotalActiveTaskEffort:
                    return TotalActiveTaskEffort;
                case GUIProjectColumns.s_Owner:
                    {
                        DBProjectPal.Person projectOwner = DBProjectPal.Person.FindPersonFromDBLogin(Owner);
                        if (projectOwner != null)
                            return projectOwner.Name;
                        return "";
                    }
                case GUIProjectColumns.s_DueDate:
                    return m_dbProject.DueDate;
                case GUIProjectColumns.s_StartDate:
                    return m_dbProject.StartDate;
                case GUIProjectColumns.s_EndDate: return m_dbProject.EndDate;


            }
            throw new Exception("There is no column called '" + columnName + "'");
        }

        public bool IsReadOnly(string columnName)
        {
            if (columnName == GUIProjectColumns.s_StartDate)
                return true;
            if (Permissions.IsAllowed(this.Owner, Permissions.EntityType.Project, Permissions.ChangeType.Edit))
            {
                return true;
            }
            return true;
        }


        // public for interface
        bool CustomGUIControls.Grid.IGridItem.IsActive()
        {
            if (DBProject.Priority == DBProjectPal.PriorityValue._0_Cancelled ||
                DBProject.Priority == DBProjectPal.PriorityValue._0_Closed ||
                DBProject.TotalActiveTaskCount == 0)
            {
                return false;
            }
            return true;
        }


        public void SetField(string columnName, string value)
        {
            switch (columnName)
            {
                case GUIProjectColumns.s_Name:
                    Name = value;
                    break;
                case GUIProjectColumns.s_Priority:
                    Priority = value;
                    break;
                case GUIProjectColumns.s_DueDate:
                    m_dbProject.DueDate = null;
                    DateTime dueDate;
                    if (DateTime.TryParse(value, out dueDate))
                    {
                        m_dbProject.DueDate = dueDate;
                    }
                    break;
                //case GUIProjectColumns.s_StartDate:
                //    m_dbProject.StartDate = null;
                //    DateTime startDate;
                //    if (DateTime.TryParse(value, out startDate))
                //    {
                //        m_dbProject.StartDate = startDate;
                //    }
                //break;
            }
        }

        public static int SortProjectName(GUIProject a, GUIProject b)
        {
            return string.Compare(a.Name, b.Name);
        }



        public static int SortProjectsByPriority(DBProjectPal.Project a, DBProjectPal.Project b)
        {
            int priorityA = a == null ? -10 : DBProjectPal.Enums.PriorityValueAsInt(a.Priority ?? DBProjectPal.PriorityValue._3_Med);
            int priorityB = b == null ? -10 : DBProjectPal.Enums.PriorityValueAsInt(b.Priority ?? DBProjectPal.PriorityValue._3_Med);

            if (priorityA == priorityB)
            {
                return string.Compare(a.Name, b.Name);
            }
            return priorityB - priorityA;
        }

        public static int SortProjectsByPriority(GUIProject a, GUIProject b)
        {
            return SortProjectsByPriority(a.DBProject, b.DBProject);
        }

        public static int SortProjectsByPriority(DBProjectPal.Project a, GUIProject b)
        {
            return SortProjectsByPriority(a, b.DBProject);
        }

        public static int SortProjectsByPriority(GUIProject a, DBProjectPal.Project b)
        {
            return SortProjectsByPriority(a.DBProject, b);
        }


        public void GridCellDragEnter(DragEventArgs e) { return; }
        public void GridCellDragDrop(DragEventArgs e) { return; }
        public void GridCellDragLeave(EventArgs e) { return; }
    }
}
