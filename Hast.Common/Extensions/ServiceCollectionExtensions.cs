using Hast.Common.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection RemoveImplementations<T>(this IServiceCollection services) =>
            RemoveImplementations(services, typeof(T).FullName);

        public static IServiceCollection RemoveImplementations(this IServiceCollection services, string serviceFullName)
        {
            var servicesToRemove = services
                .Where(service => service.ServiceType?.FullName == serviceFullName)
                .ToList();

            foreach (var service in servicesToRemove)
            {
                services.Remove(service);
            }

            return services;
        }

        public static IServiceCollection RemoveImplementationsExcept<TService, TImplementation>(this IServiceCollection services) =>
            RemoveImplementationsExcept<TService>(services, typeof(TImplementation).FullName);

        public static IServiceCollection RemoveImplementationsExcept<TService>(
            this IServiceCollection services,
            string keepImplementationTypeFullName)
        {
            if (!services.Any(service => service.ImplementationType?.FullName == keepImplementationTypeFullName))
            {
                throw new InvalidOperationException("There is no service registered that matches " +
                    $"{keepImplementationTypeFullName}. This will make the service {typeof(TService).Name} unresolvable.");
            }

            var servicesToRemove = services
                .Where(service => service.ServiceType == typeof(TService) && service.ImplementationType.FullName != keepImplementationTypeFullName)
                .ToList();

            foreach (var service in servicesToRemove)
            {
                services.Remove(service);
            }

            return services;
        }

        /// <summary>
        /// Schedules a new log entry to be displayed when the service collection is build.
        /// </summary>
        public static IServiceCollection Log(
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
        public static IServiceCollection DeferredLogEntry(
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
}
