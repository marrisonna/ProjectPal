using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities.Helpers
{
    public class EnvironmentHelper
    {
        private static string m_userName;
        private static string m_DomainName;
        private static string m_ShortUserName;

        static EnvironmentHelper()
        {
            m_userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            string[] userNameParts = m_userName.Split('\\');
            m_DomainName = userNameParts[0];
            m_ShortUserName = userNameParts[1];
        }

        public static string CompleteWindowsUserName
        {
            get
            {
                return m_userName;
            }
        }

        public static string ShortWindowsUserName
        {
            get
            {
                return m_ShortUserName;            
            }
        }

        public static string UserDomain
        {
            get
            {
                return m_DomainName;
            }
        }
    }
}