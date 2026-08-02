using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBAccess
{
    public class UpdatedInstance
    {

        internal UpdatedInstance(DBObjectBase memoryInstance, DBObjectBase databaseInstance)
        {
            m_memoryInstance = memoryInstance;
            m_databaseInstance = databaseInstance;
        }

        public DBObjectBase MemoryInstance { get { return m_memoryInstance; } }
        public DBObjectBase DatabaseInstance { get { return m_databaseInstance; } }

        DBObjectBase m_memoryInstance;
        DBObjectBase m_databaseInstance;


    }
}
