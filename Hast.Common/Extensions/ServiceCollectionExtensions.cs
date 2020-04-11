using System;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection RemoveImplementations(this IServiceCollection services, string serviceName)
        {
            var servicesToRemove = services
                .Where(service => service.ServiceType?.Name == serviceName)
                .ToList();

            foreach (var service in servicesToRemove)
            {
                services.Remove(service);
            }

            return services;
        }

        public static IServiceCollection RemoveImplementations<T>(this IServiceCollection services) => RemoveImplementations(services, typeof(T).Name);

        public static IServiceCollection RemoveImplementationsExcept<TService, Timplementation>(this IServiceCollection services) =>
            RemoveImplementationsExcept<TService>(services, typeof(Timplementation).Name);
        public static IServiceCollection RemoveImplementationsExcept<TService>(this IServiceCollection services, string keepImplementationTypeName)
        {
            if (!services.Any(service => service.ImplementationType?.Name == keepImplementationTypeName))
            {
                throw new InvalidOperationException("There is no service registered that matches keepImplementationTypeName. " +
                    $"({keepImplementationTypeName}) This will make the service '{typeof(TService).Name}' unresolvable.");
            }

            var servicesToRemove = services
                .Where(service => service.ServiceType == typeof(TService) && service.ImplementationType.Name != keepImplementationTypeName)
                .ToList();

            foreach (var service in servicesToRemove)
            {
                services.Remove(service);
            }

            return services;
        }
    }
}
