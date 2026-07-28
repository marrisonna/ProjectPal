using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CustomGUIControls.Grid;

namespace ProjectPal.Links
{
    class GUILinkColumns : IGridColumns
    {

        internal const string s_owner = "Owner";
        internal const string s_createTime = "CreateTime";
        internal const string s_link = "Link";


        private static GUILinkColumns s_instance = null;

        private GUILinkColumns()
        { }

        public static GUILinkColumns Instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = new GUILinkColumns();
                return s_instance;
            }
        }

        public IEnumerable<string> ColumnNames
        {
            get
            {
                List<string> columns = new List<string>();
                columns.Add(s_owner);
                columns.Add(s_createTime);
                columns.Add(s_link);


                return columns;
            }
        }

        public string ColumnFormat(string columnName)
        {
            if (columnName == s_createTime)
                return "dd-MMM-yyyy HH:mm:ss";
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
