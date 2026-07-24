using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
namespace Utilities.Helpers
{
    static public class CommonHelper
    {
        static public string ApplicationName
        {
            get
            {
                string fullName = String.Empty;
                Assembly assembly = Assembly.GetEntryAssembly();
                if(assembly != null)
                {
                    AssemblyName assemblyName = assembly.GetName(false);
                    if(assemblyName != null)
                    {
                        fullName = assemblyName.Name;
                        if ((false == String.IsNullOrEmpty(fullName) && (fullName.StartsWith("Nomura.FixedIncome."))))
                        {
                            fullName = fullName.Substring(19);
                        }
                    }
                }
                return fullName;
            }
        }

        static public string ApplicationPath
        {
            get
            {
                Assembly assembly = Assembly.GetEntryAssembly();
                string fullPath = assembly.Location;
                int i = fullPath.Length - 1;
                for (; i >= 0 && fullPath[i] != '\\'; i--) ;
                return fullPath.Substring(0, i + 1);
            }

        }
    }
}
