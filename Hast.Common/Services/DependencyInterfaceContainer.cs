using Hast.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Hast.Common.Services
{
    public static class DependencyInterfaceContainer
    {
        internal class Lazier<T> : Lazy<T> where T : class
        {
            public Lazier(IServiceProvider provider)
                : base(() => provider.GetRequiredService<T>())
            {
            }
        }

        public static void LoadAssemblies(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                Assembly.LoadFile(Path.IsPathRooted(path) ? path : Path.Combine(Environment.CurrentDirectory, path));
            }
        }

        public static void RegisterIDependencies(IServiceCollection services)
        {
            var iDependencyType = typeof(IDependency);

            var types = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetLoadedModules())
                .SelectMany(m => { try { return m.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .Where(t => !t.IsInterface && !t.IsAbstract && iDependencyType.IsAssignableFrom(t))
                .Distinct()
                .ToList();

            var typesNames = new Dictionary<string, List<string>>();
            var xxx = types.Select(x => x.Name).OrderBy(x => x).ToList();
            foreach (var implementationType in types)
            {
                var className = implementationType.Name;
                var serviceTypes = new List<Type>();
                var lifetime = ServiceLifetime.Scoped;
                foreach (var iface in implementationType.GetInterfaces())
                {
                    if (iface.Name == nameof(IDependency))
                    {
                        continue;
                    }
                    if (iface.Name == nameof(ISingletonDependency))
                    {
                        lifetime = ServiceLifetime.Singleton;
                    }
                    else if (iface.Name == nameof(ITransientDependency))
                    {
                        lifetime = ServiceLifetime.Transient;
                    }
                    else
                    {
                        serviceTypes.Add(iface);
                    }
                }

                foreach (var serviceType in serviceTypes)
                {
                    services.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
                    if (!typesNames.ContainsKey(serviceType.Name)) typesNames[serviceType.Name] = new List<string>();
                    typesNames[serviceType.Name].Add(implementationType.Name);
                }
            }
        }

        public static IServiceCollection AddIDependencyContainer(this IServiceCollection services, IEnumerable<string> paths)
        {
            if (services == null) services = new ServiceCollection();
            if (paths != null) LoadAssemblies(paths);

            RegisterIDependencies(services);
            services.AddTransient(typeof(Lazy<>), typeof(Lazier<>));
            services.AddLogging();
            services.AddMemoryCache();

            return services;
        }
    }
}
