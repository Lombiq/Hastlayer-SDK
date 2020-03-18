using Hast.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            public Lazier(IServiceProvider provider) : base(() => provider.GetRequiredService<T>()) { }
        }

        public static void LoadAssemblies(IEnumerable<string> paths)
        {
            var loadedPaths = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => !a.IsDynamic)
                .Select(a => a.Location)
                .ToList();

            foreach (var path in paths)
            {
                var fileInfo = new FileInfo(path);
                if (!loadedPaths.Contains(fileInfo.FullName)) Assembly.LoadFrom(fileInfo.FullName);
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
                .Distinct();

            foreach (var implementationType in types)
            {
                var serviceTypes = new List<Type>();
                var lifetime = ServiceLifetime.Scoped;
                foreach (var implementedInterfaces in implementationType.GetInterfaces())
                {
                    if (!implementedInterfaces.IsPublic || implementedInterfaces.Name == nameof(IDependency))
                    {
                        continue;
                    }
                    if (implementedInterfaces.Name == nameof(ISingletonDependency))
                    {
                        lifetime = ServiceLifetime.Singleton;
                    }
                    else if (implementedInterfaces.Name == nameof(ITransientDependency))
                    {
                        lifetime = ServiceLifetime.Transient;
                    }
                    else
                    {
                        serviceTypes.Add(implementedInterfaces);
                    }
                }

                foreach (var serviceType in serviceTypes)
                {
                    services.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
                }
            }
        }

        public static IServiceCollection AddExternalHastlayerDependencies(this IServiceCollection services)
        {
            services.AddScoped(typeof(Lazy<>), typeof(Lazier<>));
            services.AddLogging();
            services.AddSingleton(provider => provider.GetService<ILoggerFactory>().CreateLogger("Hastlayer"));
            services.AddMemoryCache();

            return services;
        }

        public static IServiceCollection AddIDependencyContainer(this IServiceCollection services, IEnumerable<string> paths, IEnumerable<Assembly> assemblies = null)
        {
            if (paths?.Any() == true) LoadAssemblies(paths);
            RegisterIDependencies(services, assemblies);

            return AddExternalHastlayerDependencies(services);
        }
    }
}
