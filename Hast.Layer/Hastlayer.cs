using Hast.Catapult.Abstractions;
using Hast.Common.Services;
using Hast.Common.Validation;
using Hast.Communication;
using Hast.Communication.Services;
using Hast.Layer.Extensibility.Events;
using Hast.Layer.Models;
using Hast.Remote.Client;
using Hast.Synthesis.Abstractions;
using Hast.Transformer;
using Hast.Transformer.Abstractions;
using Hast.Xilinx.Abstractions.ManifestProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Hast.Layer
{
    public class Hastlayer : IHastlayer
    {
        private readonly IHastlayerConfiguration _configuration;
        private readonly ServiceProvider _serviceProvider;

        public event ExecutedOnHardwareEventHandler ExecutedOnHardware;


        // Private so the static factory should be used.
        private Hastlayer(IHastlayerConfiguration configuration)
        {
            _configuration = configuration;

            var dynamicAssemblies = configuration.DynamicAssemblies.Any() ?
                configuration.DynamicAssemblies :
                 Directory.GetFiles(".", "Hast.*.dll");

            var services = new ServiceCollection();
            services.AddIDependencyContainer(dynamicAssemblies);
            services.AddSingleton(configuration);
            services.AddSingleton<IAppDataFolder>(new AppDataFolder(configuration.AppDataFolderPath));
            services.AddSingleton<IHardwareExecutionEventHandlerHolder, HardwareExecutionEventHandlerHolder>();
            configuration.OnServiceRegistration?.Invoke(configuration, services);

            var transformerServices = services.Where(x => x.ServiceType == typeof(ITransformer)).ToList();
            if (transformerServices.Count > 1)
            {
                if (configuration.Flavor == HastlayerFlavor.Client)
                {
                    services.RemoveImplementationsExcept<ITransformer>("RemoteTransformer");
                }
                else
                {
                    services.RemoveImplementationsExcept<ITransformer>("DefaultTransformer");
                }
            }

            _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        }
        public static Task<IHastlayer> Create() => Create(HastlayerConfiguration.Default);

        /// <summary>
        /// Instantiates a new <see cref="IHastlayer"/> implementation.
        /// </summary>
        /// <remarks>
        /// Point of this factory is that it returns an interface type instead of the implementation and can throw
        /// exceptions.
        /// </remarks>
        /// <param name="configuration">Configuration for Hastlayer.</param>
        /// <returns>A newly created <see cref="IHastlayer"/> implementation.</returns>
        public static Task<IHastlayer> Create(IHastlayerConfiguration configuration)
        {
            Argument.ThrowIfNull(configuration, nameof(configuration));
            Argument.ThrowIfNull(configuration.Extensions, nameof(configuration.Extensions));

            var hastlayer = new Hastlayer(configuration);
            hastlayer.LoadHost();
            return Task.FromResult((IHastlayer)hastlayer);
        }


        public void Dispose()
        {
            _serviceProvider.Dispose();
        }

        public async Task<IHardwareRepresentation> GenerateHardware(
            IEnumerable<string> assembliesPaths,
            IHardwareGenerationConfiguration configuration)
        {
            Argument.ThrowIfNull(assembliesPaths, nameof(assembliesPaths));
            if (!assembliesPaths.Any())
            {
                throw new ArgumentException("No assemblies were specified.");
            }

            if (assembliesPaths.Count() != assembliesPaths.Distinct().Count())
            {
                throw new ArgumentException(
                    "The same assembly was included multiple times. Only supply each assembly to generate hardware from once.");
            }

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var transformer = scope.ServiceProvider.GetService<ITransformer>();
                    var hardwareImplementationComposer = scope.ServiceProvider.GetService<IHardwareImplementationComposer>();
                    var deviceManifestSelector = scope.ServiceProvider.GetService<IDeviceManifestSelector>();
                    var loggerService = scope.ServiceProvider.GetService<ILogger>();

                    var hardwareDescription = await transformer.Transform(assembliesPaths, configuration);

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
                            "There is no supported device with the name " + configuration.DeviceName + ".");
                    }

                    var hardwareImplementation = await hardwareImplementationComposer
                        .Compose(configuration, hardwareDescription, deviceManifest);

                    return new HardwareRepresentation
                    {
                        SoftAssemblyPaths = assembliesPaths,
                        HardwareDescription = hardwareDescription,
                        HardwareImplementation = hardwareImplementation,
                        DeviceManifest = deviceManifest
                    };
                }
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

        public async Task<T> GenerateProxy<T>(
            IHardwareRepresentation hardwareRepresentation,
            T hardwareObject,
            IProxyGenerationConfiguration configuration) where T : class
        {
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

        public async Task<ICommunicationService> GetCommunicationService(string communicationChannelName) => await Task.Run(() =>
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                return scope.ServiceProvider.GetService<ICommunicationServiceSelector>().GetCommunicationService(communicationChannelName);
            }
        });

        public Task<IEnumerable<IDeviceManifest>> GetSupportedDevices() => Task.Run(() =>
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                return scope.ServiceProvider.GetService<IDeviceManifestSelector>().GetSupportedDevices();
            }
        });

        public async Task RunAsync<T>(Func<T, Task> process)
        {
            using (var scope = _serviceProvider.CreateScope())
                await process(scope.ServiceProvider.GetService<T>());
        }

        public async Task<Tout> RunGetAsync<Tout>(Func<IServiceProvider, Task<Tout>> process)
        {
            using (var scope = _serviceProvider.CreateScope())
                return await process(scope.ServiceProvider);
        }

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

            var importedExtensions = new List<Assembly>
            {
                typeof(Hastlayer).Assembly,
                typeof(IProxyGenerator).Assembly,
                typeof(IHardwareImplementationComposer).Assembly,
                typeof(ITransformer).Assembly,
                typeof(NexysA7ManifestProvider).Assembly,
                typeof(CatapultManifestProvider).Assembly
            };

            // Adding imported extensions last so they can override anything.
            importedExtensions.AddRange(_configuration.Extensions);

            _serviceProvider.GetService<IHardwareExecutionEventHandlerHolder>()
                .RegisterExecutedOnHardwareEventHandler(eventArgs => ExecutedOnHardware?.Invoke(this, eventArgs));
        }

        private void LogException(Exception exception, string message) =>
            _serviceProvider.GetService<ILogger>().LogError(exception, message);
    }
}
