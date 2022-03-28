using Hast.Common.Services;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Schedules a new log entry to be displayed when the service collection is build.
    /// </summary>
    public static IServiceCollection LogDeferred(
        this IServiceCollection services,
        LogLevel level,
        string message,
        params object[] arguments)
    {
        var entry = new DeferredLogEntry
        {
            Level = level,
            Message = message,
            Arguments = arguments,
        };

        return services.AddSingleton<IDeferredLogEntry>(entry);
    }

    /// <summary>
    /// Schedules a new log entry to be displayed when the service collection is build.
    /// </summary>
    public static IServiceCollection LogDeferred(
        this IServiceCollection services,
        Exception exception,
        string message,
        params object[] arguments)
    {
        var entry = new DeferredLogEntry
        {
            Level = exception.IsFatal() ? LogLevel.Critical : LogLevel.Error,
            Exception = exception,
            Message = message,
            Arguments = arguments,
        };

        return services.AddSingleton<IDeferredLogEntry>(entry);
    }
}
