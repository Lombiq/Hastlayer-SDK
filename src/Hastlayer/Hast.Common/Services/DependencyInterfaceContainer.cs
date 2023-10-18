using Hast.Common.Interfaces;
using Lombiq.HelpfulLibraries.Common.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Hast.Common.Services;

public static class DependencyInterfaceContainer
{
    [SuppressMessage(
        "Major Code Smell",
        "S3885:\"Assembly.Load\" should be used",
        Justification = "Necessary to prevent duplicate type declarations.")]
    public static IEnumerable<Assembly> LoadAssemblies(IEnumerable<string> paths) =>
        paths.Select(path => Assembly.LoadFrom(Path.GetFullPath(path)));

    public static void RegisterIDependencies(IServiceCollection services, IEnumerable<Assembly> assemblies = null)
    {
        var iDependencyType = typeof(IDependency);

        assemblies ??= AppDomain.CurrentDomain.GetAssemblies();
        var types = assemblies
            .Where(assembly => !assembly.IsDynamic)
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.IsClass && !type.IsAbstract && iDependencyType.IsAssignableFrom(type))
            .Distinct();

        foreach (var implementationType in types)
        {
            var serviceTypes = new List<Type>();
            var lifetime = ServiceLifetime.Scoped;
            foreach (var implementedInterfaces in implementationType.GetInterfaces())
            {
                lifetime = RegisterImplementation(services, implementationType, implementedInterfaces, serviceTypes, lifetime);
            }

            foreach (var serviceType in serviceTypes)
            {
                services.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
            }
        }
    }

    private static ServiceLifetime RegisterImplementation(
        IServiceCollection services,
        Type implementationType,
        Type implementedInterfaces,
        List<Type> serviceTypes,
        ServiceLifetime lifetime)
    {
        if (!implementedInterfaces.IsPublic || implementedInterfaces.Name == nameof(IDependency))
        {
            return lifetime;
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

        var initializerName = implementationType.GetCustomAttribute<DependencyInitializerAttribute>()?.MemberName;
        if (string.IsNullOrEmpty(initializerName)) return lifetime;

        var method = implementationType.GetMethod(initializerName, BindingFlags.Public | BindingFlags.Static) ??
            throw new ArgumentException($"The initializer method does not exist: '{implementationType.FullName}.{initializerName}'");
        method.Invoke(null, new object[] { services });

        return lifetime;
    }

    public static IServiceCollection AddExternalHastlayerDependencies(this IServiceCollection services)
    {
        services.AddLazyInjectionSupport();
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
