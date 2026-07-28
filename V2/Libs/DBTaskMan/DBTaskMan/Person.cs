using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Data.Common;
using Utils;

namespace DBTaskMan
{
    public class Person : DBAccess.DBObject<Person>, IResource
    {

        #region Public Interface


        public override string ObjectDescription { get { return "Person: " + Utils.Misc.RemoveInvalidCharacters(this.Name); } }


        private string m_name;

        public string Name
        {
            get { return m_name; }
            set { m_name = value; StatusModified(); }
        }

        private string m_dbLogin;

        public string DBLogin
        {
            get { return m_dbLogin == null ? null : m_dbLogin.ToLower(); }
            set { m_dbLogin = value; StatusModified(); }
        }

        private bool m_isResource;

        public bool IsResource
        {
            get { return m_isResource; }
            set { m_isResource = value; StatusModified(); }
        }
        private bool m_isActive;

        public bool IsActive
        {
            get { return m_isActive; }
            set { m_isActive = value; StatusModified(); }
        }

        private Utils.Permissions.UserLevel m_userType;

        public Utils.Permissions.UserLevel UserType
        {
            get { return m_userType; }
            set { m_userType = value; StatusModified(); }
        }

        private string m_colourName;

        public string ColourName
        {
            get { return m_colourName; }
            set { m_colourName = value; StatusModified(); }
        }



        public System.Windows.Media.Color Colour
        {
            get
            {
                System.Drawing.Color colourCode = ColourCode(m_name);
                return Color.FromArgb(colourCode.A, colourCode.R, colourCode.G, colourCode.B);
            }
        }

        public static System.Windows.Media.Color ColourForResource(string resourceName)
        {

            System.Drawing.Color colourCode = ColourCode(resourceName);
            return Color.FromArgb(colourCode.A, colourCode.R, colourCode.G, colourCode.B);

        }

        static public IEnumerable<Person> AllActiveInstances
        {
            get
            {
                List<Person> activePeople = new List<Person>();
                foreach (Person aPerson in DBTaskMan.Person.AllInstances)
                {
                    if (aPerson.IsActive)
                        activePeople.Add(aPerson);
                }
                return activePeople;
            }

        }

        private static System.Drawing.Color ColourCode(string resourceName)
        {
            Person thePerson = FindPerson(resourceName);
            if (thePerson != null && !string.IsNullOrEmpty(thePerson.ColourName))
            {
                System.Drawing.Color actualColour = System.Drawing.Color.FromName(thePerson.ColourName);
                if (actualColour.IsKnownColor)
                    return actualColour;

                string[] rgb = thePerson.ColourName.Split(',');

                if (rgb.Length == 3)
                {
                    int r, g, b;
                    if (int.TryParse(rgb[0], out r) && int.TryParse(rgb[1], out g) && int.TryParse(rgb[2], out b)
                        && ((r & 255) == r) && ((g & 255) == g) && ((b & 255) == b))
                        return System.Drawing.Color.FromArgb(r, g, b);
                }
            }

            switch (resourceName)
            {
                case "Neil":
                    return System.Drawing.Color.Yellow;
                case "Paresh":
                    return System.Drawing.Color.Orange;
                case "Ruth":
                    return System.Drawing.Color.Red;
                case "Parth":
                    return System.Drawing.Color.BlueViolet;
                case "Sachin":
                    return System.Drawing.Color.Brown;
                case "Rahul":
                    return System.Drawing.Color.Cyan;
                case "Sreevani":
                    return System.Drawing.Color.HotPink;
                case "Rizwan":
                    return System.Drawing.Color.SteelBlue;
                case "Nikhil":
                    return System.Drawing.Color.DodgerBlue;
                case "Unassigned":
                    return System.Drawing.Color.WhiteSmoke;
                case DBTaskMan.Task.OtherResource:
                    return System.Drawing.Color.LightSlateGray;
                default:
                    return System.Drawing.Color.Khaki;

            }
        }

        public IEnumerable<Task> RequestedTasks
        {
            get
            {
                return Task.RequestedTasksForPerson(this);
            }
        }

        static public Person AddNewInstance(string name)
        {

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(Person));
            return new Person(name);
        }

        #endregion

        //////// Database access


