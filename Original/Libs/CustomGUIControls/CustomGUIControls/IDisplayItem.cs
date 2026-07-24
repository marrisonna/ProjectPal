using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CustomGUIControls
{
    public interface IDisplayItem
    {
        void AddView(IView view);
        void RemoveView(IView view);
        void Redisplay();
        void DisplayItemDeleted();
    }
}
