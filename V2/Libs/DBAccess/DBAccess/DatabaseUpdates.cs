using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBAccess
{
    public class DatabaseUpdates
    {
        public DatabaseUpdates(Type dbObjectBaseType,
            List<DBObjectBase> updatedInstances,
            List<UpdatedInstance> conflictingInstances,
            List<DBObjectBase> newInstances,
            List<DBObjectBase> deletedInstances)
        {
            m_updatedInstances = updatedInstances;
            m_conflictingInstances = conflictingInstances;
            m_newInstances=newInstances;
            m_deletedInstances = deletedInstances;
            m_dbObjectBaseType = dbObjectBaseType;
            m_affectedInstances = new List<DBObjectBase>();
        }


        public DatabaseUpdates(List<DBObjectBase> affectedInstances)
        {
            m_updatedInstances = new List<DBObjectBase>();
            m_conflictingInstances = new List<UpdatedInstance>();
            m_newInstances = new List<DBObjectBase>();;
            m_deletedInstances = new List<DBObjectBase>();
            m_dbObjectBaseType = null;
            m_affectedInstances = affectedInstances;
        }


        public Type ObjectBaseType { get { return m_dbObjectBaseType; } }
        public List<DBObjectBase> UpdatedInstances { get { return m_updatedInstances; } }
        public List<UpdatedInstance> ConflictingInstances { get { return m_conflictingInstances; } }
        public List<DBObjectBase> NewInstances { get { return m_newInstances; } }
        public List<DBObjectBase> DeletedInstances { get { return m_deletedInstances; } }
        public List<DBObjectBase> AffectedInstances { get { return m_affectedInstances; } }



        Type m_dbObjectBaseType;
        List<DBObjectBase> m_updatedInstances;
        List<UpdatedInstance> m_conflictingInstances;
        List<DBObjectBase> m_newInstances;
        List<DBObjectBase> m_deletedInstances;
        List<DBObjectBase> m_affectedInstances;

    }
}
