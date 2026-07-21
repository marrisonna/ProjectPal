using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CustomGUIControls.Grid;

namespace TaskMan.Attachments
{
    class GUIAttachmentColumns : IGridColumns
    {

        internal const string s_Name = "Name";
        internal const string s_DataType = "DataType";
        internal const string s_DataFrom = "From";
        internal const string s_CreateTime = "CreateTime";
        internal const string s_Size = "Size";
        internal const string s_Owner = "Owner";


        private static GUIAttachmentColumns s_instance = null;

        private GUIAttachmentColumns()
        { }

        public static GUIAttachmentColumns Instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = new GUIAttachmentColumns();
                return s_instance;
            }
        }

        public IEnumerable<string> ColumnNames
        {
            get
            {
                List<string> columns = new List<string>();
                columns.Add(s_Name);
                columns.Add(s_DataFrom);
                columns.Add(s_CreateTime);
                columns.Add(s_Size);
                columns.Add(s_DataType);
                columns.Add(s_Owner);

                return columns;
            }
        }

        public string ColumnFormat(string columnName)
        {
            if (columnName == s_CreateTime)
                return "dd-MMM-yyyy HH:mm:ss";
            if (columnName == s_Size)
                return @"#0 k";
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
