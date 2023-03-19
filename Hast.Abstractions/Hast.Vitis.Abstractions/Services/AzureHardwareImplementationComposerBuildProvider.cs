using Hast.Common.Interfaces;
using Hast.Layer;
using Hast.Synthesis.Delegates;
using Hast.Synthesis.Models;
using Hast.Synthesis.Services;
using Hast.Vitis.Extensions;
using Hast.Vitis.Models;
using Hast.Xilinx;
using Lombiq.HelpfulLibraries.RestEase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Vitis.Services;

[DependencyInitializer(nameof(InitializeService))]
public class AzureHardwareImplementationComposerBuildProvider : IHardwareImplementationComposerBuildProvider
{
    private const string BlobContainerName = "hastlayer-attestation";

    private readonly IAzureAttestationApi _azureAttestationApi;
    private readonly IAzureStorageServiceFactory _azureStorageServiceFactory;
    private readonly ILogger<AzureHardwareImplementationComposerBuildProvider> _logger;

    public IDictionary<string, BuildProviderShortcut> Shortcuts { get; } = new Dictionary<string, BuildProviderShortcut>();

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
        var instanceId = (await _azureAttestationApi.StartAsync(
            configuration.StartFunctionUrl.AbsolutePath.TrimStart('/'),
            new AzureStartPostData(configuration)
            {
                BlobContainerSignature = sharedAccessSignature,
                Container = BlobContainerName,
                NetlistName = Path.GetFileName(binaryPath),
            })).InstanceId;

        _logger.LogInformation(
            "Attestation request was submitted successfully with the following orchestration instance ID: {InstanceId}",
            instanceId);
        _logger.LogInformation("Checking the status of attestation using {PollFunctionUrl}", configuration.PollFunctionUrl);

        while (true)
        {
            var (statusText, output) = await _azureAttestationApi.PollAsync(
                configuration.PollFunctionUrl.AbsolutePath.TrimStart('/'),
                new AzurePollPostData(configuration, instanceId));
            var statusUpper = statusText.ToUpperInvariant();

            _logger.LogInformation("Polled status: {StatusText}", statusText);

            if (statusUpper is "PENDING" or "RUNNING")
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
                continue;
            }

            var outputList = output?.AsList() ?? Array.Empty<string>();
            if (outputList.Contains("Attestation process succeeded"))
            {
                _logger.LogInformation("Attestation process has succeeded.");
                return;
            }

            var outputString = outputList.Any()
                ? string.Join("\n", outputList.Select(item => "- " + item.Trim()))
                : "- Unknown issue";
            _logger.LogError("Attestation failed:\n{OutputString}", outputString);

            throw new InvalidOperationException("Attestation failed.");
        }
    }

    private static string UpdateBinaryPath(string input) => input.RegexReplace(@"\.xclbin$", ".azure.xclbin");

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

    public void InvokeProgress(BuildProgressEventArgs eventArgs)
    {
        // There are no numbered steps for Azure.
    }
}
