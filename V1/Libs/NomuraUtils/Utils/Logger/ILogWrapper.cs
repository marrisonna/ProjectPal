using System;
using System.Collections.Generic;
using System.Text;
using log4net;

namespace Utilities.Logging
{
    public interface ILogWrapper : ILog
    {
        bool IsTraceEnabled { get; }

        void Trace(object message);
        void Trace(object message, Exception t);

        void SetLevel(LogLevel level);
        
    }
}
