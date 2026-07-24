using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Utils;

namespace DBTaskMan
{
    public class Component : DBAccess.DBObject<Component>, ISearchable
    {

        ///// Public


        public override string ObjectDescription { get { return "Component: " + Utils.Misc.RemoveInvalidCharacters(this.Name); } }

        public bool ContainsText(string searchText)
        {
            return (Name != null && Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) != -1);
        }

        public bool IsClosed
        {
            get
            {
                return ActiveTaskCount == 0;
            }
        }

        public string Name
        {
            get
            {
                return m_name;
            }
            set
            {
                m_name = value;
                StatusModified();
            }
        }




        public string FullName
        {
            get
            {
                if (Parent == null) return m_name;
                return m_name + " => [" + FullParentName + "]";
            }
        }

        public string FullParentName
        {
            get
            {
                if (Parent == null) return "";
                return Parent.FullPathName;
            }
        }

        private string FullPathName
        {
            get
            {
                if (Parent == null) return m_name;
                return Parent.FullPathName + "=>" + m_name;
            }
        }

        public Component Parent
        {
            get
            {
                if (m_parent == null && m_parentId.HasValue)
                {
                    m_parent = Component.GetInstanceFromDatabaseId(m_parentId.Value);
                }
                return m_parent;
            }
            set
            {
                m_parent = value;
                m_parentId = null;
                StatusModified();
            }

        }
        public IEnumerable<Component> SubComponents
        {
            get
            {
                IEnumerable<Component> subComponents = from theInstance in AllInstances
                                                       where theInstance.Parent == this
                                                       select theInstance;
                return subComponents;
            }
        }

        public IEnumerable<Task> Tasks
        {
            get
            {
                return Task.TasksForComponent(this);
            }
        }

        public List<Attachment> Attachments
        {
            get
            {
                return Attachment.AttachmentsForComponent(this);
            }
        }

        public int TotalActiveTaskCount
        {
            get
            {
                int taskCount = ActiveTaskCount;
                foreach (DBTaskMan.Component currentComponent in SubComponents)
                {
                    taskCount += currentComponent.TotalActiveTaskCount;
                }
                return taskCount;
            }
        }

        public int ActiveTaskCount
        {
            get
            {
                int taskCount = 0;
                foreach (Task currentTask in Tasks)
                {
                    if (currentTask.Status != StatusValue.Cancelled &&
                        currentTask.Status != StatusValue.Closed)
                        taskCount++;
                }

                return taskCount;
            }
        }

        static public Component AddNewInstance(string name, Component parent)
        {

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(Component));

