using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBAccess
{
    public interface IInternalDBClass
    {
        List<DBObjectBase> AffectInstances { get; }
        void ClearCaches();
    }
}
