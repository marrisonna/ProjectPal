using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBProjectPal;

namespace ProjectPal
{
    class ProjectPalApplication
    {
        private ProjectPalApplication()
        {

        }
        static private ProjectPalApplication s_instance;

        static public ProjectPalApplication Instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = new ProjectPalApplication();
                return s_instance;

            }
        }


       

        public void SaveToDataBase()
        {
            DBAccess.DBObjectBase.Save();
        }


    }
}
