using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using Utils;


namespace ProjectPal.Links
{
    public class GUILink : CustomGUIControls.Grid.IGridItem
    {
        CustomGUIControls.DisplayItemImpl m_displayItem = null;

        public DataGridViewCellStyle GetCellStyle(string columnName, DataGridViewCellStyle defaultStyle)
        {
            return null;
        }

        private DBProjectPal.Link m_dbLink;
        static public GUILink GetInstanceFromDBLink(DBProjectPal.Link dbLink)
        {
            GUILink result = null;
            if (!m_instances.TryGetValue(dbLink, out result))
            {
                result = new GUILink(dbLink);
            }
            return result;
        }

        static Dictionary<DBProjectPal.Link, GUILink> m_instances = new Dictionary<DBProjectPal.Link, GUILink>();

        private GUILink(DBProjectPal.Link dbLink)
        {
            m_displayItem = new CustomGUIControls.DisplayItemImpl(this);

            m_dbLink = dbLink;

            m_instances.Add(dbLink, this);
        }

        public String CreatedBy
        {
            get { return m_dbLink.Owner; }
        }
        public DateTime ModifiedTime
        {
            get { return m_dbLink.ModifiedTime.Value; }
        }
        public String LinkText
        {
            get { return m_dbLink.LinkText; }
            set { m_dbLink.LinkText = value; }
        }

        public string Owner
        {
            get
            {
                return m_dbLink.Owner;
            }
        }


        public object GetFieldValue(string columnName)
        {
            switch (columnName)
            {
                case GUILinkColumns.s_owner: return RemoveDomain(CreatedBy);
                case GUILinkColumns.s_createTime: return ModifiedTime;
                case GUILinkColumns.s_link: return Clean(LinkText);
            }
            throw new Exception("There is no column called '" + columnName + "'");
        }


        private string RemoveDomain(string inputStr)
        {
            if (inputStr == null)
                return null;
            string[] parts = inputStr.Split(new char[] { '\\', '/' });
            return parts[parts.Length - 1];

        }


        private string Clean(string inputStr)
        {
            StringBuilder result = new StringBuilder();
            bool spaceWasLastAdded = false;
            foreach (char c in inputStr)
            {
                if (c > 31)
                {
                    result.Append(c);
                    spaceWasLastAdded = (c == ' ');

                }
                else
                    if (!spaceWasLastAdded)
                    {
                        result.Append(' ');
                        spaceWasLastAdded = true;
                    }

            }
            return result.ToString();
        }

        public void DisplayItemDeleted()
        {
            m_displayItem.DisplayItemDeleted();
        }

        public void Redisplay()
        {
            m_displayItem.Redisplay();
        }

        public void AddView(CustomGUIControls.IView view)
        {
            m_displayItem.AddView(view);
        }

        public void RemoveView(CustomGUIControls.IView view)
        {
            m_displayItem.RemoveView(view);
        }

        public Color Colour { get { return Color.White; } }

        public bool PopulateDragDropDataObject(System.Windows.Forms.DataObject dragdropDataContainer)
        {
            return false;
        }

        public void SetField(string columnName, string value)
        { }



        static public void OpenLink(CustomGUIControls.IDisplayItem objectToDisplay)
        {
            GUILink Link = objectToDisplay as GUILink;
            if (Link != null)
            {
                LinkWindow.ShowLinkWindow(objectToDisplay);


            }
        }

        public void RedisplayTask()
        {
            ProjectPal.Tasks.GUITask.GetInstanceFromDBTask(m_dbLink.Task).Redisplay();
        }


        static public bool ConfirmDeleteLink(CustomGUIControls.IDisplayItem link)
        {
            Links.GUILink guiLink = link as Links.GUILink;
            if (guiLink != null)
            {

                if (Permissions.IsAllowed(guiLink.CreatedBy, Permissions.EntityType.Link, Permissions.ChangeType.Delete))
                {
                    if (System.Windows.MessageBox.Show("Are you sure you want to delete the link?", "Delete Link",
                        System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question)
                        == System.Windows.MessageBoxResult.Yes)
                    {
                        return true;

                    }
                }

            }
            return false;
        }

        static public void DeleteLink(CustomGUIControls.IDisplayItem link)
        {
            Links.GUILink guiLink = link as Links.GUILink;
            if (guiLink != null && Permissions.IsAllowed(guiLink.Owner, Permissions.EntityType.Link, Permissions.ChangeType.Delete))
            {
                guiLink.DeleteInstance();
            }
        }

        public void DeleteInstance()
        {
            DisplayItemDeleted();
            m_dbLink.DeleteInstance();
        }

        public void SaveToDB()
        {
            DBAccess.DBObjectBase.Save(m_dbLink);
        }

        public bool IsReadOnly(string columnName)
        {
            return false;
        }



        public bool IsActive()
        {
            return true;
        }


        public string ObjectDescription { get { return m_dbLink.ObjectDescription; } }

        public bool IsDeleted { get { return m_dbLink.IsDeleted; } }

        public bool IsPrivateToOtherUser { get { return false; } }


        public void GridCellDragEnter(DragEventArgs e) { return; }
        public void GridCellDragDrop(DragEventArgs e) { return; }
        public void GridCellDragLeave(EventArgs e) { return; }

    }
}
