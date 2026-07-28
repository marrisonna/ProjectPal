using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Utils;

namespace DBProjectPal
{
    public class Attachment : DBAccess.DBObject<Attachment>
    {
        #region Public Interface

        public override int GetHashCode()
        {
            return this.DatabaseId;
        }

        public override bool Equals(object obj)
        {
            Attachment that = obj as Attachment;
            return Object.ReferenceEquals(this, that);
            //return that != null &&  DatabaseId == that.DatabaseId && Object.ReferenceEquals(this,that);

        }

        public override string ObjectDescription { get { return "Attachment: " + Utils.Misc.RemoveInvalidCharacters(this.Name); } }


        /// <summary>
        /// Pass in an array of strings to look for
        /// Returns an array of strings found
        /// </summary>
        /// <param name="searchText"></param>
        /// <returns></returns>
        public string[] ContainsText(string[] searchText)
        {
            try
            {
                int lastDot = Name == null ? -1 : Name.LastIndexOf('.');
                string extension = lastDot == -1 ? "" : Name.Substring(lastDot + 1);

                if (DataType == "Mail")
                    extension = "msg";

                Utils.DocumentSearch.DocumentType type = DocumentSearch.DocumentType.Unknown;
                if (extension == "msg")
                    type = DocumentSearch.DocumentType.OutLookMsg;

                string[] result = null;

                if (extension.Length > 2 && extension.Substring(0, 3) == "doc")
                {
                    type = DocumentSearch.DocumentType.Word;
                    result = Utils.DocumentSearch.SearchWordDoc(searchText, SaveAttachmentToDisk());
                }
                else
                {
                    result = Utils.DocumentSearch.Search(searchText, Data, type, true);
                }

                // Clear the attachment from memory
                m_attachmentHasBeenRetrieved = false;
                m_data = null;
                IsCompressed = m_originalIsCompressed;

                return result;
            }
            catch (Exception err)
            {
                Logger.LogException(err, "Error searching attachment '" + Name + "' id = " + DatabaseId);
            }
            return new string[0];
        }

        public String Name
        {
            get { return m_name; }
            set { m_name = value; StatusModified(); }
        }


        public bool IsCompressed { get; private set; }


        public Task Task
        {
            get { return m_task; }
            set
            {
                if (m_task != null)
                    m_task.RemoveAttachment(this);

                m_task = value;
                if (m_task != null)
                {
                    m_task.AddAttachment(this);
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
                    if (m_task != null)
                        m_task.RemoveAttachment(this);
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
                    if (m_task != null)
                        m_task.RemoveAttachment(this);
                    m_task = null;
                }
                StatusModified();
            }
        }

        public String DataType
        {
            get { return m_dataType; }
            set { m_dataType = value; StatusModified(); }
        }

        public String From
        {
            get { return m_from; }
            set { m_from = value; StatusModified(); }
        }

        public int Size
        {
            get
            {
                if (!m_size.HasValue)
                    m_size = Data.Length;
                return m_size.Value;
            }
        }

        public DateTime? CreateTime
        {
            get { return m_createTime; }
            set { m_createTime = value; StatusModified(); }
        }

        public byte[] Data
        {
            get
            {
                if (m_data == null && !m_attachmentHasBeenRetrieved && DatabaseId >= 0)
                {
                    m_attachmentHasBeenRetrieved = true;
                    //string query = "execute TaskMan.SelectAttachementData " + DatabaseId.ToString();

                    if (DatabaseBase.DBType == DatabaseBase.DBTypeValues.SQLServer)
                    {

                        string query = "select Data from TaskMan.Attachment	where AttachmentId = " + DatabaseId.ToString();

                        using (DbDataReader queryResult = ExecuteReader(query, "Attachement.Data"))
                        {

                            if (queryResult.Read())
                            {
                                object data = DatabaseBase.GetColumnValueAsObject(queryResult, 0);
                                m_data = Utils.SecurityFunctions.Decrypt((byte[])data);
                            }
                            queryResult.Close();
                        }
                    }
                    else
                    {
                        m_data = DBAccess.FileSystem_File.GetByteArray(this, "Data");
                        m_data = Utils.SecurityFunctions.Decrypt(m_data);
                    }
                }
                if (m_data == null)
                    return null;
                if (IsCompressed)
                {
                    m_data = DeCompressData(m_data);
                    IsCompressed = false;
                }
                return m_data;
            }
            set { m_data = value; StatusModified(); }
        }


