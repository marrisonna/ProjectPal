using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using Utils;


namespace ProjectPal.Remarks
{
    public class GUIRemark : CustomGUIControls.Grid.IGridItem
    {
        CustomGUIControls.DisplayItemImpl m_displayItem = null;

        public DataGridViewCellStyle GetCellStyle(string columnName, DataGridViewCellStyle defaultStyle)
        {
            return null;
        }

        private DBProjectPal.Remark m_dbRemark;
        static public GUIRemark GetInstanceFromDBRemark(DBProjectPal.Remark dbRemark)
        {
            GUIRemark result = null;
            if (!m_instances.TryGetValue(dbRemark, out result))
            {
                result = new GUIRemark(dbRemark);
            }
            return result;
        }

        static Dictionary<DBProjectPal.Remark, GUIRemark> m_instances = new Dictionary<DBProjectPal.Remark, GUIRemark>();

        private GUIRemark(DBProjectPal.Remark dbRemark)
        {
            m_displayItem = new CustomGUIControls.DisplayItemImpl(this);

            m_dbRemark = dbRemark;

            m_instances.Add(dbRemark, this);
        }

        public String CreatedBy
        {
            get { return m_dbRemark.Owner; }
        }
        public DateTime ModifiedTime
        {
            get { return m_dbRemark.ModifiedTime.Value; }
        }
        public String RemarkText
        {
            get { return m_dbRemark.RemarkText; }
            set { m_dbRemark.RemarkText = value; }
        }

        public string Owner
        {
            get
            {
                return m_dbRemark.Owner;
            }
        }


        public object GetFieldValue(string columnName)
        {
            switch (columnName)
            {
                case GUIRemarkColumns.s_owner: return RemoveDomain(CreatedBy);
                case GUIRemarkColumns.s_createTime: return ModifiedTime;
                case GUIRemarkColumns.s_remark: return Clean(RemarkText);
            }
            throw new Exception("There is no column called '" + columnName + "'");
        }


        private string RemoveDomain(string inputStr)
        {
            if (inputStr == null)
                return null;
            string[] parts = inputStr.Split(new char[]{'\\','/'});
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



        static public void OpenRemark(CustomGUIControls.IDisplayItem objectToDisplay)
        {
            GUIRemark remark = objectToDisplay as GUIRemark;
            if (remark != null)
            {
                RemarkWindow.ShowRemarkWindow(objectToDisplay);

                
            }
        }

        public void RedisplayTask()
        {
            ProjectPal.Tasks.GUITask.GetInstanceFromDBTask(m_dbRemark.Task).Redisplay();
        }


        static public bool ConfirmDeleteRemark(CustomGUIControls.IDisplayItem remark)
        {
            Remarks.GUIRemark guiRemark = remark as Remarks.GUIRemark;
            if (guiRemark != null)
            {

                if (Permissions.IsAllowed(guiRemark.CreatedBy, Permissions.EntityType.Remark, Permissions.ChangeType.Delete))
                {
                    if (System.Windows.MessageBox.Show("Are you sure you want to delete the remark?", "Delete Remark",
                        System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question)
                        == System.Windows.MessageBoxResult.Yes)
                    {
                        return true;

                    }
                }

            }
            return false;
        }

        static public void DeleteRemark(CustomGUIControls.IDisplayItem remark)
        {
            Remarks.GUIRemark guiRemark = remark as Remarks.GUIRemark;
            if (guiRemark != null && Permissions.IsAllowed(guiRemark.Owner,Permissions.EntityType.Remark,Permissions.ChangeType.Delete))
            {
                guiRemark.DeleteInstance();
            }
        }

        public void DeleteInstance()
        {
            DisplayItemDeleted();
            m_dbRemark.DeleteInstance();
        }

        public void SaveToDB()
        {
            DBAccess.DBObjectBase.Save(m_dbRemark);
        }

        public bool IsReadOnly(string columnName)
        {
            return false;
        }



        public bool IsActive()
        {
            return true;
        }


        public string ObjectDescription { get { return m_dbRemark.ObjectDescription; } }

        public bool IsDeleted { get { return m_dbRemark.IsDeleted; } }

        public bool IsPrivateToOtherUser { get { return false; } }


        public void GridCellDragEnter(DragEventArgs e) { return; }
        public void GridCellDragDrop(DragEventArgs e) { return; }
        public void GridCellDragLeave(EventArgs e) { return; }

    }
}
