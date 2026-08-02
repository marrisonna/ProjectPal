using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CustomGUIControls
{
    public class RedisplayManager
    {

        private RedisplayManager()
        {
            m_itemsAlreadyRefreshed = new Dictionary<object, HashSet<object>>();
        }

        public void Reset()
        {
            m_itemsAlreadyRefreshed.Clear();
            m_lastResetThing = null;
        }


        object m_lastResetThing = null;
        public void Reset(object thing)
        {
            if (m_lastResetThing == null)
            {
                m_lastResetThing = thing;
                m_itemsAlreadyRefreshed.Clear();
            }
        }

        public void ClearReset(object thing)
        {
            if (m_lastResetThing == thing)
            {
                m_lastResetThing = null;
            }
        }

        public bool HasItemAlreadyBeenRedisplayed(object item)
        {
            return HasItemAlreadyBeenRedisplayed(item, this);
        }

        public bool HasItemAlreadyBeenRedisplayed(object item1, object item2)
        {
            HashSet<object> hashSet;
            if (m_itemsAlreadyRefreshed.TryGetValue(item1, out hashSet))
            {
                if (hashSet.Contains(item2))
                    return true;

                hashSet.Add(item2);
                return false;
            }

            hashSet = new HashSet<object>();
            hashSet.Add(item2);
            m_itemsAlreadyRefreshed.Add(item1, hashSet);

            return false;
        }

        private Dictionary<object, HashSet<object>> m_itemsAlreadyRefreshed = null;

        public static RedisplayManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = new RedisplayManager();
                }
                return s_instance;
            }
        }

        private static RedisplayManager s_instance = null;
    }
}
