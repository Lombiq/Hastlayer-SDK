﻿using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection RemoveImplementations(this IServiceCollection services, string serviceName)
        {
            var servicesToRemove = services
                .Where(service => service.ServiceType.Name == serviceName)
                .ToList();

            foreach (var service in servicesToRemove)
            {
                services.Remove(service);
            }

            return services;
        }

        public static IServiceCollection RemoveImplementations<T>(this IServiceCollection services) => RemoveImplementations(services, typeof(T).Name);
    }
}
