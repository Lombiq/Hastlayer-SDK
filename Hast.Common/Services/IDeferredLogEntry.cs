using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Hast.Common.Services
{
    /// <summary>
    /// A log entry to be registered during service collection creation and to be logged after the service provider is
    /// built.
    /// </summary>
    public interface IDeferredLogEntry
    {
        /// <summary>
        /// Gets the severity or the log event.
        /// </summary>
        LogLevel Level { get; }

        /// <summary>
        /// Gets the Exception associated with the log event if there is any.
        /// </summary>
        Exception Exception { get; }

        /// <summary>
        /// Gets the Message to be written.
        /// </summary>
        string Message { get; }

        /// <summary>
        /// Gets the substitutions to be passed into <see cref="Message"/>.
        /// </summary>
        IEnumerable<object> Arguments { get; }

        /// <summary>
        /// Logs the event into the <paramref name="logger"/>.
        /// </summary>
        void Log(ILogger logger);
    }
}
