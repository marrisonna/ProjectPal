using System;
using System.Collections.Generic;
using System.Text;
using CustomGUIControls.Grid;

namespace TaskMan.Reports
{
    class GUIMainReportItemColumns : IGridColumns
    {
        private static GUIMainReportItemColumns s_instance = null;

        private GUIMainReportItemColumns()
        { }

        public static GUIMainReportItemColumns Instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = new GUIMainReportItemColumns();
                return s_instance;
            }
        }

        internal const string s_Person = "Person";
        internal const string s_NumberOfOpenTasks = "#Tasks";
        internal const string s_Effort = "Effort";
        internal const string s_MaxUrgency = "Max Urgency";
        internal const string s_AveUrgency = "Ave Urgency";
        internal const string s_NumberOfInProgressTasks = "#InProgress";
        internal const string s_NumberOfReadyTasks = "#Ready";


        public IEnumerable<string> ColumnNames
        {
            get
            {
                List<string> columns = new List<string>();

                columns.Add(s_Person);
                columns.Add(s_NumberOfOpenTasks);
                columns.Add(s_NumberOfReadyTasks);
                columns.Add(s_NumberOfInProgressTasks);
                columns.Add(s_Effort);
                columns.Add(s_MaxUrgency);
                columns.Add(s_AveUrgency);

                return columns;
            }
        }

        public string ColumnFormat(string columnName)
        {
            if (columnName == s_Effort || columnName == s_AveUrgency || columnName == s_MaxUrgency)
                return "#0.0";
            //if (columnName == s_StartDate || columnName == s_EndDate || columnName == s_DateAdded || columnName == s_StatusDate)
            //    return "dd-MMM-yyyy";
            //if (columnName == s_PercentageAllocation)
            //    return "P0";
            return null;
        }

        public CustomGUIControls.Grid.GridControl.ColumnTypes ColumnType(string columnName)
        {
            return CustomGUIControls.Grid.GridControl.ColumnTypes.Text;
        }

        public System.Windows.Forms.DataGridViewContentAlignment? ColumnAlignment(string columnName)
        {
            if (columnName != s_Person)
                return System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            return null;
        }

        public IList<string> GetComboValues(string columnName)
        {
            return new List<string>();
        }

        static public IList<string> GetComboValues_static(string columnName)
        {
            return null;
        }

        public bool ColumnIsReadOnly(string columnName)
        {
            return true;
        }

        public string MultiValueSeparator(string columnName)
        {
            return null;
        }


        public void AdjustComboEditor(string columnName, IGridItem underlyingItem,
                                    System.Windows.Forms.DataGridViewComboBoxEditingControl comboEditor,
                                        GridControl theGrid)
        { }
    }
}
