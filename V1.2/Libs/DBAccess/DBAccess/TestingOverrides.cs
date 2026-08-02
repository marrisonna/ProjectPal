using System;
using System.Collections.Generic;
using System.Text;

namespace DBAccess
{
    public class TestingOverrides
    {
        public enum TestUserLevel { SuperUser=3, PowerUser=2, NormalUser=1, ReadOnlyUser=0 }


        //static private string s_dbName = @"ASIAPAC\parnaik";
        //static private string s_dbName = @"EUROPE\dsouzaru";
        //static private TestUserLevel? s_userLevel = TestUserLevel.NormalUser;

        static private string s_dbName = null;
        static private TestUserLevel? s_userLevel = null;

        static public string DBName
        {
            get
            {
                return s_dbName;

            }
        }

        static public TestUserLevel? UserLevel
        {
            get
            {
                return s_userLevel;
            }
        }

        static public void SetSuperUser()
        {
            s_dbName = @"EUROPE\marrison";
            s_userLevel = TestUserLevel.SuperUser;
        }

        static public void SetUser(string dbUserName)
        {
            s_dbName = dbUserName;
        }

        static public void SetUserLevel(TestingOverrides.TestUserLevel userLevel)
        {
            s_userLevel = userLevel;
        }
    }
}
