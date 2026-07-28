using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Utils;

namespace DBTaskMan
{
    public class Remark : DBAccess.DBObject<Remark>, ISearchable
    {
        public bool ContainsText(string searchText)
        {
            return (RemarkText != null && RemarkText.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) != -1);
        }


        public override string ObjectDescription { get { return "Remark: " + Utils.Misc.RemoveInvalidCharacters(RemarkText); } }

        public List<Attachment> Attachments
        {
            get
            {
                return new List<Attachment>();
            }
        }

        public bool IsClosed
        {
            get
            {

                return m_task == null || m_task.IsClosed;
            }
        }


        public Task Task
        {
            get { return m_task; }
            set
            {
                m_task = value;
                if (m_task != null)
                {
                    m_component = null;
                    m_project = null;
                }
                StatusModified();
            }
        }

        public Component Component
        {
            get { return m_component; }
            set
            {
                m_component = value;
                if (m_component != null)
                {
                    m_project = null;
                    m_task = null;
                }
                StatusModified();
            }
        }

        public Project Project
        {
            get { return m_project; }
            set
            {
                m_project = value;
                if (m_project != null)
                {
                    m_component = null;
                    m_task = null;
                }
                StatusModified();
            }
        }



        public String RemarkText
        {
            get { return m_remarkText; }
            set { m_remarkText = value; m_owner = DBAccess.DBObjectBase.DBUser; StatusModified(); }
        }

