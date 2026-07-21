using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace TaskMan.Reports
{
    class GUIMainReportItem : CustomGUIControls.Grid.IGridItem
    {
        public DataGridViewCellStyle GetCellStyle(string columnName, DataGridViewCellStyle defaultStyle)
        {
            return null;
        }

        public object GetFieldValue(string columnName)
        {
            switch (columnName)
            {
                case GUIMainReportItemColumns.s_Person: return m_personName;
                case GUIMainReportItemColumns.s_NumberOfOpenTasks: return m_taskCount;
                case GUIMainReportItemColumns.s_Effort: return m_totalEffortInDays;
                case GUIMainReportItemColumns.s_MaxUrgency: return m_maxUrgency;
                case GUIMainReportItemColumns.s_AveUrgency: return AveUrgency;
                case GUIMainReportItemColumns.s_NumberOfReadyTasks: return m_ready;
                case GUIMainReportItemColumns.s_NumberOfInProgressTasks: return m_inProgess;

            }
            throw new Exception("There is no column called '" + columnName + "'");
        }

        public string Person { get { return m_personName; } }
        public double MaxUrgency { get { return m_maxUrgency; } }
        public double AveUrgency { get { return m_taskCount == 0 ? 0 : m_totalUrgency / m_taskCount; } }

        public GUIMainReportItem(string personName)
        {
            m_personName = personName;
            m_taskCount = 0;
            m_totalEffortInDays = 0;
            m_maxUrgency = 0;
            m_totalUrgency = 0;
            m_ready = 0;
            m_inProgess = 0;
        }

        public void AddTaskData(double effort, double urgency, DBTaskMan.StatusValue? taskStatus)
        {
            m_taskCount++;
            m_totalEffortInDays += effort;
            m_totalUrgency += urgency;
            m_maxUrgency = Math.Max(m_maxUrgency, urgency);

            if (taskStatus == DBTaskMan.StatusValue.Ready)
                m_ready++;

            if (taskStatus == DBTaskMan.StatusValue.InProgress)
                m_inProgess++;
        }

        string m_personName;
        int m_taskCount;
        int m_inProgess;
        int m_ready;
        double m_totalEffortInDays;
        double m_maxUrgency;
        double m_totalUrgency;




        public bool PopulateDragDropDataObject(System.Windows.Forms.DataObject dragdropDataContainer)
        {
            return false;
        }

        public void SetField(string columnName, string value)
        { }

        public Color Colour { get { return Utils.Colours.UrgencyColour(AveUrgency); } }


        public void DisplayItemDeleted() { }
        public void Redisplay() { }
        public void RemoveView(CustomGUIControls.IView view) { }
        public void AddView(CustomGUIControls.IView view) { }

        public string ObjectDescription { get { return m_personName; } }
        public bool IsPrivateToOtherUser { get { return false; } }
        public bool IsDeleted { get { return false; } }

        public void GridCellDragLeave(EventArgs e) { return; }
        public void GridCellDragDrop(DragEventArgs e) { return; }
        public void GridCellDragEnter(DragEventArgs e) { return; }

        public bool IsActive() { return true; }
        public bool IsReadOnly(string columnName) { return true; }

    }
}
