using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBProjectPal;
using ProjectPal.Tasks;
using ProjectPal.Components;
using System.Windows.Forms;

namespace ProjectPal
{
    internal class FindResult : CustomGUIControls.Grid.IGridItem
    {
        CustomGUIControls.DisplayItemImpl m_displayItem = null;

        public DataGridViewCellStyle GetCellStyle(string columnName, DataGridViewCellStyle defaultStyle)
        {
            return null;
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
            m_displayItem.Redisplay();
        }

        public Color Colour
        {
            get
            {
                if (m_foundItem is Task)
                {
                    Task theTask = (m_foundItem as Task);

                    if (theTask.Priority.Value == PriorityValue._0_Cancelled ||
                         theTask.Priority.Value == PriorityValue._0_Closed)
                        return Utils.Colours.ReadOnlyColour;
                    return Utils.Colours.ReadWriteColour;
                }

                if (m_foundItem is Component)
                {
                    Component theComponent = (m_foundItem as Component);
                    if (theComponent.TotalActiveTaskCount > 0)
                        return Utils.Colours.ReadWriteColour;
                    return Utils.Colours.ReadOnlyColour;
                }

                if (m_foundItem is Project)
                {
                    Project theProject = (m_foundItem as Project);
                    if (theProject.Priority == PriorityValue._0_Cancelled ||
                        theProject.Priority == PriorityValue._0_Closed ||
                        theProject.TotalActiveTaskCount == 0)
                        return Utils.Colours.ReadOnlyColour;

                    return Utils.Colours.ReadWriteColour;
                }

                if (m_foundItem is Remark)
                    return Utils.Colours.ReadWriteColour;

                return Utils.Colours.ReadOnlyColour;
            }
        }


        public bool PopulateDragDropDataObject(System.Windows.Forms.DataObject dragdropDataContainer)
        {
            return false;
        }

        public void SetField(string columnName, string value)
        { }

        public object GetFieldValue(string columnName)
        {
            switch (columnName)
            {
                case FindResultColumns.s_Type: return TypeString;
                case FindResultColumns.s_Description: return Description;
                case FindResultColumns.s_Date: return Date;
                case FindResultColumns.s_Person: return Person;
            }
            throw new Exception("There is no column called '" + columnName + "'");
        }


        private ISearchable m_foundItem;

        internal ISearchable FoundItem { get { return m_foundItem; } }


        public FindResult(ISearchable foundItem)
        {
            m_displayItem = new CustomGUIControls.DisplayItemImpl(this);

            m_foundItem = foundItem;
        }

        public string TypeString
        {
            get
            {
                if (m_foundItem is Task)
                    return "Task";
                if (m_foundItem is Component)
                    return "Component";
                if (m_foundItem is Project)
                    return "Project";
                if (m_foundItem is Remark)
                    return "Remark";
                return "?";
            }
        }

        public string ObjectDescription
        {
            get
            {
                return "FindResult: " + (m_foundItem == null ? "null" :
                                           (m_foundItem.GetType().Name + ": " + m_foundItem.ObjectDescription));
            }
        }

        public string Description
        {
            get
            {
                if (m_foundItem is Task)
                    return Clean((m_foundItem as Task).Description);
                if (m_foundItem is Component)
                    return (m_foundItem as Component).Name;
                if (m_foundItem is Project)
                    return (m_foundItem as Project).Name;
                if (m_foundItem is Remark)
                    return Clean((m_foundItem as Remark).RemarkText);
                return "?";
            }
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


        public string Person
        {
            get
            {
                if (m_foundItem is Task)
                {
                    string result = "";
                    foreach (IResource resource in (m_foundItem as Task).Resources)
                    {
                        if (result.Length > 0)
                            result += ", ";
                        result += resource.Name;
                    }
                    return result;
                }
                if (m_foundItem is Remark)
                {
                    return (m_foundItem as Remark).Owner;
                }
                if (m_foundItem is Project)
                {
                    Person projectOwner = DBProjectPal.Person.FindPersonFromDBLogin((m_foundItem as Project).Owner);
                    if (projectOwner != null)
                        return projectOwner.Name;
                }
                return "";
            }
        }

        public DateTime? Date
        {
            get
            {
                if (m_foundItem is Task)
                {
                    Task theTask = (m_foundItem as Task);
                    if (theTask.Status == StatusValue.Cancelled ||
                        theTask.Status == StatusValue.Closed)
                        return theTask.StatusDate;
                    return theTask.EndDate;
                }
                if (m_foundItem is Remark)
                {
                    return (m_foundItem as Remark).ModifiedTime;
                }
                if (m_foundItem is Project)
                {
                    return (m_foundItem as Project).StartDate;
                }
                return null;
            }
        }

        public bool IsReadOnly(string columnName)
        {
            return false;
        }



        public bool IsActive()
        {
            if (m_foundItem is Task)
            {
                Task foundTask = m_foundItem as Task;
                if (foundTask.Status == StatusValue.Cancelled ||
                    foundTask.Status == StatusValue.Closed)
                    return false;
                return true;
            }

            if (m_foundItem is Project)
            {
                Project foundProject = m_foundItem as Project;
                if (foundProject.Priority == PriorityValue._0_Cancelled ||
                    foundProject.Priority == PriorityValue._0_Closed ||
                    foundProject.TotalActiveTaskCount == 0)
                    return false;
                return true;
            }

            if (m_foundItem is Component)
            {
                Component foundComponent = m_foundItem as Component;
                if (foundComponent.ActiveTaskCount == 0)
                    return false;
                return true;
            }

            if (m_foundItem is Remark)
            {
                return true;
            }
            return false;
        }


        public bool IsDeleted { get { return false; } }

        public bool IsPrivateToOtherUser { get { return false; } }


        public void GridCellDragEnter(DragEventArgs e) { return; }
        public void GridCellDragDrop(DragEventArgs e) { return; }
        public void GridCellDragLeave(EventArgs e) { return; }

    }
}