        static public Remark AddNewInstance(Task ownerTask, string remarkText)
        {
            Remark theNewRemark = new Remark(ownerTask, remarkText);

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(Remark));
            return theNewRemark;
        }


        private Remark(Task ownerTask, string remarkText)
        {
            AddNewInstance(this);
            m_task = ownerTask;
            m_remarkText = remarkText;

            ModifiedTime = DBAccess.DBObjectBase.DBTime;
            Owner = DBAccess.DBObjectBase.DBUser;// Environment.UserName;

        }

        override public void CopyDetails(Remark instanceToMerge)
        {
            CopyDetailsBase(instanceToMerge);

            m_remarkText = instanceToMerge.m_remarkText;

            StatusNotModified();

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(Remark));
        }

        Task m_task = null;
        Component m_component = null;
        Project m_project = null;
        string m_remarkText;


        internal static List<Remark> RemarksForTask(Task theTask)
        {
            List<Remark> taskAttachments = new List<Remark>();
            if (theTask != null)
            {
                foreach (Remark thisRemark in AllInstances)
                {
                    if (thisRemark.Task == theTask)
                        taskAttachments.Add(thisRemark);
                }
            }
            return taskAttachments;
        }



        //////// Database access

        static Remark()
        {
            AllInstanceQuery = "select * from " + DatabaseBase.Schema + "Remark";
        }

        public Remark()
        {
        }

        protected override void CreateFromFile(DBAccess.FileSystem_File file)
        {

            int? taskId = file.GetInt("TaskId");
            if (taskId.HasValue)
                m_task = Task.GetInstanceFromDatabaseId(taskId.Value);


            int? componentId = file.GetInt("ComponentId");
            if (componentId.HasValue)
                m_component = Component.GetInstanceFromDatabaseId(componentId.Value);

            int? projectId = file.GetInt("ProjectId");
            if (projectId.HasValue)
                m_project = Project.GetInstanceFromDatabaseId(projectId.Value);

            Owner = file.GetString("Owner");
            m_remarkText = Utils.SecurityFunctions.Decrypt(file.GetString("RemarkText"));
        }



        protected override void CreateFromReader(DbDataReader remarkRecord)
        {

            if (!s_setupHasBeenDone)
            {
                s_setupHasBeenDone = true;
                SetUpFromReader(remarkRecord);
            }

            DatabaseId = DatabaseBase.GetColumnValueAs<int>(remarkRecord, s_columnRemarkId).Value;

            int? taskId = DatabaseBase.GetColumnValueAs<int>(remarkRecord, s_columnTaskId);
            if (taskId.HasValue)
                m_task = Task.GetInstanceFromDatabaseId(taskId.Value);

            int? componentId = DatabaseBase.GetColumnValueAs<int>(remarkRecord, s_columnComponentId);
            if (componentId.HasValue)
                m_component = Component.GetInstanceFromDatabaseId(componentId.Value);

            int? projectId = DatabaseBase.GetColumnValueAs<int>(remarkRecord, s_columnProjectId);
            if (projectId.HasValue)
                m_project = Project.GetInstanceFromDatabaseId(projectId.Value);

            Owner = DatabaseBase.GetColumnValueAsString(remarkRecord, s_columnOwner);
            m_remarkText = Utils.SecurityFunctions.Decrypt(DatabaseBase.GetColumnValueAsString(remarkRecord, s_columnRemarkText));

        }

        protected override DBAccess.FileSystem_File FileSystemSaveObject()
        {
            DBAccess.FileSystem_File result = new DBAccess.FileSystem_File(this);

            result.Add("TaskId", (int?)GetDatabaseIdForWritingToDatabase(m_task));
            result.Add("ComponentId", (int?)GetDatabaseIdForWritingToDatabase(m_component));
            result.Add("ProjectId", (int?)GetDatabaseIdForWritingToDatabase(m_project));
            result.Add("RemarkText", Utils.SecurityFunctions.Encrypt(m_remarkText));
            result.Add("Owner", m_owner);

            return result;
        }


        protected override DbCommand SaveToDataBaseSQLCommand()
        {
            SqlInsert sqlForInstert = new SqlInsert("Remark", DatabaseId);
            sqlForInstert.AddParameter("TaskId", System.Data.SqlDbType.Int, GetDatabaseIdForWritingToDatabase(m_task));
            sqlForInstert.AddParameter("ComponentId", System.Data.SqlDbType.Int, GetDatabaseIdForWritingToDatabase(m_component));
            sqlForInstert.AddParameter("ProjectId", System.Data.SqlDbType.Int, GetDatabaseIdForWritingToDatabase(m_project));
            if (Utils.DatabaseBase.DBType == DatabaseBase.DBTypeValues.SQLServer)
                sqlForInstert.AddParameter("RemarkText", System.Data.SqlDbType.NVarChar, 1000000, Utils.SecurityFunctions.Encrypt(m_remarkText));
            else
                sqlForInstert.AddParameter("RemarkText", System.Data.SqlDbType.NText, Utils.SecurityFunctions.Encrypt(m_remarkText));
            sqlForInstert.AddParameter("Owner", System.Data.SqlDbType.NVarChar, 50, m_owner);

            return sqlForInstert.Command;
        }




        override public void DeleteInstance()
        {
            Remark.DeleteInstance(this);

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(Remark));
        }


        static public void DeleteInstance(Task theTask)
        {
            List<Remark> itemsToDelete = new List<Remark>();
            foreach (Remark currentRemark in Remark.AllInstances)
            {
                if (currentRemark.m_task == theTask)
                {
                    itemsToDelete.Add(currentRemark);
                }
            }
            foreach (Remark remarkToDelete in itemsToDelete)
            {
                DeleteInstance(remarkToDelete);
            }

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(Remark));
        }

        protected override DbCommand DeleteFromDataBaseSQLCommand()
        {
            SqlDelete deleteSql = new SqlDelete("Remark", DatabaseId);
            return deleteSql.Command;
        }




        static protected void SetUpFromReader(DbDataReader tempateRecord)
        {

            s_columnRemarkId = tempateRecord.GetOrdinal("RemarkId");
            s_columnTaskId = tempateRecord.GetOrdinal("TaskId");
            s_columnComponentId = tempateRecord.GetOrdinal("ComponentId");
            s_columnProjectId = tempateRecord.GetOrdinal("ProjectId");
            s_columnOwner = tempateRecord.GetOrdinal("Owner");
            s_columnRemarkText = tempateRecord.GetOrdinal("RemarkText");

        }

        static bool s_setupHasBeenDone = false;

        static int s_columnRemarkId;
        static int s_columnTaskId;
        static int s_columnComponentId;
        static int s_columnProjectId;
        static int s_columnOwner;
        static int s_columnRemarkText;


    }
}
