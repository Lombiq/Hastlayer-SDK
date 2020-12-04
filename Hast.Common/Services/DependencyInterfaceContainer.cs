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
        // This is necessary because .Net Core Dependency Injection does not resolve Lazy<T> out of the box.
        // https://stackoverflow.com/questions/44934511/does-net-core-dependency-injection-support-lazyt
        internal class Lazier<T> : Lazy<T> where T : class
        {
            public Lazier(IServiceProvider provider) : base(() => provider.GetRequiredService<T>()) { }
        }

        public static IEnumerable<Assembly> LoadAssemblies(IEnumerable<string> paths) =>
            paths.Select(x => Assembly.LoadFrom(Path.GetFullPath(x)));

        public static void RegisterIDependencies(IServiceCollection services, IEnumerable<Assembly> assemblies = null)
        {
            var iDependencyType = typeof(IDependency);

            if (assemblies is null) assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var types = assemblies
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

                    var initializerName = implementationType.GetCustomAttribute<IDependencyInitializerAttribute>()?.MemberName;
                    if (!string.IsNullOrEmpty(initializerName))
                    {
                        var method = implementationType.GetMethod(initializerName, BindingFlags.Public | BindingFlags.Static);
                        if (method is null) throw new ArgumentException($"The initializer method does not exist: '{implementationType.FullName}.{initializerName}'");
                        method.Invoke(null, new object[] { services });
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

        public static IServiceCollection AddIDependencyContainer(this IServiceCollection services, IEnumerable<Assembly> assemblies)
        {
            RegisterIDependencies(services, assemblies);
            return AddExternalHastlayerDependencies(services);
        }
    }
}
