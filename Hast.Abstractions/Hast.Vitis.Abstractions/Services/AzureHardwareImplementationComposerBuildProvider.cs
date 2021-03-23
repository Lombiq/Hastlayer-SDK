using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Vitis.Abstractions.Extensions;
using Hast.Vitis.Abstractions.Models;
using Hast.Xilinx.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Hast.Vitis.Abstractions.Services
{
    public class AzureHardwareImplementationComposerBuildProvider : IHardwareImplementationComposerBuildProvider
    {
        private const string BlobContainerName = "hastlayer-attestation";
        private readonly IAzureStorageServiceFactory _azureStorageServiceFactory;
        private readonly ILogger<AzureHardwareImplementationComposerBuildProvider> _logger;

        public Dictionary<string, BuildProviderShortcut> Shortcuts { get; } = new Dictionary<string, BuildProviderShortcut>();

        public ISet<string> Requirements { get; } = new HashSet<string>
        {
            nameof(VitisHardwareImplementationComposerBuildProvider)
        };

        public AzureHardwareImplementationComposerBuildProvider(
            IAzureStorageServiceFactory azureStorageServiceFactory,
            ILogger<AzureHardwareImplementationComposerBuildProvider> logger)
        {
            _azureStorageServiceFactory = azureStorageServiceFactory;
            _logger = logger;
        }

        public bool CanCompose(IHardwareImplementationCompositionContext context) =>
            context.DeviceManifest is XilinxDeviceManifest xilinxDeviceManifest &&
            xilinxDeviceManifest.Name.StartsWith("Azure", StringComparison.InvariantCulture);

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
                .SetupAndVerify();

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
                    fileNameBase + "-logPhase0.txt",
                    fileNameBase + "-logPhase1.txt",
                    fileNameBase + "-logPhase2.txt",
                    fileNameBase + "-logPhase3.txt");
            }
        }

        public void AddShortcutsToOtherProviders(IEnumerable<IHardwareImplementationComposerBuildProvider> providers)
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
            var instanceId = (await GetResponseAsync<AzureResponseData>(
                configuration.StartFunctionUrl,
                new AzureStartPostData(configuration)
                {
                    BlobContainerSignature = sharedAccessSignature,
                    Container = BlobContainerName,
                    NetlistName = Path.GetFileName(binaryPath),
                })).InstanceId;

            _logger.LogInformation(
                "Attestation request was submitted successfully with orchestration instance ID: {0}",
                instanceId);
            _logger.LogInformation("Checking the status of attestation using {0}", configuration.PollFunctionUrl);

            while (true)
            {
                var (statusText, output) = await GetResponseAsync<AzurePollResponseData>(
                    configuration.PollFunctionUrl,
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

        private async Task<T> GetResponseAsync<T>(Uri url, object post)
            where T : AzureResponseData
        {
            const string jsonContentType = "application/json";

            var postJson = JsonConvert.SerializeObject(post);
            var postContent = new StringContent(postJson, Encoding.UTF8, jsonContentType);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(jsonContentType));
            var response = await client.PostAsync(url, postContent);

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content) || !(JsonConvert.DeserializeObject<T>(content) is { } result))
            {
                throw response.IsSuccessStatusCode
                    ? new InvalidOperationException("Response is empty.")
                    : new WebException(response.StatusCode.ToString());
            }

            var hasErrorMessage = !string.IsNullOrWhiteSpace(result.ErrorMessage);
            if (!hasErrorMessage && response.IsSuccessStatusCode) return result;

            _logger.LogError(
                "There was an error in the response to the request for {0}. (request: {1}; response: {2})",
                url,
                postJson,
                JsonConvert.SerializeObject(result));
            throw hasErrorMessage
                ? new InvalidOperationException(result.ErrorMessage)
                : new WebException(response.StatusCode.ToString());
        }

        private static string UpdateBinaryPath(string input) => Regex.Replace(input, @"\.xclbin$", ".azure.xclbin");
    }
}
