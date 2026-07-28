using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CustomGUIControls
{
    public interface IGridItem
    {
        IEnumerable<string> ColumnNames { get; }
        IEnumerable<object> FieldValues { get; }
        string ColumnFormat(string columnName);
        GridControl.ColumnTypes ColumnType(string columnName);
        void SetField(string columnName, string value);
        Color Colour { get; }
    }
}
