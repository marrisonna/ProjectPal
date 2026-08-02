using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

using Utils;

namespace ProjectPal.People
{
    public class GUIPerson : CustomGUIControls.Grid.IGridItem
    {

        internal DBProjectPal.Person DBPerson { get { return m_dbPerson; } }

        private DBProjectPal.Person m_dbPerson;


        public DataGridViewCellStyle GetCellStyle(string columnName, DataGridViewCellStyle defaultStyle)
        {
            DataGridViewCellStyle result = defaultStyle.Clone();


            if (columnName == GUIPersonColumns.s_ColourBlock)
            {
                System.Windows.Media.Color personColourA = this.m_dbPerson.Colour;
                System.Drawing.Color personColourB = System.Drawing.Color.FromArgb(personColourA.A, personColourA.R, personColourA.G, personColourA.B);
                result.BackColor = personColourB;
            }


            return result;
        }

        public object GetFieldValue(string columnName)
        {
            switch (columnName)
            {
                case GUIPersonColumns.s_Name: return Name;
                case GUIPersonColumns.s_IsResource: return IsResource;
                case GUIPersonColumns.s_IsActive: return IsActivePerson;
                case GUIPersonColumns.s_DBLogin: return DBLogin;
                case GUIPersonColumns.s_UserType: return UserType;
                case GUIPersonColumns.s_Colour: return ColourName;
                case GUIPersonColumns.s_ColourBlock: return " ";

            }
            throw new Exception("There is no column called '" + columnName + "'");
        }



        public bool PopulateDragDropDataObject(System.Windows.Forms.DataObject dragdropDataContainer)
        {
            return false;
        }


        public void SetField(string columnName, string value)
        {
            switch (columnName)
            {
                case GUIPersonColumns.s_Name:
                    Name = value;
                    break;
                case GUIPersonColumns.s_IsResource:
                    IsResource = value;
                    break;
                case GUIPersonColumns.s_IsActive:
                    IsActivePerson = value;
                    break;
                case GUIPersonColumns.s_DBLogin:
                    DBLogin = value;
                    break;
                case GUIPersonColumns.s_UserType:
                    UserType = value;
                    break;
                case GUIPersonColumns.s_Colour:
                    ColourName = value;

                    break;
                case GUIPersonColumns.s_ColourBlock:
                    break;


            }
        }

        public Color Colour
        {
            get
            {
                if (!Permissions.IsSuperUser)
                    return Utils.Colours.ReadOnlyColour;

                return Utils.Colours.ReadWriteColour;

            }
        }





        static public GUIPerson GetInstanceFromDPerson(DBProjectPal.Person dbPerson)
        {
            GUIPerson result = null;
            if (!m_instances.TryGetValue(dbPerson, out result))
            {
                result = new GUIPerson(dbPerson);
            }
            return result;
        }

        static public GUIPerson GetExistingInstanceFromDBPerson(DBProjectPal.Person dbTask)
        {
            GUIPerson result = null;
            m_instances.TryGetValue(dbTask, out result);
            return result;
        }

        static Dictionary<DBProjectPal.Person, GUIPerson> m_instances = new Dictionary<DBProjectPal.Person, GUIPerson>();

        private GUIPerson(DBProjectPal.Person dbPerson)
        {
            m_displayItem = new CustomGUIControls.DisplayItemImpl(this);

            m_dbPerson = dbPerson;

            m_instances.Add(dbPerson, this);
        }




        public string IsResource
        {
            get { return m_dbPerson.IsResource ? "Y" : "N"; }
            set { m_dbPerson.IsResource = (value == "Y"); }
        }
        public string IsActivePerson
        {
            get { return m_dbPerson.IsActive ? "Y" : "N"; }
            set { m_dbPerson.IsActive = (value == "Y"); }
        }
        public string DBLogin
        {
            get { return m_dbPerson.DBLogin; }
            set { m_dbPerson.DBLogin = value; }
        }
        public string Name
        {
            get { return m_dbPerson.Name; }
            set { m_dbPerson.Name = value; }
        }
        public string UserType
        {
            get { return m_dbPerson.UserType.ToString(); }
            set
            {
                Permissions.UserLevel userType;
                if (!Enum.TryParse(value, out userType))
                {
                    userType = Permissions.UserLevel.ReadOnlyUser;
                }
                m_dbPerson.UserType = userType;
            }
        }
        public string ColourName
        {
            get { return m_dbPerson.ColourName; }
            set
            {
                m_dbPerson.ColourName = value;
                GUIPersonColumns.ResetColours();
                GanttDisplayHelper.ClearColourCache();
            }
        }


        public void AddView(CustomGUIControls.IView view)
        {
            m_displayItem.AddView(view);
        }

        public void RemoveView(CustomGUIControls.IView view)
        {
            m_displayItem.RemoveView(view);
        }

        public void DisplayItemDeleted()
        {
            m_displayItem.DisplayItemDeleted();
        }


        public void Redisplay()
        {

            if (CustomGUIControls.RedisplayManager.Instance.HasItemAlreadyBeenRedisplayed(this))
                return;

            m_displayItem.Redisplay();

            ApplicationProjectPal.Instance.RefreshAllWindows();

        }



        CustomGUIControls.DisplayItemImpl m_displayItem = null;

        public void DeleteInstance()
        {
            DisplayItemDeleted();
            m_dbPerson.DeleteInstance();
        }

        static public void DeleteTask(CustomGUIControls.IDisplayItem task)
        {
            ProjectPal.Tasks.GUITask guiTask = task as ProjectPal.Tasks.GUITask;
            if (guiTask != null)
            {
                guiTask.DeleteInstance();
            }
        }

        static public bool ConfirmDeleteTask(CustomGUIControls.IDisplayItem task)
        {
            ProjectPal.Tasks.GUITask guiTask = task as ProjectPal.Tasks.GUITask;
            if (guiTask != null && Permissions.IsAllowed(guiTask.Owner, Permissions.EntityType.Task, Permissions.ChangeType.Delete))
            {
                if (System.Windows.MessageBox.Show("Are you sure you want to delete the Task?", "Delete Task",
                    System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question)
                    == System.Windows.MessageBoxResult.Yes)
                {
                    return true;

                }

            }
            return false;
        }


        IList<string> NormalUserEditableColumns
        {
            get
            {
                List<string> result = new List<string>();
                return result;
            }
        }


        public bool IsDeleted { get { return m_dbPerson.IsDeleted; } }


        public bool IsReadOnly(string columnName)
        {
            return columnName == GUIPersonColumns.s_ColourBlock || !Permissions.IsSuperUser;

        }

        public string ObjectDescription { get { return m_dbPerson.ObjectDescription; } }


        public bool IsPrivateToOtherUser { get { return m_dbPerson.IsPrivateToAnotherAndHidden; } }

        public bool IsActive()
        {
            return m_dbPerson.IsActive;
        }


        public void GridCellDragEnter(DragEventArgs e)
        {
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
        }



        public void GridCellDragDrop(DragEventArgs e)
        {

        }
        public void GridCellDragLeave(EventArgs e) { return; }
    }
}
