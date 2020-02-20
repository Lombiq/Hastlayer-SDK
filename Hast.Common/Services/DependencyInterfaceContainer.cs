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

        public static void RegisterIDependencies(IServiceCollection services, IEnumerable<Assembly> assemblies)
        {
            var iDependencyType = typeof(IDependency);

            var assemblyList = assemblies is IList<Assembly> list ? list : assemblies?.ToList();
            if (assemblyList?.Any() != true) assemblyList = AppDomain.CurrentDomain.GetAssemblies();

            var types = assemblyList
                .Where(a => !a.IsDynamic)
                .SelectMany(a => a.GetExportedTypes())
                .Where(t => t.IsClass && !t.IsAbstract && iDependencyType.IsAssignableFrom(t))
                .Distinct()
                .ToList();

            foreach (var implementationType in types)
            {
                var serviceTypes = new List<Type>();
                var lifetime = ServiceLifetime.Scoped;
                foreach (var iface in implementationType.GetInterfaces())
                {
                    if (!iface.IsPublic || iface.Name == nameof(IDependency))
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
                }
            }
        }

        public static IServiceCollection AddIDependencyContainer(this IServiceCollection services, IEnumerable<string> paths, IEnumerable<Assembly> assemblies = null)
        {
            if (services == null) services = new ServiceCollection();
            if (paths != null) LoadAssemblies(paths);

            RegisterIDependencies(services, assemblies);
            services.AddTransient(typeof(Lazy<>), typeof(Lazier<>));
            services.AddLogging();
            services.AddMemoryCache();

            return services;
        }
    }
}