            return new Component(name, parent);

        }

        static public Component FindComponent(string name)
        {
            foreach (Component thisComponent in AllInstances)
            {
                if (thisComponent.FullName == name)
                    return thisComponent;
            }

            foreach (Component thisComponent in AllInstances)
            {
                if (thisComponent.m_name == name)
                    return thisComponent;
            }

            return null;

        }


        static public IList<Component> TopLevelComponents
        {
            get
            {
                List<Component> topLevelComponents = new List<Component>();

                foreach (Component thisComponent in AllInstances)
                {
                    if (thisComponent.Parent == null)
                        topLevelComponents.Add(thisComponent);
                }

                topLevelComponents.Sort(CompareComponentsByName);
                return topLevelComponents;
            }
        }

        static int CompareComponentsByName(Component x, Component y)
        {
            return string.Compare(x.m_name, y.m_name);
        }

        public bool IsDescendantOf(Component possibleParent)
        {
            if (Parent == null)
                return false;

            if (Parent == possibleParent)
                return true;

            return Parent.IsDescendantOf(possibleParent);
        }

        override public void CopyDetails(Component instanceToMerge)
        {
            CopyDetailsBase(instanceToMerge);

            m_name = instanceToMerge.m_name;
            m_parent = instanceToMerge.m_parent;
            m_parentId = instanceToMerge.m_parentId;

            StatusNotModified();


            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(Component));
        }

        ///// Private

        Component m_parent = null;
        int? m_parentId = null;
        string m_name;


        private Component(string name, Component parent)
        {
            AddNewInstance(this);
            m_name = name;
            m_parent = parent;
        }

        //////// Database access

        static Component()
        {
            AllInstanceQuery = "select * from " + DatabaseBase.Schema + "Component";
        }

        public Component()
        { }


        protected override void CreateFromFile(DBAccess.FileSystem_File file)
        {

            m_parentId = file.GetInt("ParentComponentId");
            if (m_parentId.HasValue)
            {
                m_parent = Component.GetInstanceFromDatabaseId(m_parentId.Value);
            }

            m_name = Utils.SecurityFunctions.Decrypt(file.GetString("Name"));
            m_owner = file.GetString("Owner");


        }



        protected override void CreateFromReader(DbDataReader record)
        {

            if (!s_setupHasBeenDone)
            {
                s_setupHasBeenDone = true;
                SetUpFromReader(record);
            }

            DatabaseId = DatabaseBase.GetColumnValueAs<int>(record, s_columnComponentId).Value;
            m_parentId = DatabaseBase.GetColumnValueAs<int>(record, s_columnParentComponentId);
            if (m_parentId.HasValue)
            {
                m_parent = Component.GetInstanceFromDatabaseId(m_parentId.Value);
            }

            m_name = Utils.SecurityFunctions.Decrypt(DatabaseBase.GetColumnValueAsString(record, s_columnName));
            m_owner = DatabaseBase.GetColumnValueAsString(record, s_columnOwner);

        }

        protected override DBAccess.FileSystem_File FileSystemSaveObject()
        {
            DBAccess.FileSystem_File result = new DBAccess.FileSystem_File(this);

            result.Add("ParentComponentId", (int?)GetDatabaseIdForWritingToDatabase(Parent));
            result.Add("Name", Utils.SecurityFunctions.Encrypt(m_name));
            result.Add("Owner", m_owner);

            return result;
        }


        protected override DbCommand SaveToDataBaseSQLCommand()
        {
            SqlInsert sqlForInstert = new SqlInsert("Component", DatabaseId);
            sqlForInstert.AddParameter("ParentComponentId", System.Data.SqlDbType.Int, GetDatabaseIdForWritingToDatabase(Parent));
            sqlForInstert.AddParameter("Name", System.Data.SqlDbType.NVarChar, 1000, Utils.SecurityFunctions.Encrypt(m_name));
            sqlForInstert.AddParameter("Owner", System.Data.SqlDbType.NVarChar, 50, m_owner);

            return sqlForInstert.Command;
        }

        override public void DeleteInstance()
        {
            if (!HasDependants)
                Component.DeleteInstance(this);

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(Component));
        }

        public bool HasDependants
        {
            get
            {
                return this.Tasks.Count() > 0 || this.SubComponents.Count() > 0;
            }
        }

        protected override DbCommand DeleteFromDataBaseSQLCommand()
        {
            SqlDelete deleteSql = new SqlDelete("Component", DatabaseId);
            return deleteSql.Command;
        }

        static protected void SetUpFromReader(DbDataReader tempateRecord)
        {

            s_columnComponentId = tempateRecord.GetOrdinal("ComponentId");
            s_columnParentComponentId = tempateRecord.GetOrdinal("ParentComponentId");
            s_columnName = tempateRecord.GetOrdinal("Name");
            s_columnOwner = tempateRecord.GetOrdinal("Owner");

        }

        static bool s_setupHasBeenDone = false;

        static int s_columnComponentId;
        static int s_columnParentComponentId;
        static int s_columnName;
        static int s_columnOwner;


        //////////////////

        /* public void AddParent(Component parent)
         {
             if (parent != Parent)
             {
                 if (Parent != null)
                     Parent.RemoveSubComponent(this);
                 Parent = parent;
                 if (Parent != null)
                     Parent.AddSubComponent(this);
             }


         }

         public void AddSubComponent(Component subComponent)
         {
             if (subComponent != null)
             {
                 if (!m_subComponents.Contains(subComponent))
                     m_subComponents.Add(subComponent);
                 subComponent.AddParent(this);
             }
         }

         public Component(string name, Component parent)
         {
             Name = name;
             Parent = parent;
             if (Parent != null)
                 Parent.AddSubComponent(this);
         }

         public void RemoveSubComponent(Component subComponent)
         {
             m_subComponents.Remove(subComponent);
             subComponent.Parent = null;
         }*/

    }
}
