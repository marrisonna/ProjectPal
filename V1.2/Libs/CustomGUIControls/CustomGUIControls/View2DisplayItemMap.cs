using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CustomGUIControls
{
    internal class View2DisplayItemMap
    {

        static public HashSet<IDisplayItem> ItemsOnView(IView theView)
        {
            HashSet<IDisplayItem> items = null;
            if (m_view2DisplayItems.TryGetValue(theView, out items))
                return items;
            return new HashSet<IDisplayItem>();
        }


        static public HashSet<IView> ViewsOfItem(IDisplayItem item)
        {
            HashSet<IView> views = null;
            if (m_displayItem2View.TryGetValue(item, out views))
                return views;
            return new HashSet<IView>();
        }

        static public void WindowClosed(IView theView)
        {
            HashSet<IDisplayItem> items = null;
            if (m_view2DisplayItems.TryGetValue(theView, out items))
            {
                foreach (IDisplayItem item in items)
                {
                    HashSet<IView> views = null;
                    if (m_displayItem2View.TryGetValue(item, out views))
                    {
                        views.Remove(theView);
                        if (views.Count == 0)
                            m_displayItem2View.Remove(item);
                    }
                }
                m_view2DisplayItems.Remove(theView);
            }
        }

        static public void DisplayItemDeleted(IDisplayItem item)
        {
            HashSet<IView> views = null;
            if (m_displayItem2View.TryGetValue(item, out views))
            {
            Redo:
                int origViewCount = views.Count;
                foreach (IView view in views)
                {
                    view.RemoveDisplayItem(item);

                    HashSet<IDisplayItem> displayItems = null;
                    if (m_view2DisplayItems.TryGetValue(view, out displayItems) && displayItems.Count == 0)
                        m_view2DisplayItems.Remove(view);
                    if (origViewCount != views.Count)
                        goto Redo;
                }
                m_displayItem2View.Remove(item);
            }
        }

        static public void Add(IView theView, IDisplayItem item)
        {
            HashSet<IDisplayItem> displayItemCollection;
            if (!m_view2DisplayItems.TryGetValue(theView,out displayItemCollection))
                m_view2DisplayItems.Add(theView, displayItemCollection = new HashSet<IDisplayItem>());

            //if (!m_view2DisplayItems[theView].Contains(item))
            displayItemCollection.Add(item);

            HashSet<IView> displayViewCollection;
            if (!m_displayItem2View.TryGetValue(item, out displayViewCollection))
                m_displayItem2View.Add(item, displayViewCollection=new HashSet<IView>());

            //if (!m_displayItem2View[item].Contains(theView))
            displayViewCollection.Add(theView);
        }

        static public void Remove(IView theView, IDisplayItem item)
        {
            if (m_view2DisplayItems.ContainsKey(theView))
            {
                m_view2DisplayItems[theView].Remove(item);
                if (m_view2DisplayItems[theView].Count == 0)
                    m_view2DisplayItems.Remove(theView);
            }


            if (m_displayItem2View.ContainsKey(item))
            {
                m_displayItem2View[item].Remove(theView);
                if (m_displayItem2View[item].Count == 0)
                    m_displayItem2View.Remove(item);
            }
        }


        static public void RedisplayAll()
        {
            // Take a copy of 'Keys' incase something changes.
            List<IDisplayItem> itemsToRefresh = new List<IDisplayItem>(m_displayItem2View.Keys);
            foreach (IDisplayItem displayItem in itemsToRefresh)
            {
                displayItem.Redisplay();
            }
        }


        static private Dictionary<IView, HashSet<IDisplayItem>> m_view2DisplayItems = new Dictionary<IView, HashSet<IDisplayItem>>();
        static private Dictionary<IDisplayItem, HashSet<IView>> m_displayItem2View = new Dictionary<IDisplayItem, HashSet<IView>>();
    }




}
