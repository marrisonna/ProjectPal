using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBProjectPal;

namespace ProjectPal.Components
{
    public class GUIComponent : CustomGUIControls.IDisplayItem
    {
        public int ID { get; private set; }

        private static int s_nextId = 0;
        private static Dictionary<int, GUIComponent> s_allInstances = new Dictionary<int, GUIComponent>();

        private DBProjectPal.Component m_dbComponent;
        public DBProjectPal.Component DBComponent { get { return m_dbComponent; } }

        public enum TasksDisplayValues { All, Open, None };


        public string Name { get { return m_dbComponent.Name; } set { m_dbComponent.Name = value; } }

        public IList<GUIComponent> SubItems
        {
            get
            {
                List<GUIComponent> subComponents = new List<GUIComponent>();
                foreach (DBProjectPal.Component currentComponent in m_dbComponent.SubComponents)
                {
                    subComponents.Add(GetInstanceFromDBComponent(currentComponent));
                }
                return subComponents;
            }
        }

        public IList<CustomGUIControls.Grid.IGridItem> Tasks(GUIComponent.TasksDisplayValues taskToDisplay)
        {
            
                List<CustomGUIControls.Grid.IGridItem> tasks = new List<CustomGUIControls.Grid.IGridItem>();
                foreach (DBProjectPal.Task currentTask in m_dbComponent.Tasks)
                {
                    if (taskToDisplay == TasksDisplayValues.All ||
                        (currentTask.Status.HasValue &&
                         currentTask.Status.Value != DBProjectPal.StatusValue.Cancelled &&
                         currentTask.Status.Value != DBProjectPal.StatusValue.Closed))
                        tasks.Add(ProjectPal.Tasks.GUITask.GetInstanceFromDBTask(currentTask));
                }
                return tasks;
            
        }

        public IEnumerable<Attachment> Attachments
        {
            get
            {
                return m_dbComponent.Attachments;
            }
        }


        public int TotalActiveTaskCount
        {
            get
            {
                return m_dbComponent.TotalActiveTaskCount;
            }
        }

        public int ActiveTaskCount
        {
            get
            {
                return m_dbComponent.ActiveTaskCount;
            }
        }




        static public GUIComponent GetInstanceFromDBComponent(DBProjectPal.Component dbComponent)
        {
            GUIComponent results = null;
            if (m_instances.TryGetValue(dbComponent, out results))
            {
                return results;
            }
            return new GUIComponent(dbComponent);
        }

        static Dictionary<DBProjectPal.Component, GUIComponent> m_instances = new Dictionary<DBProjectPal.Component, GUIComponent>();

        private GUIComponent(DBProjectPal.Component dbComponent)
        {
            this.ID = s_nextId;
            s_allInstances.Add(this.ID, this);
            s_nextId++;
            m_displayItem = new CustomGUIControls.DisplayItemImpl(this);

            m_dbComponent = dbComponent;
            
            GUIComponent results = null;
            if (!m_instances.TryGetValue(dbComponent, out results))
                m_instances.Add(dbComponent, this);
        }

        public static GUIComponent GetInstanceFromId(int id)
        {
            GUIComponent result = null;
            s_allInstances.TryGetValue(id, out result);
            return result;
        }

        ~GUIComponent()
        {
            s_allInstances.Remove(this.ID);
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
            m_displayItem.Redisplay();
        }

        public void DisplayItemDeleted()
        {
            m_displayItem.DisplayItemDeleted();
        }

        static public void Redisplay(DBProjectPal.Component component)
        {
            GUIComponent guiComponent;
            if (m_instances.TryGetValue(component, out guiComponent))
                guiComponent.Redisplay();

        }


        static public void RedisplayAll()
        {
            List<GUIComponent> guis = new List<GUIComponent>(m_instances.Values);
            foreach (GUIComponent gui in guis)
            {
                gui.Redisplay();
            }
        }


        static public void DeleateAllDisplayItems(DBProjectPal.Component theComponent)
        {
            GUIComponent guiComponent;
            if (m_instances.TryGetValue(theComponent, out guiComponent))
            {
                guiComponent.DeleteInstance();
                m_instances.Remove(theComponent);
            }

        }

        CustomGUIControls.DisplayItemImpl m_displayItem = null;

        public void SetNewParent(DBProjectPal.Component newParent)
        {
            DBProjectPal.Component currentParent = m_dbComponent.Parent;

            if (newParent != null && newParent.IsDescendantOf(m_dbComponent))
                throw new Exception("Cannot make a parent Component a child of one of its own children");

            m_dbComponent.Parent = newParent;

            //if (currentParent == null || newParent == null)
            //    ComponentWindow.RedisplayAll();
            //if (currentParent != null)
            //    RedisplayTopParent(currentParent);
            //if (newParent != null)
            //    RedisplayTopParent(newParent);
            Functions.ClearDisplayCaches(); 
            ApplicationProjectPal.Instance.RefreshAllWindows();
        }
   

        public void DeleteInstance()
        {
            DisplayItemDeleted();
            m_dbComponent.DeleteInstance();
        }


        public bool IsDeleted { get { return m_dbComponent.IsDeleted; } }
    }
}
