using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Utils;
using System.Windows.Forms;

namespace TaskMan.Attachments
{



    public class GUIAttachment : CustomGUIControls.Grid.IGridItem
    {

        public DataGridViewCellStyle GetCellStyle(string columnName, DataGridViewCellStyle defaultStyle)
        {
            return null;
        }
      
        private DBTaskMan.Attachment m_dbAttachment;
        static public GUIAttachment GetInstanceFromDBAttachment(DBTaskMan.Attachment dbAttachment)
        {
            GUIAttachment result = null;
            if (!m_instances.TryGetValue(dbAttachment, out result))
            {
                result = new GUIAttachment(dbAttachment);
            }
            return result;
        }

        static Dictionary<DBTaskMan.Attachment, GUIAttachment> m_instances = new Dictionary<DBTaskMan.Attachment, GUIAttachment>();

        private GUIAttachment(DBTaskMan.Attachment dbAttachment)
        {
            m_displayItem = new CustomGUIControls.DisplayItemImpl(this);

            m_dbAttachment = dbAttachment;

            m_instances.Add(dbAttachment, this);
        }


        public string Name
        {
            get { return m_dbAttachment.Name; }
            //set { m_dbAttachment.Name = value;}
        }

        public string DataType
        {
            get { return m_dbAttachment.DataType; }
            //set { m_dbAttachment.Name = value;}
        }

        public string From
        {
            get { return m_dbAttachment.From; }
            //set { m_dbAttachment.Name = value;}
        }

        public string Owner
        {
            get { return m_dbAttachment.Owner; }
            //set { m_dbAttachment.Name = value;}
        }

        public int Size
        {
            get { return 1 + m_dbAttachment.Size / 1024; }
            //set { m_dbAttachment.Name = value;}
        }

        public DateTime? CreateTime
        {
            get { return m_dbAttachment.CreateTime; }
            //set { m_dbAttachment.Name = value;}
        }





        public object GetFieldValue(string columnName)
        {
            switch (columnName)
            {
                case GUIAttachmentColumns.s_Name: return Name;
                case GUIAttachmentColumns.s_DataType: return DataType;
                case GUIAttachmentColumns.s_DataFrom: return From;
                case GUIAttachmentColumns.s_CreateTime: return CreateTime;
                case GUIAttachmentColumns.s_Size: return Size;
                case GUIAttachmentColumns.s_Owner: return Owner;
            }
            throw new Exception("There is no column called '" + columnName + "'");
        }



        public bool PopulateDragDropDataObject(System.Windows.Forms.DataObject dragdropDataContainer)
        {
            string fileName = SaveAttachmentToDisk();
            if (fileName == null)
                return false;
            string[] fileNames = new string[] { fileName };
            dragdropDataContainer.SetData("FileNameW", fileNames);
            return true;
        }


        public void SetField(string columnName, string value)
        { }

        public Color Colour
        {
            get
            {
                return Utils.Colours.ReadWriteColour;
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

        public void Redisplay()
        {
            m_displayItem.Redisplay();
        }

        public void DisplayItemDeleted()
        {
            m_displayItem.DisplayItemDeleted();
        }

        private string SaveAttachmentToDisk()
        {
            return m_dbAttachment.SaveAttachmentToDisk();   
        }

        public void OpenAttachment()
        {
            string fileName = SaveAttachmentToDisk();
            if (fileName != null)
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo ps = new System.Diagnostics.ProcessStartInfo(fileName);
                p.StartInfo = ps;
                p.Start();
            }

            //string[] searchFor = new string[] { "and", "the", "for" };
            //Utils.DocumentSearch.Search(searchFor, m_dbAttachment.Data, Utils.DocumentSearch.DocumentType.OutLookMsg, true);



        }

      
        public void DeleteInstance()
        {
            DisplayItemDeleted();
            m_dbAttachment.DeleteInstance();
        }

        static public void OpenAttachment(CustomGUIControls.IDisplayItem objectToDisplay)
        {

            GUIAttachment underlyingObject = objectToDisplay as GUIAttachment;
            if (underlyingObject != null)
            {
                underlyingObject.OpenAttachment();
            }
        }

        static public void DeleteAttachment(CustomGUIControls.IDisplayItem attachment)
        {

            Attachments.GUIAttachment guiAttachment = attachment as Attachments.GUIAttachment;
            if (guiAttachment != null)
            {
                guiAttachment.DeleteInstance();
            }
        }

        static public bool ConfirmDeleteAttachment(CustomGUIControls.IDisplayItem attachment)
        {
            Attachments.GUIAttachment guiAttachment = attachment as Attachments.GUIAttachment;
            if (guiAttachment != null)
            {
                if (Permissions.IsAllowed(guiAttachment.Owner, Permissions.EntityType.Attachment, Permissions.ChangeType.Delete))
                {
                    if (System.Windows.MessageBox.Show("Are you sure you want to delete the attachment?", "Delete Attachment",
                        System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question)
                        == System.Windows.MessageBoxResult.Yes)
                    {
                        return true;

                    }
                }

            }
            return false;
        }

        public bool IsReadOnly(string columnName)
        {
            if (Permissions.IsAllowed(Owner, Permissions.EntityType.Attachment, Permissions.ChangeType.Delete))
                return false;
            return true;
        }


        public bool IsActive()
        {
            return true;
        }


        public bool IsDeleted { get { return m_dbAttachment.IsDeleted; } }

        public bool IsPrivateToOtherUser { get { return false; } }


        public string ObjectDescription { get { return m_dbAttachment.ObjectDescription; } }

        CustomGUIControls.DisplayItemImpl m_displayItem = null;


        public void GridCellDragEnter(DragEventArgs e) { return; }
        public void GridCellDragDrop(DragEventArgs e) { return; }
        public void GridCellDragLeave(EventArgs e) { return; }
    }
}
