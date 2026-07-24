using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBTaskMan;
using CustomGUIControls.Grid;

namespace TaskMan.Projects
{
    class GUIProjectColumns : IGridColumns
    {

        private static GUIProjectColumns s_instance = null;

        private GUIProjectColumns()
        { }

        public static GUIProjectColumns Instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = new GUIProjectColumns();
                return s_instance;
            }
        }

        internal const string s_priortyVHigh = "5-HIGH";
        internal const string s_priortyHigh = "4-HIGH";
        internal const string s_priortyMed = "3-MED";
        internal const string s_priortyLow = "2-LOW";
        internal const string s_priortyVLow = "1-LOW";
        internal const string s_priortyCancelled = "0-Cancelled";
        internal const string s_priortyClosed = "0-Closed";
        internal const string s_priortyNull = "";

        internal const string s_Name = "Name";
        internal const string s_Priority = "Priority";
        internal const string s_ParentName = "ParentName";
        internal const string s_IsActive = "IsActive";
        internal const string s_TotalActiveTaskCount = "ActiveTasks";
        internal const string s_TotalActiveTaskEffort = "Effort";
        internal const string s_Owner = "Owner";
        internal const string s_DueDate = "DueDate";
        internal const string s_StartDate = "StartDate";
        internal const string s_EndDate = "EndDate";

        static List<string> s_priorityComboValues = null;


        public IEnumerable<string> ColumnNames
        {
            get
            {
                List<string> columns = new List<string>();

                columns.Add(s_Name);
                columns.Add(s_Priority);
                columns.Add(s_ParentName);
                columns.Add(s_IsActive);
                columns.Add(s_TotalActiveTaskCount);
                columns.Add(s_TotalActiveTaskEffort);
                columns.Add(s_Owner);
                columns.Add(s_StartDate);
                columns.Add(s_DueDate);

                return columns;
            }
        }


        public string ColumnFormat(string columnName)
        {
            if (columnName == s_DueDate || columnName == s_StartDate)
                return "dd-MMM-yyyy";
            return null;
        }


        public CustomGUIControls.Grid.GridControl.ColumnTypes ColumnType(string columnName)
        {
            if (columnName == s_Priority)
                return CustomGUIControls.Grid.GridControl.ColumnTypes.DropDown;
            return CustomGUIControls.Grid.GridControl.ColumnTypes.Text;
        }
        
        public System.Windows.Forms.DataGridViewContentAlignment? ColumnAlignment(string columnName) { return null; }

        public IList<string> GetComboValues(string columnName)
        {
            return GetComboValues_static(columnName);
        }

        static public IList<string> GetComboValues_static(string columnName)
        {
            if (s_priorityComboValues == null)
            {
                s_priorityComboValues = new List<string>();
                s_priorityComboValues.Add(s_priortyVHigh);
                s_priorityComboValues.Add(s_priortyHigh);
                s_priorityComboValues.Add(s_priortyMed);
                s_priorityComboValues.Add(s_priortyLow);
                s_priorityComboValues.Add(s_priortyVLow);
                s_priorityComboValues.Add(s_priortyCancelled);
                s_priorityComboValues.Add(s_priortyClosed);
                s_priorityComboValues.Add(s_priortyNull);
            }


            switch (columnName)
            {
                case s_Priority:
                    return s_priorityComboValues;

            }

            return null;

        }

        public bool ColumnIsReadOnly(string columnName)
        {
            if (columnName == s_Name ||
                columnName == s_ParentName ||
                columnName == s_IsActive ||
                columnName == s_TotalActiveTaskCount ||
                columnName == s_TotalActiveTaskEffort ||
                columnName == s_StartDate ||
                columnName == s_Owner)
                return true;
            return false;
        }

        public string MultiValueSeparator(string columnName)
        {
            return null;

        }

        public void AdjustComboEditor(string columnName, IGridItem underlyingItem,
                                     System.Windows.Forms.DataGridViewComboBoxEditingControl comboEditor,
                                        GridControl theGrid)
        {
            return;
        }
    }
}

