using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CustomGUIControls
{
    public class DisplayItemImpl : IDisplayItem
    {
        public DisplayItemImpl(IDisplayItem realItem)
        {
            m_realItem = realItem;
        }

        public void AddView(IView view)
        {
            View2DisplayItemMap.Add(view, m_realItem);
        }


        public void RemoveView(IView view)
        {
            View2DisplayItemMap.Remove(view, m_realItem);
        }

        public void DisplayItemDeleted()
        {
            View2DisplayItemMap.DisplayItemDeleted(m_realItem);
        }

        public void Redisplay()
        {
            

            List<IView> views = new List<IView>();
            foreach (IView view in View2DisplayItemMap.ViewsOfItem(m_realItem))
                views.Add(view);


            foreach (IView view in views)
            {
                view.Redisplay(m_realItem);
            }
        }

        static public void RedisplayAll()
        {
            View2DisplayItemMap.RedisplayAll();
        }

        IDisplayItem m_realItem = null;
    }
}
