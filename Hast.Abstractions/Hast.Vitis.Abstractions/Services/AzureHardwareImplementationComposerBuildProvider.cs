using Hast.Common.Interfaces;
using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Vitis.Abstractions.Extensions;
using Hast.Vitis.Abstractions.Models;
using Hast.Xilinx.Abstractions;
using Lombiq.HelpfulLibraries.RestEase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Hast.Vitis.Abstractions.Services
{
    [IDependencyInitializer(nameof(InitializeService))]
    public class AzureHardwareImplementationComposerBuildProvider : IHardwareImplementationComposerBuildProvider
    {
        private const string BlobContainerName = "hastlayer-attestation";

        private readonly IAzureAttestationApi _azureAttestationApi;
        private readonly IAzureStorageServiceFactory _azureStorageServiceFactory;
        private readonly ILogger<AzureHardwareImplementationComposerBuildProvider> _logger;

        public Dictionary<string, BuildProviderShortcut> Shortcuts { get; } = new();

        public ISet<string> Requirements { get; } = new HashSet<string>
        {
            nameof(VitisHardwareImplementationComposerBuildProvider),
        };

        public AzureHardwareImplementationComposerBuildProvider(
            IAzureAttestationApi azureAttestationApi,
            IAzureStorageServiceFactory azureStorageServiceFactory,
            ILogger<AzureHardwareImplementationComposerBuildProvider> logger)
        {
            _azureAttestationApi = azureAttestationApi;
            _azureStorageServiceFactory = azureStorageServiceFactory;
            _logger = logger;
        }

        public bool CanCompose(IHardwareImplementationCompositionContext context) =>
            context.DeviceManifest is AzureNpDeviceManifest;

        public async Task BuildAsync(
            IHardwareImplementationCompositionContext context,
            IHardwareImplementation implementation)
        {
            // Update xclbin file path. Skip if the validated bit file already exists.
            var oldBinaryPath = implementation.BinaryPath;
            var directoryPath = Path.GetDirectoryName(oldBinaryPath);
            var fileNameBase = Path.GetFileNameWithoutExtension(oldBinaryPath);
            implementation.BinaryPath = UpdateBinaryPath(implementation.BinaryPath);
            if (File.Exists(implementation.BinaryPath)) return;

            var configuration = context.Configuration
                .GetOrAddAzureAttestationConfiguration()
                .Verify();

            _logger.LogInformation("Setting up storage client...");
            var azureStorageService = _azureStorageServiceFactory.CreateForBlob(configuration, BlobContainerName);

            try
            {
                await azureStorageService.UploadAsync(oldBinaryPath);

                var sharedAccessSignature = await azureStorageService.GetSharedAccessSignatureAsync();
                await PerformAttestationAsync(configuration, oldBinaryPath, sharedAccessSignature);

                _logger.LogInformation("Downloading validated xclbin files...");
                await azureStorageService.DownloadAsync(
                    directoryPath,
                    fileNameBase + ".bit.xclbin",
                    fileNameBase + ".azure.xclbin");
            }
            finally
            {
                _logger.LogInformation("Downloading log files...");
                await azureStorageService.DownloadMaybeAsync(
                    directoryPath,
                    fileNameBase + "-log.txt",
                    fileNameBase + "-logPhase1.txt",
                    fileNameBase + "-logPhase2.txt",
                    fileNameBase + "-logPhase3.txt");
            }
        }

        public void AddShortcuts(IEnumerable<IHardwareImplementationComposerBuildProvider> providers)
        {
            var shortcuts = providers
                .Single(provider => provider.Name == nameof(VitisHardwareImplementationComposerBuildProvider))
                .Shortcuts;
            shortcuts.Add(
                nameof(AzureHardwareImplementationComposerBuildProvider),
                context =>
                    File.Exists(
                        UpdateBinaryPath(
                            VitisHardwareImplementationComposerBuildProvider
                                .GetBinaryPath(context.Configuration, context.HardwareDescription))));
        }

        private async Task PerformAttestationAsync(
            AzureAttestationConfiguration configuration,
            string binaryPath,
            string sharedAccessSignature)
        {
            _logger.LogInformation("Sending attestation start request...");
            var instanceId = (await _azureAttestationApi.Start(
                configuration.StartFunctionUrl.AbsolutePath.TrimStart('/'),
                new AzureStartPostData(configuration)
                {
                    BlobContainerSignature = sharedAccessSignature,
                    Container = BlobContainerName,
                    NetlistName = Path.GetFileName(binaryPath),
                })).InstanceId;

            _logger.LogInformation(
                "Attestation request was submitted successfully with the following orchestration instance ID: {0}",
                instanceId);
            _logger.LogInformation("Checking the status of attestation using {0}", configuration.PollFunctionUrl);

            while (true)
            {
                var (statusText, output) = await _azureAttestationApi.Poll(
                    configuration.PollFunctionUrl.AbsolutePath.TrimStart('/'),
                    new AzurePollPostData(configuration, instanceId));
                var statusUpper = statusText.ToUpperInvariant();

                _logger.LogInformation("Polled status: {0}", statusText);

                if (statusUpper == "PENDING" || statusUpper == "RUNNING")
                {
                    await Task.Delay(TimeSpan.FromSeconds(30));
                    continue;
                }

                var outputList = output?.ToArray() ?? Array.Empty<string>();
                if (outputList.Contains("Attestation process succeeded"))
                {
                    _logger.LogInformation("Attestation process has succeeded.");
                    return;
                }

                var outputString = outputList.Any()
                    ? string.Join("\n", outputList.Select(item => "- " + item.Trim()))
                    : "- Unknown issue";
                _logger.LogError("Attestation failed:\n{0}", outputString);

                throw new InvalidOperationException("Attestation failed.");
            }
        }

        private static string UpdateBinaryPath(string input) => Regex.Replace(input, @"\.xclbin$", ".azure.xclbin");

        public static void InitializeService(IServiceCollection services) =>
            services.AddRestEaseHttpClient<IAzureAttestationApi>(
                nameof(IAzureAttestationApi),
                provider =>
                {
                    // The service provider we receive is not scoped so we can't reach the
                    // IHardwareGenerationConfigurationAccessor service through it. Even if we created a new scope its
                    // content would be uninitialized so we have to build it from configuration.
                    var configuration = new AzureAttestationConfiguration();
                    provider
                        .GetRequiredService<IConfiguration>()
                        .GetSection(nameof(HardwareGenerationConfiguration))
                        .GetSection(nameof(HardwareGenerationConfiguration.CustomConfiguration))
                        .GetSection(nameof(AzureAttestationConfiguration))
                        .Bind(configuration);

                    return new Uri(configuration.StartFunctionUrl, "/").AbsoluteUri;
                });

        public void InvokeProgress(BuildProgressEventArgs eventArgs) { }
    }
}
