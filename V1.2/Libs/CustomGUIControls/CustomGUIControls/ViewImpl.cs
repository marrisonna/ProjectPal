using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CustomGUIControls
{
    public class ViewImpl
    {
        private IView m_window;

        public ViewImpl(IView window)
        {
            m_window = window;
        }

        public IDisplayItem FirstItemToDisplay
        {
            get
            {
                foreach (IDisplayItem item in ItemsToDisplay)
                    return item;
                return null;
            }
        }

        public HashSet<IDisplayItem> ItemsToDisplay
        {
            get
            {
                return View2DisplayItemMap.ItemsOnView(m_window);
            }
        }



        public void WindowClosed()
        {
            View2DisplayItemMap.WindowClosed(m_window);
        }

        public void AddDisplayItem(IDisplayItem itemToDisplay)
        {
            View2DisplayItemMap.Add(m_window, itemToDisplay);
        }

        public void RemoveDisplayItem(IDisplayItem itemToDisplay)
        {
            View2DisplayItemMap.Remove(m_window, itemToDisplay);
        }


    }
}
