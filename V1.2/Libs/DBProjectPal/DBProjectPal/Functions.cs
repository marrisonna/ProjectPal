using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBProjectPal
{
    public class Functions
    {
        static public void ClearDateCaches()
        {
            Task.ClearDateCaches();
            Project.ClearDateCaches();

        }

        static public void LoadAllTypes()
        {
            object tmp;
            tmp = Task.AllInstances;
            tmp = Remark.AllInstances;
            tmp = Project.AllInstances;
            tmp = Person.AllInstances;
            tmp = Component.AllInstances;
            tmp = Attachment.AllInstances;
            tmp = Internal.LinkTable_Task2Resource.AllInstances;
            tmp = Internal.LinkTable_TimeDependency.AllInstances;

        }
       
    }
}
