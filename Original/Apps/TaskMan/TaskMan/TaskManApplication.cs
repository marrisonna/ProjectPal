using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBTaskMan;

namespace TaskMan
{
    class TaskManApplication
    {
        private TaskManApplication()
        {

        }
        static private TaskManApplication s_instance;

        static public TaskManApplication Instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = new TaskManApplication();
                return s_instance;

            }
        }


       

        public void SaveToDataBase()
        {
            DBAccess.DBObjectBase.Save();
        }


    }
}
