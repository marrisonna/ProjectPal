using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CustomGUIControls.Grid;

namespace TaskMan
{
    class FindResultColumns : IGridColumns
    {
        private static FindResultColumns s_instance = null;

        private FindResultColumns()
        { }

        public static FindResultColumns Instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = new FindResultColumns();
                return s_instance;
            }
        }

        internal const string s_Type = "Type";
        internal const string s_Description = "Description";
        internal const string s_Person = "Person";
        internal const string s_Date = "Date";
        

        public IEnumerable<string> ColumnNames
        {
            get
            {
                List<string> columns = new List<string>();
                columns.Add(s_Type);
                columns.Add(s_Description);
                columns.Add(s_Person);
                columns.Add(s_Date);
                return columns;
            }
        }

        public string ColumnFormat(string columnName)
        {
            if (columnName == s_Date )
                return "dd-MMM-yyyy";
            return null;
        }

        public CustomGUIControls.Grid.GridControl.ColumnTypes ColumnType(string columnName)
        {
            return CustomGUIControls.Grid.GridControl.ColumnTypes.Text;
        }

        public System.Windows.Forms.DataGridViewContentAlignment? ColumnAlignment(string columnName) { return null; }

        public IList<string> GetComboValues(string columnName)
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
        {
            return;
        }
       
    }
}
