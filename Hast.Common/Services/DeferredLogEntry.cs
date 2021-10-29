using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Common.Services
{
    public class DeferredLogEntry : IDeferredLogEntry
    {
        public LogLevel Level { get; set; }
        public Exception Exception { get; set; }
        public string Message { get; set; }
        public IEnumerable<object> Arguments { get; set; }

        internal DeferredLogEntry() { }

        public void Log(ILogger logger)
        {
            var arguments = Arguments is string[] array ? array : Arguments.ToArray();
            if (Exception == null)
            {
                logger.Log(Level, Message, arguments);
            }
            else
            {
                logger.Log(Level, Exception, Message, arguments);
            }
        }
    }
}
