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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Hast.Layer
{
    public sealed class Hastlayer : IHastlayer
    {
        public const string AppsettingsJsonFileName = "appsettings.json";

        private readonly IHastlayerConfiguration _configuration;
        private readonly ServiceProvider _serviceProvider;
        private readonly HashSet<string> _serviceNames;

        public event EventHandler<ServiceEventArgs<IMemberHardwareExecutionContext>> ExecutedOnHardware;
        public event EventHandler<ServiceEventArgs<IMemberInvocationContext>> Invoking;

        // Private so the static factory should be used.
        private Hastlayer(IHastlayerConfiguration configuration)
        {
            _configuration = configuration;
            var appDataFolder = new AppDataFolder(configuration.AppDataFolderPath);

            // Since the DI prefers services in order of registration, we take the user assemblies first followed by
            // dynamic lookup of Hast.*.dll files.
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
            assemblies.AddRange(GetHastLibraries());

            var services = new ServiceCollection();
            services.AddSingleton<IHastlayer>(this);
            services.AddSingleton(configuration);
            services.AddSingleton<IAppDataFolder>(appDataFolder);
            services.AddSingleton(BuildConfiguration());
            services.AddScoped<IHardwareGenerationConfigurationAccessor, HardwareGenerationConfigurationAccessor>();
            services.AddIDependencyContainer(assemblies);

            ConfigureLogging(services, configuration.ConfigureLogging);

            configuration.OnServiceRegistration?.Invoke(configuration, services);

            var transformerServices = services.Where(x => x.ServiceType == typeof(ITransformer)).ToList();
            if (transformerServices.Count > 1)
            {
                switch (configuration.Flavor)
                {
                    case HastlayerFlavor.Client:
                        services.RemoveImplementationsExcept<ITransformer, Remote.Client.RemoteTransformer>();
                        break;
                    case HastlayerFlavor.Developer:
                        // Can't use the type directly because it won't be available in the Client flavor.
                        services.RemoveImplementationsExcept<ITransformer>("Hast.Transformer.DefaultTransformer");
                        break;
                    default:
                        throw new ArgumentException($"Unknown flavor in configuration: '{configuration.Flavor}'");
                }
            }

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

        public static void ConfigureLogging(IServiceCollection services, Action<ILoggingBuilder> configureLogging = null)
        {
            services.AddSingleton(LoggerFactory.Create(builder =>
            {
                if (configureLogging is null)
                {
                    builder.AddNLog("NLog.config");
                }
                else
                {
                    configureLogging(builder);
                }
            }));

            services.AddSingleton(provider => provider.GetService<ILoggerFactory>().CreateLogger("hastlayer"));
        }

        public static IHastlayer Create() => Create(HastlayerConfiguration.Default);

        /// <summary>
        /// Instantiates a new <see cref="IHastlayer"/> implementation.
        /// </summary>
        /// <remarks>
        /// Point of this factory is that it returns an interface type instead of the implementation and can throw
        /// exceptions.
        /// </remarks>
        /// <param name="configuration">Configuration for Hastlayer.</param>
        /// <returns>A newly created <see cref="IHastlayer"/> implementation.</returns>
        public static IHastlayer Create(IHastlayerConfiguration configuration)
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

        ~Hastlayer() => Dispose();

        public async Task<IHardwareRepresentation> GenerateHardwareAsync(
            IEnumerable<string> assemblyPaths,
            IHardwareGenerationConfiguration configuration)
        {
            // Avoid repeated multiple enumerations.
            var assembliesPaths = assemblyPaths.ToList();

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

                // Load any not-yet-populated configuration with appsettings > HardwareGenerationConfiguration >
                // CustomConfiguration into the current hardware generation configuration.
                var newCustomConfigurations = appConfiguration
                    .GetSection(nameof(HardwareGenerationConfiguration))
                    .GetSection(nameof(HardwareGenerationConfiguration.CustomConfiguration))
                    .GetChildren()
                    .Where(x => !configuration.CustomConfiguration.ContainsKey(x.Key));
                foreach (var item in newCustomConfigurations)
                {
                    configuration.CustomConfiguration[item.Key] = JObject.Parse(item.Serialize())
                        [nameof(HardwareGenerationConfiguration)]?
                        [nameof(HardwareGenerationConfiguration.CustomConfiguration)]?
                        [item.Key];
                }

                foreach (var transformationEvent in transformationEvents)
                {
                    await transformationEvent.BeforeTransformAsync();
                }

                var hardwareDescription = configuration.EnableHardwareTransformation ?
                    await transformer.TransformAsync(assembliesPaths, configuration) :
                    EmptyHardwareDescriptionFactory.Create(configuration);

                foreach (var transformationEvent in transformationEvents)
                {
                    await transformationEvent.AfterTransformAsync(hardwareDescription);
                }

                foreach (var warning in hardwareDescription.Warnings)
                {
                    loggerService.LogWarning(
                        "Hastlayer transformation warning (code: {0}): {1}",
                        warning.Code,
                        warning.Message);
                }

                var deviceManifest = deviceManifestSelector
                    .GetSupportedDevices()
                    .FirstOrDefault(manifest => manifest.Name == configuration.DeviceName);

                if (deviceManifest == null)
                {
                    throw new HastlayerException(
                        "There is no supported device with the name \"" + configuration.DeviceName + "\".");
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
                else hardwareImplementation = EmptyHardwareImplementationFactory.Create();

                return new HardwareRepresentation
                {
                    SoftAssemblyPaths = assembliesPaths,
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
                    string.Join(", ", assembliesPaths);
                LogException(ex, message);
                throw new HastlayerException(message, ex);
            }
        }

        public async Task<T> GenerateProxyAsync<T>(
            IHardwareRepresentation hardwareRepresentation,
            T hardwareObject,
            IProxyGenerationConfiguration configuration = null) where T : class
        {
            if (configuration is null) configuration = ProxyGenerationConfiguration.Default;
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
                scope = _serviceProvider.CreateScope();
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
                scope = _serviceProvider.CreateScope();
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
            var moduleFolderPaths = new List<string>();

            // Since Hast.Core either exists or not we need to start by probing for the Hast.Abstractions folder.
            var abstractionsPath = Path.GetDirectoryName(GetType().Assembly.Location);
            var currentDirectory = Path.GetFileName(abstractionsPath);
            if (currentDirectory.Equals("Debug", StringComparison.OrdinalIgnoreCase) ||
                currentDirectory.Equals("Release", StringComparison.OrdinalIgnoreCase))
            {
                abstractionsPath = Path.GetDirectoryName(abstractionsPath);
            }
            currentDirectory = Path.GetFileName(abstractionsPath);
            if (currentDirectory.Equals("bin", StringComparison.OrdinalIgnoreCase))
            {
                abstractionsPath = Path.GetDirectoryName(abstractionsPath);
            }

            // Now we're at the level above the current project's folder.
            abstractionsPath = Path.GetDirectoryName(abstractionsPath);

            var coreFound = false;
            while (abstractionsPath != null && !coreFound)
            {
                var abstractionsSubFolder = Path.Combine(abstractionsPath, "Hast.Abstractions");
                if (Directory.Exists(abstractionsSubFolder))
                {
                    abstractionsPath = abstractionsSubFolder;
                    coreFound = true;
                }
                else
                {
                    abstractionsPath = Path.GetDirectoryName(abstractionsPath);
                }
            }

            // There won't be an Abstractions folder, nor a Core one when the app is being run from a deployment folder
            // (as opposed to a solution).
            if (!string.IsNullOrEmpty(abstractionsPath))
            {
                moduleFolderPaths.Add(abstractionsPath);
            }

            if (_configuration.Flavor == HastlayerFlavor.Developer)
            {
                var corePath = !string.IsNullOrEmpty(abstractionsPath) ?
                    Path.Combine(Path.GetDirectoryName(abstractionsPath), "Hast.Core") :
                    null;

                if (corePath != null && Directory.Exists(corePath)) moduleFolderPaths.Add(corePath);
            }

            var factory = _serviceProvider.GetService<IMemberInvocationHandlerFactory>();
            factory.MemberExecutedOnHardware += (_, context) =>
                ExecutedOnHardware?.Invoke(this, new ServiceEventArgs<IMemberHardwareExecutionContext>(context));
            factory.MemberInvoking += (_, context) =>
                Invoking?.Invoke(this, new ServiceEventArgs<IMemberInvocationContext>(context));
        }

        private void LogException(Exception exception, string message) =>
            _serviceProvider.GetService<ILogger<Hastlayer>>().LogError(exception, message);

        private void ThrowIfService<TOut>()
        {
            if (_serviceNames.Contains(typeof(TOut).FullName))
            {
                throw new InvalidOperationException($"The return type (used: {typeof(TOut).FullName}) must not be a registered service.");
            }
        }

        private static IEnumerable<Assembly> GetHastLibraries(string path = ".") =>
            DependencyInterfaceContainer.LoadAssemblies(
                Directory
                    .GetFiles(path, "Hast.*.dll")
                    .Where(path => !Path.GetFileName(path)
                        // Check any core project names that aren't referenced by Hastlayer to avoid accidental loading.
                        .StartsWith("Hast.Remote.Worker", StringComparison.Ordinal)
                    ));
    }
}
