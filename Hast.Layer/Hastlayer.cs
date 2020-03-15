using Autofac;
using Hast.Catapult.Abstractions;
using Hast.Communication;
using Hast.Communication.Services;
using Hast.Layer.Extensibility.Events;
using Hast.Layer.Models;
using Hast.Synthesis.Abstractions;
using Hast.Transformer.Abstractions;
using Hast.Xilinx.Abstractions.ManifestProviders;
using Lombiq.OrchardAppHost;
using Lombiq.OrchardAppHost.Configuration;
using Orchard.Environment.Configuration;
using Orchard.Exceptions;
using Orchard.Logging;
using Orchard.Validation;
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
        private const string ShellName = ShellSettings.DefaultName;

        private readonly IHastlayerConfiguration _configuration;
        private IOrchardAppHost _host;

        public event ExecutedOnHardwareEventHandler ExecutedOnHardware;


        // Private so the static factory should be used.
        private Hastlayer(IHastlayerConfiguration configuration)
        {
            _configuration = configuration;
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
        public static async Task<IHastlayer> Create(IHastlayerConfiguration configuration)
        {
            Argument.ThrowIfNull(configuration, nameof(configuration));
            Argument.ThrowIfNull(configuration.Extensions, nameof(configuration.Extensions));

            var hastlayer = new Hastlayer(configuration);
            // It's easier to eagerly load the host than to lazily create it, because the latter would also need 
            // synchronization to allow concurrent access to this type's instance methods.
            await hastlayer.LoadHost();
            return hastlayer;
        }


        public Task<IEnumerable<IDeviceManifest>> GetSupportedDevices() =>
            _host.RunGet(scope => Task.FromResult(scope.Resolve<IDeviceManifestSelector>().GetSupportedDevices()));

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
                HardwareRepresentation hardwareRepresentation = null;

                await _host
                    .Run<ITransformer, IHardwareImplementationComposer, IDeviceManifestSelector, ILoggerService>(
                        async (transformer, hardwareImplementationComposer, deviceManifestSelector, loggerService) =>
                        {
                            var hardwareDescription = await transformer.Transform(assembliesPaths, configuration);

                            foreach (var warning in hardwareDescription.Warnings)
                            {
                                loggerService.Warning(
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

                            hardwareRepresentation = new HardwareRepresentation
                            {
                                SoftAssemblyPaths = assembliesPaths,
                                HardwareDescription = hardwareDescription,
                                HardwareImplementation = hardwareImplementation,
                                DeviceManifest = deviceManifest
                            };
                        }, ShellName, false);

                return hardwareRepresentation;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                var message =
                    "An error happened during generating the Hastlayer hardware representation for the following assemblies: " +
                    string.Join(", ", assembliesPaths);
                await _host.Run<ILoggerService>(logger => Task.Run(() => logger.Error(ex, message)));
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
                return await
                    _host
                    .RunGet(scope => Task.Run(() => scope.Resolve<IProxyGenerator>().CreateCommunicationProxy(hardwareRepresentation, hardwareObject, configuration)));
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                var message =
                    "An error happened during generating the Hastlayer proxy for an object of the following type: " +
                    hardwareObject.GetType().FullName;
                await _host.Run<ILoggerService>(logger => Task.Run(() => logger.Error(ex, message)));
                throw new HastlayerException(message, ex);
            }
        }

        public async Task<ICommunicationService> GetCommunicationService(string communicationChannelName) =>
            await _host.RunGet(scope => Task.FromResult(scope.Resolve<ICommunicationServiceSelector>()
                .GetCommunicationService(communicationChannelName)));

        public void Dispose()
        {
            if (_host == null) return;

            _host.Dispose();
            _host = null;
        }


        private async Task LoadHost()
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

            var importedExtensions = new[]
            {
                typeof(Hastlayer).Assembly,
                typeof(IProxyGenerator).Assembly,
                typeof(IHardwareImplementationComposer).Assembly,
                typeof(ITransformer).Assembly,
                typeof(NexysA7ManifestProvider).Assembly,
                typeof(CatapultManifestProvider).Assembly
            }
            .ToList();

            if (_configuration.Flavor == HastlayerFlavor.Client)
            {
                importedExtensions.Add(typeof(Remote.Client.RemoteTransformer).Assembly);
            }

            // Adding imported extensions last so they can override anything.
            importedExtensions.AddRange(_configuration.Extensions);

            var settings = new AppHostSettings
            {
                // Setting a custom path so if the parent app is also an AppHost app then with the default settings
                // those won't clash.
                AppDataFolderPath = "~/Hastlayer/App_Data",
                ImportedExtensions = importedExtensions,
                DefaultShellFeatureStates = new[]
                {
                    new DefaultShellFeatureState
                    {
                        ShellName = ShellName,
                        EnabledFeatures = importedExtensions.Select(extension => extension.ShortName())
                    }
                },
                ModuleFolderPaths = moduleFolderPaths
            };


            var registrations = new AppHostRegistrations
            {
                HostRegistrations = builder => builder
                    .RegisterType<HardwareExecutionEventHandlerHolder>()
                    .As<IHardwareExecutionEventHandlerHolder>()
                    .SingleInstance()
            };

            _host = await OrchardAppHostFactory.StartTransientHost(settings, registrations, null);

            await _host.Run<IHardwareExecutionEventHandlerHolder>(proxy => Task.Run(() =>
                proxy.RegisterExecutedOnHardwareEventHandler(eventArgs => ExecutedOnHardware?.Invoke(this, eventArgs))));

            // Enable all loaded features. This is needed so extensions just added to the solution, but not referenced
            // anywhere in the current app can contribute dependencies.
            await _host
                .Run<Orchard.Environment.Features.IFeatureManager>(
                    (featureManager) =>
                    {
                        featureManager.EnableFeatures(featureManager.GetAvailableFeatures().Select(feature => feature.Id), true);

                        return Task.CompletedTask;
                    }, ShellName, false);
        }
    }
}
