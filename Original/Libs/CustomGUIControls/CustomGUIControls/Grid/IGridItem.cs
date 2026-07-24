using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CustomGUIControls.Grid
{
    public interface IGridItem : IDisplayItem
    {
        object GetFieldValue(string columnName);
        bool IsReadOnly(string columnName);
        bool IsActive();
        bool IsDeleted{get;}
        bool IsPrivateToOtherUser { get; }
        void SetField(string columnName, string value);
        Color Colour { get; }
        bool PopulateDragDropDataObject(System.Windows.Forms.DataObject dragdropDataContainer);
        string ObjectDescription { get; }
        DataGridViewCellStyle GetCellStyle(string columnName, DataGridViewCellStyle defaultStyle);

        void GridCellDragEnter(DragEventArgs e);
        void GridCellDragDrop(DragEventArgs e);
        void GridCellDragLeave(EventArgs e);
    }
}
