using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CustomGUIControls
{
    public interface IView
    {
        void Redisplay(IDisplayItem itemToDisplay);
        void WindowClosed();
        void AddDisplayItem(IDisplayItem itemToDisplay);
        void RemoveDisplayItem(IDisplayItem itemToDisplay);
        
    }
}