        override public void CopyDetails(Attachment instanceToMerge)
        {
            CopyDetailsBase(instanceToMerge);

            // An attachment can't be merged.

            StatusNotModified();


            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(Attachment));
        }

        static public void ReSaveAll()
        {
            foreach (Attachment currentAttachment in AllInstances)
            {
                int size = currentAttachment.Size;
                currentAttachment.StatusModified();
            }
            DBAccess.DBObjectBase.Save();
        }




        static public Attachment AddNewInstance(Task ownerTask, string name, string dataType, string from, DateTime? creationTime, byte[] data)
        {
            bool alreadyAdded = false;
            foreach (Attachment existing in ownerTask.Attachments)
            {
                if (existing.Name == name &&
                    existing.CreateTime.HasValue &&
                    Math.Abs((existing.CreateTime.Value - creationTime.Value).TotalSeconds) < 1 &&
                    existing.Data.LongLength == data.LongLength)
                {
                    Logger.Log("Attachment is already attached: '" + name + "'");
                    alreadyAdded = true;
                    break;
                }
            }

            if (alreadyAdded)
                return null;

            Attachment theNewAttachment = new Attachment(ownerTask, name, dataType, from, creationTime, data);

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(Attachment));
            return theNewAttachment;
        }

        static public Attachment AddNewInstance(Component ownerTask, string name, string dataType, string from, DateTime? creationTime, byte[] data)
        {
            Attachment theNewAttachment = new Attachment(ownerTask, name, dataType, from, creationTime, data);

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(Attachment));
            return theNewAttachment;
        }

        static public Attachment AddNewInstance(Project ownerTask, string name, string dataType, string from, DateTime? creationTime, byte[] data)
        {
            Attachment theNewAttachment = new Attachment(ownerTask, name, dataType, from, creationTime, data);

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(Attachment));
            return theNewAttachment;
        }

        private Attachment(Task ownerTask, string name, string dataType, string from, DateTime? creationTime, byte[] data)
        {
            AddNewInstance(this);
            IsCompressed = false;
            m_task = ownerTask;
            if (m_task != null)
                m_task.AddAttachment(this);
            m_name = name;
            m_owner = DBAccess.DBObjectBase.DBUser;
            m_dataType = dataType;
            m_from = from;
            m_createTime = creationTime;
            m_data = data;
            m_size = data.Length;
        }

        private Attachment(Component ownerTask, string name, string dataType, string from, DateTime? creationTime, byte[] data)
        {
            AddNewInstance(this);
            IsCompressed = false;
            m_component = ownerTask;
            m_name = name;
            m_owner = DBAccess.DBObjectBase.DBUser;
            m_dataType = dataType;
            m_from = from;
            m_createTime = creationTime;
            m_data = data;
            m_size = data.Length;
        }

        private Attachment(Project ownerTask, string name, string dataType, string from, DateTime? creationTime, byte[] data)
        {
            AddNewInstance(this);
            IsCompressed = false;
            m_project = ownerTask;
            m_name = name;
            m_owner = DBAccess.DBObjectBase.DBUser;
            m_dataType = dataType;
            m_from = from;
            m_createTime = creationTime;
            m_data = data;
            m_size = data.Length;
        }

        #endregion

        Task m_task = null;
        Component m_component = null;
        Project m_project = null;
        string m_name = null;
        string m_dataType = null;
        string m_from = null;
        DateTime? m_createTime = null;
        bool m_attachmentHasBeenRetrieved = false;
        int? m_size = null;
        bool m_originalIsCompressed = false;

        byte[] m_data = null;

        /////// Linking objects

        //internal static IEnumerable<Attachment> AttachmentsForTask(Task theTask)
        //{
        //    List<Attachment> taskAttachments = new List<Attachment>();
        //    if (theTask != null)
        //    {
        //        foreach (Attachment thisAttachment in AllInstances)
        //        {
        //            if (thisAttachment.Task == theTask)
        //                taskAttachments.Add(thisAttachment);
        //        }
        //    }
        //    return taskAttachments;
        //}

        internal static List<Attachment> AttachmentsForComponent(Component theComponent)
        {
            List<Attachment> componentAttachments = new List<Attachment>();
            if (theComponent != null)
            {
                foreach (Attachment thisAttachment in AllInstances)
                {
                    if (thisAttachment.Component == theComponent)
                        componentAttachments.Add(thisAttachment);
                }
            }
            return componentAttachments;
        }

        internal static List<Attachment> AttachmentsForProject(Project theProject)
        {
            List<Attachment> projectAttachments = new List<Attachment>();
            if (theProject != null)
            {
                foreach (Attachment thisAttachment in AllInstances)
                {
                    if (thisAttachment.Project == theProject)
                        projectAttachments.Add(thisAttachment);
                }
            }
            return projectAttachments;
        }

        /////////////////////


        //////// Database access

        private const string CompressedExtension = ".NamZipped";

        static Attachment()
        {
            //AllInstanceQuery = "execute TaskMan.SelectAllAttachements";

            AllInstanceQuery =
                "select " +
                "AttachmentId, TaskId, ComponentId, ProjectId, Name, CreateTime, DataType, Size, [From], [Owner], ModifiedTime, ModifiedBy " +
                "from " + DatabaseBase.Schema + "Attachment";

            try
            {
                s_tempAttachmentFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                        Path.Combine("ProjectPal", "AttachmentsTemp"));

                Utils.Logger.Log("Temp folder for attachments is :- " + s_tempAttachmentFolder);

                if (!Directory.Exists(s_tempAttachmentFolder))
                    Directory.CreateDirectory(s_tempAttachmentFolder);
                // Delete any old attachments
                DateTime now = DateTime.Now;
                foreach (string dirname in Directory.GetDirectories(s_tempAttachmentFolder))
                {
                    DirectoryInfo dir = new DirectoryInfo(dirname);
                    if ((now - dir.CreationTime).TotalHours > 24)
                    {
                        Directory.Delete(dirname, true);
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.LogException(exp, "Error while deleting old attachments from '" + s_tempAttachmentFolder + "'");
            }
        }

        public Attachment()
        {
            IsCompressed = false;

        }

        ~Attachment()
        {
            if (m_task != null)
                m_task.RemoveAttachment(this);
        }

        protected override void CreateFromFile(DBAccess.FileSystem_File file)
        {

            int? taskId = file.GetInt("TaskId");
            if (taskId.HasValue)
            {
                if ((m_task = Task.GetInstanceFromDatabaseId(taskId.Value)) != null)
                    m_task.AddAttachment(this);
            }

            int? componentId = file.GetInt("ComponentId");
            if (componentId.HasValue)
                m_component = Component.GetInstanceFromDatabaseId(componentId.Value);

            int? projectId = file.GetInt("ProjectId");
            if (projectId.HasValue)
                m_project = Project.GetInstanceFromDatabaseId(projectId.Value);

            string rawName = Utils.SecurityFunctions.Decrypt(file.GetString("Name"));
            CreateTime = file.GetDate("CreateTime").Value;
            DataType = file.GetString("DataType");
            From = file.GetString("From");
            m_owner = file.GetString("Owner");
            m_size = file.GetInt("Size");


            IsCompressed = (!string.IsNullOrEmpty(rawName) &&
                            rawName.Length >= CompressedExtension.Length &&
                            rawName.Substring(rawName.Length - CompressedExtension.Length) == CompressedExtension);

            m_originalIsCompressed = IsCompressed;

            if (IsCompressed)
                Name = rawName.Substring(0, rawName.Length - CompressedExtension.Length);
            else
                Name = rawName;

        }

        protected override void CreateFromReader(DbDataReader attachementRecord)
        {

            //Utilities.

            if (!s_setupHasBeenDone)
            {
                s_setupHasBeenDone = true;
                SetUpFromReader(attachementRecord);
            }

            DatabaseId = DatabaseBase.GetColumnValueAs<int>(attachementRecord, s_columnAttachmentId).Value;

            int? taskId = DatabaseBase.GetColumnValueAs<int>(attachementRecord, s_columnTaskId);
            if (taskId.HasValue)
            {
                if ((m_task = Task.GetInstanceFromDatabaseId(taskId.Value)) != null)
                    m_task.AddAttachment(this);
            }

            int? componentId = DatabaseBase.GetColumnValueAs<int>(attachementRecord, s_columnComponentId);
            if (componentId.HasValue)
                m_component = Component.GetInstanceFromDatabaseId(componentId.Value);

            int? projectId = DatabaseBase.GetColumnValueAs<int>(attachementRecord, s_columnProjectId);
            if (projectId.HasValue)
                m_project = Project.GetInstanceFromDatabaseId(projectId.Value);

            string rawName = Utils.SecurityFunctions.Decrypt(DatabaseBase.GetColumnValueAsString(attachementRecord, s_columnName));
            CreateTime = DatabaseBase.GetColumnValueAs<DateTime>(attachementRecord, s_columnCreateTime).Value;
            DataType = DatabaseBase.GetColumnValueAsString(attachementRecord, s_columnDataType);
            From = DatabaseBase.GetColumnValueAsString(attachementRecord, s_columnFrom);
            m_owner = DatabaseBase.GetColumnValueAsString(attachementRecord, s_columnOwner);
            m_size = DatabaseBase.GetColumnValueAs<int>(attachementRecord, s_columnSize);


            IsCompressed = (!string.IsNullOrEmpty(rawName) &&
                            rawName.Length >= CompressedExtension.Length &&
                            rawName.Substring(rawName.Length - CompressedExtension.Length) == CompressedExtension);

            m_originalIsCompressed = IsCompressed;

            if (IsCompressed)
                Name = rawName.Substring(0, rawName.Length - CompressedExtension.Length);
            else
                Name = rawName;

        }

        static string s_tempAttachmentFolder;

        private string CleanFileName(string rawFileName)
        {

            string result = rawFileName.
                        Replace(@"\", "").
                        Replace(@"/", "").
                        Replace(@":", "").
                        Replace(@"*", "").
                        Replace(@"?", "").
                        Replace("\"", "").
                        Replace(@"<", "").
                        Replace(@">", "").
                        Replace(@"|", "");

            //string result = "";

            //foreach (char c in rawFileName)
            //{
            //    if ((c >= '0' && c <= '9') ||
            //        (c >= 'a' && c <= 'z') ||
            //        (c >= 'A' && c <= 'Z') ||
            //        (c == ' ') ||
            //        (c == '-') ||
            //        (c == '_') ||
            //        (c == '.'))
            //        result += c;
            //}
            return result;
        }

        public string SaveAttachmentToDisk()
        {
            byte[] fileData = Data;
            if (fileData == null)
                return null;
            string dirName = s_tempAttachmentFolder + @"\" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            Directory.CreateDirectory(dirName);




            string cleanFileName = CleanFileName(Name);
            string fileName = dirName + @"\" + cleanFileName;
            if (fileName.Length > 240)
            {
                int charsToCut = fileName.Length - 240;
                string f1 = Path.GetFileNameWithoutExtension(cleanFileName);
                string extension = Path.GetExtension(cleanFileName);
                if (charsToCut >= f1.Length)
                {
                    Logger.Log(string.Format("Attachment '{0}' cannot be opened because the file name is too long", fileName));
                    return null;
                }
                cleanFileName = f1.Substring(0, f1.Length - charsToCut).Trim();
                if (string.IsNullOrEmpty(cleanFileName))
                    cleanFileName = "a";
                if (!string.IsNullOrEmpty(extension))
                    cleanFileName += "." + extension;

                fileName = dirName + @"\" + cleanFileName;
            }


            if (DataType == "Mail")
                fileName += ".msg";
            System.IO.FileStream file = new System.IO.FileStream(fileName, System.IO.FileMode.Create);
            file.Write(fileData, 0, fileData.Length);
            file.Close();

            return fileName;
        }

        protected override DBAccess.FileSystem_File FileSystemSaveObject()
        {


            if (m_data == null)
            {
                // force the data to be loaded if it hasn't already.  This shouldn't really ever happen.
                byte[] dummy = Data;
            }



            string nameForSave = Name;
            if (!IsCompressed)
            {
                byte[] compressedData = CompressData(m_data);
                if (compressedData != null && compressedData.Length < m_data.Length)
                {
                    m_data = compressedData;
                    IsCompressed = true;

                }
            }

            if (IsCompressed)
                nameForSave += CompressedExtension;

            DBAccess.FileSystem_File result = new DBAccess.FileSystem_File(this);

            result.Add("TaskId", (int?)GetDatabaseIdForWritingToDatabase(m_task));
            result.Add("ComponentId", (int?)GetDatabaseIdForWritingToDatabase(m_component));
            result.Add("ProjectId", (int?)GetDatabaseIdForWritingToDatabase(m_project));
            result.Add("Name", Utils.SecurityFunctions.Encrypt(nameForSave));
            result.Add("CreateTime", CreateTime);
            result.Add("DataType", DataType);
            result.Add("From", From);
            result.Add("Size", m_size);
            result.Add("Owner", m_owner);
            result.Add("Data", Utils.SecurityFunctions.Encrypt(m_data));

            return result;
        }


        protected override DbCommand SaveToDataBaseSQLCommand()
        {
            if (m_data == null)
            {
                // force the data to be loaded if it hasn't already.  This shouldn't really ever happen.
                byte[] dummy = Data;
            }

            string nameForSave = Name;
            if (!IsCompressed)
            {
                byte[] compressedData = CompressData(m_data);
                if (compressedData != null && compressedData.Length < m_data.Length)
                {
                    m_data = compressedData;
                    IsCompressed = true;

                }
            }



            if (IsCompressed)
                nameForSave += CompressedExtension;

            SqlInsert sqlForInstert = new SqlInsert("Attachment", DatabaseId);
            sqlForInstert.AddParameter("TaskId", System.Data.SqlDbType.Int, GetDatabaseIdForWritingToDatabase(m_task));
            sqlForInstert.AddParameter("ComponentId", System.Data.SqlDbType.Int, GetDatabaseIdForWritingToDatabase(m_component));
            sqlForInstert.AddParameter("ProjectId", System.Data.SqlDbType.Int, GetDatabaseIdForWritingToDatabase(m_project));
            sqlForInstert.AddParameter("Name", System.Data.SqlDbType.NVarChar, 250, Utils.SecurityFunctions.Encrypt(nameForSave));
            sqlForInstert.AddParameter("CreateTime", System.Data.SqlDbType.DateTime, CreateTime);
            sqlForInstert.AddParameter("DataType", System.Data.SqlDbType.NVarChar, 10, DataType);
            sqlForInstert.AddParameter("From", System.Data.SqlDbType.NVarChar, 20, From);
            sqlForInstert.AddParameter("Size", System.Data.SqlDbType.Int, m_size);
            sqlForInstert.AddParameter("Owner", System.Data.SqlDbType.NVarChar, 50, m_owner);

            if (Utils.DatabaseBase.DBType == DatabaseBase.DBTypeValues.SQLServer)
                sqlForInstert.AddParameter("Data", System.Data.SqlDbType.VarBinary, Utils.SecurityFunctions.Encrypt(m_data));
            else
                sqlForInstert.AddParameter("Data", System.Data.SqlDbType.Image, Utils.SecurityFunctions.Encrypt(m_data));


            return sqlForInstert.Command;
        }


        private static byte[] DeCompressData(byte[] data)
        {
            if (data == null)
                return null;
            try
            {
                System.IO.Stream dataStreamIn = new System.IO.MemoryStream(data);
                System.IO.Compression.GZipStream decompressor = new System.IO.Compression.GZipStream(dataStreamIn, System.IO.Compression.CompressionMode.Decompress);

                System.IO.MemoryStream dataStreamOut = new System.IO.MemoryStream();
                decompressor.CopyTo(dataStreamOut);
                decompressor.Close();

                byte[] result = dataStreamOut.ToArray();

                return result;
            }
            catch (Exception exp)
            {
                Utils.Logger.LogException(exp, "Error decompressing data");
            }
            return data;
        }

        private static byte[] CompressData(byte[] data)
        {
            if (data == null)
                return null;
            System.IO.MemoryStream dataStreamOut = new System.IO.MemoryStream();
            System.IO.Compression.GZipStream compressor = new System.IO.Compression.GZipStream(dataStreamOut, System.IO.Compression.CompressionMode.Compress);
            compressor.Write(data, 0, data.Length);
            compressor.Close();
            byte[] result = dataStreamOut.ToArray();
            return result;
        }


        override public void OnStatusTemporary()
        {
            if (m_task != null)
                m_task.RemoveAttachment(this);
        }

        override public void DeleteInstance()
        {
            Attachment.DeleteInstance(this);

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(Attachment));
            if (m_task != null)
                m_task.RemoveAttachment(this);
        }


        static public void DeleteInstance(Task theTask)
        {
            List<Attachment> itemsToDelete = new List<Attachment>();
            foreach (Attachment currentAttachment in Attachment.AllInstances)
            {
                if (currentAttachment.m_task == theTask)
                {
                    itemsToDelete.Add(currentAttachment);
                }
            }
            foreach (Attachment attachmentToDelete in itemsToDelete)
            {
                DeleteInstance(attachmentToDelete);
            }

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(Attachment));
        }

        protected override DbCommand DeleteFromDataBaseSQLCommand()
        {
            SqlDelete deleteSql = new SqlDelete("Attachment", DatabaseId);
            return deleteSql.Command;
        }



        static protected void SetUpFromReader(DbDataReader tempateRecord)
        {

            s_columnAttachmentId = tempateRecord.GetOrdinal("AttachmentId");
            s_columnTaskId = tempateRecord.GetOrdinal("TaskId");
            s_columnComponentId = tempateRecord.GetOrdinal("ComponentId");
            s_columnProjectId = tempateRecord.GetOrdinal("ProjectId");
            s_columnName = tempateRecord.GetOrdinal("Name");
            s_columnCreateTime = tempateRecord.GetOrdinal("CreateTime");
            s_columnDataType = tempateRecord.GetOrdinal("DataType");
            s_columnFrom = tempateRecord.GetOrdinal("From");
            s_columnOwner = tempateRecord.GetOrdinal("Owner");
            s_columnSize = tempateRecord.GetOrdinal("Size");


        }

        static bool s_setupHasBeenDone = false;

        static int s_columnAttachmentId;
        static int s_columnTaskId;
        static int s_columnComponentId;
        static int s_columnProjectId;

        static int s_columnName;
        static int s_columnCreateTime;
        static int s_columnDataType;
        static int s_columnFrom;
        static int s_columnOwner;
        static int s_columnSize;


    }
}
