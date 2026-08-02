using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;

namespace CustomGUIControls.Grid
{
    public interface IGridColumns
    {
        IEnumerable<string> ColumnNames { get; }
        string ColumnFormat(string columnName);
        GridControl.ColumnTypes ColumnType(string columnName);
        DataGridViewContentAlignment? ColumnAlignment(string columnName);
        IList<string> GetComboValues(string columnName);
        bool ColumnIsReadOnly(string columnName);
        string MultiValueSeparator(string columnName);
        void AdjustComboEditor(string columnName, IGridItem underlyingItem, 
            System.Windows.Forms.DataGridViewComboBoxEditingControl comboEditor,
            GridControl theGrid);
        
    }
}