        override public void CopyDetails(Person instanceToMerge)
        {
            CopyDetailsBase(instanceToMerge);

            m_name = instanceToMerge.Name;
            m_dbLogin = instanceToMerge.DBLogin;
            m_userType = instanceToMerge.UserType;
            m_isResource = instanceToMerge.IsResource;
            m_isActive = instanceToMerge.IsActive;
            m_colourName = instanceToMerge.ColourName;

            StatusNotModified();

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(Person));
        }

        static Person()
        {
            AllInstanceQuery = "select * from " + DatabaseBase.Schema + "Person";

            OtherPerson = new Person(DBTaskMan.Task.OtherResource);
            OtherPerson.NeverSave = true;
            OtherPerson.StatusNotModified();
        }

        public static Person OtherPerson { get; private set; }

        public Person()
        { }

        private Person(string name)
        {
            AddNewInstance(this);
            m_name = name;
            m_dbLogin = null;
            m_userType = Permissions.UserLevel.ReadOnlyUser;
            m_colourName = System.Drawing.Color.SlateGray.ToString();
        }

        static public Person FindPerson(string name)
        {
            foreach (Person thisPerson in AllInstances)
            {
                if (thisPerson.Name == name)
                    return thisPerson;
            }

            return null;

        }

        static public Person FindPersonFromDBLogin(string loginName)
        {
            foreach (Person thisPerson in AllInstances)
            {
                if (thisPerson.DBLogin == loginName.ToLower())
                    return thisPerson;
            }

            return null;

        }

        static public Person UnassignedResource
        {
            get
            {
                if (m_unassignedResource == null)
                {
                    m_unassignedResource = FindPerson("Unassigned");
                }
                return m_unassignedResource;
            }
        }

        static private Person m_unassignedResource = null;

        static public Person CurrentUser
        {
            get
            {
                if (m_currentUser == null)
                {
                    m_currentUser = FindPersonFromDBLogin(DBAccess.DBObjectBase.DBUser);
                }
                return m_currentUser;
            }
        }
        static private Person m_currentUser = null;

        protected override void CreateFromFile(DBAccess.FileSystem_File file)
        {
            m_name = file.GetString("Name");
            m_dbLogin = file.GetString("DBLogin");
            string userTypeStr = file.GetString("UserType");

            Permissions.UserLevel tmpUserType;
            if (Enum.TryParse<Permissions.UserLevel>(userTypeStr, true, out tmpUserType))
                m_userType = tmpUserType;
            else
                m_userType = Permissions.UserLevel.ReadOnlyUser;

            m_colourName = file.GetString("Colour");

            bool? isResource = file.GetBool("IsResource");
            m_isResource = isResource.HasValue ? isResource.Value : false;

            bool? isActive = file.GetBool("IsActive");
            m_isActive = isActive.HasValue ? isActive.Value : true;

        }


        protected override void CreateFromReader(DbDataReader record)
        {

            if (!s_setupHasBeenDone)
            {
                s_setupHasBeenDone = true;
                SetUpFromReader(record);
            }

            DatabaseId = DatabaseBase.GetColumnValueAs<int>(record, s_columnPersonId).Value;
            m_name = DatabaseBase.GetColumnValueAsString(record, s_columnName);
            m_dbLogin = DatabaseBase.GetColumnValueAsString(record, s_columnDBLogin);
            string userTypeStr = DatabaseBase.GetColumnValueAsString(record, s_columnUserType);

            Permissions.UserLevel tmpUserType;
            if (Enum.TryParse<Permissions.UserLevel>(userTypeStr, true, out tmpUserType))
                m_userType = tmpUserType;
            else
                m_userType = Permissions.UserLevel.ReadOnlyUser;

            m_colourName = DatabaseBase.GetColumnValueAsString(record, s_columnColour);

            bool? isResource = DatabaseBase.GetColumnValueAs<bool>(record, s_columnIsResource);
            m_isResource = isResource.HasValue ? isResource.Value : false;

            bool? isActive = DatabaseBase.GetColumnValueAs<bool>(record, s_columnIsActive);
            m_isActive = isActive.HasValue ? isActive.Value : true;

        }


        protected override DBAccess.FileSystem_File FileSystemSaveObject()
        {
            DBAccess.FileSystem_File result = new DBAccess.FileSystem_File(this);

            result.Add("Name", m_name);
            result.Add("DBLogin", m_dbLogin == null ? null : m_dbLogin.ToLower());
            result.Add("UserType", m_userType.ToString());
            result.Add("IsResource", m_isResource);
            result.Add("IsActive", m_isActive);
            result.Add("Colour", m_colourName != null ? m_colourName.ToString() : null);

            return result;

        }


        protected override DbCommand SaveToDataBaseSQLCommand()
        {
            SqlInsert sqlForInstert = new SqlInsert("Person", DatabaseId);
            sqlForInstert.AddParameter("Name", System.Data.SqlDbType.NVarChar, 100, m_name);
            sqlForInstert.AddParameter("DBLogin", System.Data.SqlDbType.NVarChar, 50, m_dbLogin);
            sqlForInstert.AddParameter("UserType", System.Data.SqlDbType.NVarChar, 20, m_userType.ToString());
            sqlForInstert.AddParameter("IsResource", System.Data.SqlDbType.Bit, m_isResource);
            sqlForInstert.AddParameter("IsActive", System.Data.SqlDbType.Bit, m_isActive);
            sqlForInstert.AddParameter("Colour", System.Data.SqlDbType.NVarChar, 25, m_colourName);

            return sqlForInstert.Command;
        }

        protected override DbCommand DeleteFromDataBaseSQLCommand()
        {
            SqlDelete deleteSql = new SqlDelete("Person", DatabaseId);
            return deleteSql.Command;
        }


        static protected void SetUpFromReader(DbDataReader tempateRecord)
        {

            s_columnPersonId = tempateRecord.GetOrdinal("PersonId");
            s_columnName = tempateRecord.GetOrdinal("Name");
            s_columnDBLogin = tempateRecord.GetOrdinal("DBLogin");
            s_columnUserType = tempateRecord.GetOrdinal("UserType");
            s_columnIsResource = tempateRecord.GetOrdinal("IsResource");
            s_columnIsActive = tempateRecord.GetOrdinal("IsActive");
            s_columnColour = tempateRecord.GetOrdinal("Colour");

        }

        static bool s_setupHasBeenDone = false;

        static int s_columnPersonId;
        static int s_columnName;
        static int s_columnDBLogin;
        static int s_columnUserType;
        static int s_columnIsResource;
        static int s_columnIsActive;
        static int s_columnColour;
    }




}
