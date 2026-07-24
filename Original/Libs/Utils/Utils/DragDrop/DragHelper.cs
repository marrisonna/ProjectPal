using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Utils.DragDrop
{
    public class DragHelper
    {

        static public DataObject SetDraggedObject(object draggedObject)
        {
            m_objectedBeingDragged = draggedObject;

            DataObject data = new DataObject();
            data.SetData("ProjectPal", 1);

            return data;
        }

        static public System.Windows.DataObject SetDraggedObjectWPF(object draggedObject)
        {
            m_objectedBeingDragged = draggedObject;

            System.Windows.DataObject data = new System.Windows.DataObject();
            data.SetData("ProjectPal", 1);

            return data;
        }

        static public object DraggedObject
        {
            get
            {
                return m_objectedBeingDragged;
            }
        }

        static private object m_objectedBeingDragged = null;
    }
}
