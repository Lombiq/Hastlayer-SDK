using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Common.Services;

public class DeferredLogEntry : IDeferredLogEntry
{
    public LogLevel Level { get; set; }
    public Exception Exception { get; set; }
    public string Message { get; set; }
    public IEnumerable<object> Arguments { get; set; }

    internal DeferredLogEntry() { }

    public void Log(ILogger logger)
    {
        // This is not relevant for strings.
#pragma warning disable S2330 // Array covariance should not be used
        var arguments = Arguments is string[] array ? array : Arguments.ToArray();
#pragma warning restore S2330 // Array covariance should not be used

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
