using Hast.Catapult.Abstractions;
using Hast.Common.Services;
using Hast.Common.Validation;
using Hast.Communication;
using Hast.Communication.Extensibility;
using Hast.Communication.Extensibility.Events;
using Hast.Layer.EmptyRepresentationFactories;
using Hast.Layer.Extensibility.Events;
using Hast.Layer.Models;
using Hast.Synthesis.Abstractions;
using Hast.Transformer.Abstractions;
using Hast.Transformer.Abstractions.SimpleMemory;
using Hast.Xilinx.Abstractions.ManifestProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NLog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Hast.Layer;

public sealed class Hastlayer : IHastlayer
{
    public const string AppsettingsJsonFileName = "appsettings.json";

    private readonly ServiceProvider _serviceProvider;
    private readonly HashSet<string> _serviceNames;

    public event EventHandler<ServiceEventArgs<IMemberHardwareExecutionContext>> ExecutedOnHardware;

    public event EventHandler<ServiceEventArgs<IMemberInvocationContext>> Invoking;

    // Private so the static factory should be used.
    private Hastlayer(IHastlayerConfiguration configuration)
    {
        var appDataFolder = new AppDataFolder(configuration.AppDataFolderPath);

        // Since the DI prefers services in order of registration, we take the user assemblies first followed by dynamic
        // lookup of Hast.*.dll files.
        var assemblies = new List<Assembly>(configuration.Extensions);
        assemblies.AddRange(new[]
        {
            typeof(Hastlayer).Assembly,
            typeof(IProxyGenerator).Assembly,
            typeof(IHardwareImplementationComposer).Assembly,
            typeof(ITransformer).Assembly,
            typeof(NexysA7ManifestProvider).Assembly,
            typeof(CatapultManifestProvider).Assembly,
        });
        assemblies.AddRange(DependencyInterfaceContainer.LoadAssemblies(Directory.GetFiles(".", "Hast.*.dll")));

        var services = new ServiceCollection();

        // Only a reference is saved.
#pragma warning disable S3366 // "this" should not be exposed from constructors
        services.AddSingleton<IHastlayer>(this);

#pragma warning restore S3366 // "this" should not be exposed from constructors
        services.AddSingleton(configuration);
        services.AddSingleton<IAppDataFolder>(appDataFolder);
        services.AddSingleton(BuildConfiguration());
        services.AddScoped<IHardwareGenerationConfigurationAccessor, HardwareGenerationConfigurationAccessor>();
        services.AddIDependencyContainer(assemblies);

        ConfigureLogging(services, configuration.ConfigureLogging);

        configuration.OnServiceRegistration?.Invoke(configuration, services);

        // To test that deferred logging works:
        //// services.Log(LogLevel.Critical, "Critical message!");
        //// services.Log(LogLevel.Error, "Error message!");
        //// services.Log(LogLevel.Warning, "Warning message!");
        //// services.Log(LogLevel.Critical, "Critical message {0} {1} {2}!", "with", 3, "parameters");

        _serviceNames = new HashSet<string>(services.Select(serviceDescriptor => serviceDescriptor.ServiceType.FullName));
        _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var logger = GetLogger<IDeferredLogEntry>();
        foreach (var logEntry in _serviceProvider.GetRequiredService<IEnumerable<IDeferredLogEntry>>())
        {
            logEntry.Log(logger);
        }
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Will be disposed by the DI scope.")]
    public static void ConfigureLogging(IServiceCollection services, Action<ILoggingBuilder> configureAction = null)
    {
        services.AddSingleton(LoggerFactory.Create(builder =>
        {
            if (configureAction is null)
            {
                builder.AddNLog("NLog.config");
            }
            else
            {
                configureAction(builder);
            }
        }));

        services.AddSingleton(provider => provider.GetService<ILoggerFactory>().CreateLogger("hastlayer"));
    }

    public static Hastlayer Create() => Create(HastlayerConfiguration.Default);

    /// <summary>
    /// Instantiates a new <see cref="Hastlayer"/> instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Point of this factory is that it can throw exceptions.
    /// </para>
    /// </remarks>
    /// <param name="configuration">Configuration for Hastlayer.</param>
    /// <returns>A newly created <see cref="Hastlayer"/> instance.</returns>
    public static Hastlayer Create(IHastlayerConfiguration configuration)
    {
        Argument.ThrowIfNull(configuration, nameof(configuration));
        Argument.ThrowIfNull(configuration.Extensions, nameof(configuration.Extensions));

        var hastlayer = new Hastlayer(configuration);
        hastlayer.LoadHost();
        return hastlayer;
    }

    public static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddJsonFile(AppsettingsJsonFileName, optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .AddUserSecrets(Assembly.GetEntryAssembly(), optional: true)
            .AddCommandLine(Environment.GetCommandLineArgs())
            .Build();

    public void Dispose() => _serviceProvider?.Dispose();

    public Task<IHardwareRepresentation> GenerateHardwareAsync(
        IEnumerable<string> assemblyPaths,
        IHardwareGenerationConfiguration configuration)
    {
        // Avoid repeated multiple enumerations.
        var assembliesPaths = assemblyPaths.AsList();

        Argument.ThrowIfNull(assembliesPaths, nameof(assembliesPaths));
        if (!assembliesPaths.Any())
        {
            throw new ArgumentException("No assemblies were specified.");
        }

        if (assembliesPaths.Count != assembliesPaths.Distinct().Count())
        {
            throw new ArgumentException(
                "The same assembly was included multiple times. Only supply each assembly to generate hardware from once.");
        }

        return GenerateHardwareInnerAsync(assembliesPaths, configuration);
    }

    public async Task<IHardwareRepresentation> GenerateHardwareInnerAsync(
        IList<string> assemblyPaths,
        IHardwareGenerationConfiguration configuration)
    {
        try
        {
            // This is fine because IHardwareRepresentation doesn't contain anything that relies on the scope.
            using var scope = _serviceProvider.CreateScope();
            var provider = scope.ServiceProvider;
            provider.GetRequiredService<IHardwareGenerationConfigurationAccessor>()
                .Value = configuration;

            var transformer = provider.GetRequiredService<ITransformer>();
            var deviceManifestSelector = provider.GetRequiredService<IDeviceManifestSelector>();
            var loggerService = provider.GetRequiredService<ILogger<Hastlayer>>();
            var appConfiguration = provider.GetRequiredService<IConfiguration>();
            var transformationEvents = provider.GetRequiredService<IEnumerable<ITransformationEvents>>().ToList();

            var deviceManifest = deviceManifestSelector
                .GetSupportedDevices()
                .FirstOrDefault(manifest => manifest.Name == configuration.DeviceName);

            if (deviceManifest == null)
            {
                throw new HastlayerException(
                    "There is no supported device with the name \"" + configuration.DeviceName + "\".");
            }

            if (File.Exists(configuration.SingleBinaryPath))
            {
                return new HardwareRepresentation
                {
                    DeviceManifest = deviceManifest,
                    HardwareDescription = EmptyHardwareDescriptionFactory.Create(configuration),
                    HardwareImplementation = new HardwareImplementation
                    {
                        BinaryPath = configuration.SingleBinaryPath,
                    },
                    HardwareGenerationConfiguration = configuration,
                    SoftAssemblyPaths = assemblyPaths,
                };
            }

            // Load any not-yet-populated configuration with appsettings > HardwareGenerationConfiguration >
            // CustomConfiguration into the current hardware generation configuration.
            var newCustomConfigurations = appConfiguration
                .GetSection(nameof(HardwareGenerationConfiguration))
                .GetSection(nameof(HardwareGenerationConfiguration.CustomConfiguration))
                .GetChildren()
                .Where(x => !configuration.CustomConfiguration.ContainsKey(x.Key));
            foreach (var item in newCustomConfigurations)
            {
                configuration.CustomConfiguration[item.Key] =
                    JObject.Parse(item.Serialize())[nameof(HardwareGenerationConfiguration)]?
                    [nameof(HardwareGenerationConfiguration.CustomConfiguration)]?
                    [item.Key];
            }

            foreach (var transformationEvent in transformationEvents)
            {
                await transformationEvent.BeforeTransformAsync();
            }

            var hardwareDescription = configuration.EnableHardwareTransformation ?
                await transformer.TransformAsync(assemblyPaths, configuration) :
                EmptyHardwareDescriptionFactory.Create(configuration);

            foreach (var transformationEvent in transformationEvents)
            {
                await transformationEvent.AfterTransformAsync(hardwareDescription);
            }

            foreach (var warning in hardwareDescription.Warnings)
            {
                loggerService.LogWarning(
                    "Hastlayer transformation warning (code: {Code}): {Message}",
                    warning.Code,
                    warning.Message);
            }

            var hardwareImplementationComposerSelector =
                provider.GetRequiredService<IHardwareImplementationComposerSelector>();

            IHardwareImplementation hardwareImplementation;
            if (configuration.EnableHardwareImplementationComposition && configuration.EnableHardwareTransformation)
            {
                var hardwareImplementationCompositionContext = new HardwareImplementationCompositionContext
                {
                    Configuration = configuration,
                    HardwareDescription = hardwareDescription,
                    DeviceManifest = deviceManifest,
                };

                var hardwareImplementationComposer = hardwareImplementationComposerSelector
                                                         .GetHardwareImplementationComposer(hardwareImplementationCompositionContext) ??
                                                     throw new HastlayerException("No suitable hardware implementation composer was found.");

                hardwareImplementation = await hardwareImplementationComposer
                    .ComposeAsync(hardwareImplementationCompositionContext);
            }
            else
            {
                hardwareImplementation = EmptyHardwareImplementationFactory.Create();
            }

            return new HardwareRepresentation
            {
                SoftAssemblyPaths = assemblyPaths,
                HardwareDescription = hardwareDescription,
                HardwareImplementation = hardwareImplementation,
                DeviceManifest = deviceManifest,
                HardwareGenerationConfiguration = configuration,
            };
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
            var message =
                "An error happened during generating the Hastlayer hardware representation for the following assemblies: " +
                string.Join(", ", assemblyPaths);
            LogException(ex, message);
            throw new HastlayerException(message, ex);
        }
    }

    public async Task<T> GenerateProxyAsync<T>(
        IHardwareRepresentation hardwareRepresentation,
        T hardwareObject,
        IProxyGenerationConfiguration configuration = null)
        where T : class
    {
        configuration ??= ProxyGenerationConfiguration.Default;
        if (!hardwareRepresentation.SoftAssemblyPaths.Contains(hardwareObject.GetType().Assembly.Location))
        {
            throw new InvalidOperationException(
                "The supplied type is not part of any assembly that this hardware representation was generated from.");
        }

        try
        {
            return await Task.Run(() => _serviceProvider
                .GetService<IProxyGenerator>()
                .CreateCommunicationProxy(hardwareRepresentation, hardwareObject, configuration));
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
            var message =
                "An error happened during generating the Hastlayer proxy for an object of the following type: " +
                hardwareObject.GetType().FullName;
            LogException(ex, message);
            throw new HastlayerException(message, ex);
        }
    }

    public DisposableContainer<TServiceInterface> GetService<TServiceInterface>()
    {
        IServiceScope scope = null;
        try
        {
            // If the try block successfully returns then the scope's resources are managed by the returned
            // DisposableContainer so scope must NOT be disposed here.
#pragma warning disable CA2000 // Dispose objects before losing scope
            scope = _serviceProvider.CreateScope();
#pragma warning restore CA2000 // Dispose objects before losing scope

            var service = scope.ServiceProvider.GetService<TServiceInterface>();
            return new DisposableContainer<TServiceInterface>(scope, service);
        }
        catch
        {
            scope?.Dispose();
            throw;
        }
    }

    public DisposableContainer<TServiceInterface1, TServiceInterface2> GetServices<TServiceInterface1, TServiceInterface2>()
    {
        IServiceScope scope = null;
        try
        {
            // See comment in GetService.
#pragma warning disable CA2000 // Dispose objects before losing scope
            scope = _serviceProvider.CreateScope();
#pragma warning restore CA2000 // Dispose objects before losing scope

            var service1 = scope.ServiceProvider.GetService<TServiceInterface1>();
            var service2 = scope.ServiceProvider.GetService<TServiceInterface2>();
            return new DisposableContainer<TServiceInterface1, TServiceInterface2>(scope, service1, service2);
        }
        catch
        {
            scope?.Dispose();
            throw;
        }
    }

    public SimpleMemory CreateMemory(IHardwareGenerationConfiguration configuration, int cellCount) =>
        RunGet(provider => SimpleMemory.Create(
            MemoryConfiguration.Create(configuration, provider.GetService<IEnumerable<IDeviceManifestProvider>>()),
            cellCount));

    public SimpleMemory CreateMemory(IHardwareGenerationConfiguration configuration, Memory<byte> data, int withPrefixCells = 0) =>
        RunGet(provider => SimpleMemory.Create(
            MemoryConfiguration.Create(configuration, provider.GetService<IEnumerable<IDeviceManifestProvider>>()),
            data,
            provider.GetService<ILogger>(),
            withPrefixCells));

    public IMemoryConfiguration CreateMemoryConfiguration(IHardwareRepresentation hardwareRepresentation) =>
        RunGet(provider => MemoryConfiguration.Create(
                hardwareRepresentation.HardwareGenerationConfiguration,
                provider.GetService<IEnumerable<IDeviceManifestProvider>>())
        );

    public IEnumerable<IDeviceManifest> GetSupportedDevices()
    {
        // This is fine because IDeviceManifest doesn't contain anything that relies on the scope.
        using var scope = _serviceProvider.CreateScope();
        return scope.ServiceProvider.GetService<IDeviceManifestSelector>().GetSupportedDevices();
    }

    public async Task RunAsync<T>(Func<T, Task> process)
    {
        using var scope = _serviceProvider.CreateScope();
        await process(scope.ServiceProvider.GetRequiredService<T>());
    }

    public async Task<TOut> RunGetAsync<TOut>(Func<IServiceProvider, Task<TOut>> process)
    {
        ThrowIfService<TOut>();
        using var scope = _serviceProvider.CreateScope();
        return await process(scope.ServiceProvider);
    }

    public TOut RunGet<TOut>(Func<IServiceProvider, TOut> process)
    {
        ThrowIfService<TOut>();
        using var scope = _serviceProvider.CreateScope();
        return process(scope.ServiceProvider);
    }

    public ILogger<T> GetLogger<T>() => _serviceProvider.GetService<ILogger<T>>();

    private void LoadHost()
    {
        var factory = _serviceProvider.GetService<IMemberInvocationHandlerFactory>();
        factory.MemberExecutedOnHardware += (_, context) =>
            ExecutedOnHardware?.Invoke(this, new ServiceEventArgs<IMemberHardwareExecutionContext>(context.Arguments));
        factory.MemberInvoking += (_, context) =>
            Invoking?.Invoke(this, new ServiceEventArgs<IMemberInvocationContext>(context.Arguments));
    }

    // This method is specifically to proxy log messages.
#pragma warning disable CA2254 // Template should be a static expression
    private void LogException(Exception exception, string message) =>
        _serviceProvider.GetService<ILogger<Hastlayer>>().LogError(exception, message);
#pragma warning restore CA2254 // Template should be a static expression

    private void ThrowIfService<TOut>()
    {
        if (_serviceNames.Contains(typeof(TOut).FullName))
        {
            throw new InvalidOperationException($"The return type (used: {typeof(TOut).FullName}) must not be a registered service.");
        }
    }
}
