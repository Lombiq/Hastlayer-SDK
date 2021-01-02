using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Vitis.Abstractions.Extensions;
using Hast.Vitis.Abstractions.Models;
using Hast.Xilinx.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Hast.Vitis.Abstractions.Services
{
    public class AzureHardwareImplementationComposerBuildProvider : IHardwareImplementationComposerBuildProvider
    {
        private readonly ILogger<AzureHardwareImplementationComposerBuildProvider> _logger;
        public ISet<string> Requirements { get; } = new HashSet<string> { nameof(VitisCommunicationService) };

        public AzureHardwareImplementationComposerBuildProvider(
            ILogger<AzureHardwareImplementationComposerBuildProvider> logger) =>
            _logger = logger;

        public bool CanCompose(IHardwareImplementationCompositionContext context) =>
            context.DeviceManifest is XilinxDeviceManifest xilinxDeviceManifest &&
            xilinxDeviceManifest.Name.StartsWith("Azure", StringComparison.InvariantCulture);

        public async Task BuildAsync(
            IHardwareImplementationCompositionContext context,
            IHardwareImplementation implementation)
        {
            // Update XCLBIN file path. Skip if the validated bit file already exists.
            implementation.BinaryPath = Regex.Replace(implementation.BinaryPath, @"\.xclbin$", ".bit.xclbin");
            if (System.IO.File.Exists(implementation.BinaryPath)) return;

            var configuration = context.Configuration.GetOrAddAzureAttestationConfiguration();
            VerifyConfiguration(configuration);

            await UploadToStorageAsync(configuration);
            await PerformAttestationAsync(configuration);
            await DownloadValidatedFileAsnyc(configuration, implementation.BinaryPath);
        }

        private async Task UploadToStorageAsync(AzureAttestationConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        private async Task PerformAttestationAsync(AzureAttestationConfiguration configuration)
        {
            var (orchestrationId, errorMessage) = await GetResponseAsync<AzureStartResponseData>(
                configuration.StartFunctionUrl,
                new AzureStartPostData(configuration));
            if (!string.IsNullOrWhiteSpace(errorMessage) && errorMessage != "None")
            {
                throw new InvalidOperationException($"Couldn't start attestation: {errorMessage}");
            }

            _logger.LogInformation(
                "Attestation request was submitted successfully with Orchestration ID: {0}",
                errorMessage);
            _logger.LogInformation("Checking the status of attestation using {0}", configuration.PollFunctionUrl);

            while (true)
            {
                var (statusText, output) = await GetResponseAsync<AzurePollResponseData>(
                    configuration.PollFunctionUrl,
                    new AzurePollPostData(configuration, orchestrationId));

                var status = Enum.TryParse<OrchestrationStatus>(statusText, out var newStatus)
                    ? newStatus
                    : (OrchestrationStatus?) null;
                switch (status)
                {
                    case OrchestrationStatus.Pending:
                    case OrchestrationStatus.Running:
                        _logger.LogInformation("Polled status: {0}", statusText);
                        await Task.Delay(TimeSpan.FromSeconds(30));
                        break;
                    case OrchestrationStatus.Failed:
                    case OrchestrationStatus.Successful:
                        var outputList = output?.ToArray() ?? Array.Empty<string>();
                        var runStatus = outputList.FirstOrDefault();
                        if (runStatus == "Attestation process succeeded") return;

                        // TODO download the logs from blob storage before throwing this exception.
                        throw new InvalidOperationException($"Attestation failed: {runStatus ?? "Unknown issue"}");
                    default:
                        throw new InvalidOperationException($"Unknown polled status: {statusText}");
                }
            }
        }

        private async Task DownloadValidatedFileAsnyc(AzureAttestationConfiguration configuration, string binaryPath)
        {
            throw new NotImplementedException();
        }

        private static async Task<T> GetResponseAsync<T>(Uri url, object post)
        {
            const string jsonContentType = "application/json";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(jsonContentType));
            var response = await client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(post)));
            if (!response.IsSuccessStatusCode) throw new WebException(response.StatusCode.ToString());

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content) || JsonConvert.DeserializeObject<T>(content) is not { } result)
            {
                throw new InvalidOperationException("Response is empty.");
            }

            return result;
        }

        private static void VerifyConfiguration(AzureAttestationConfiguration configuration)
        {
            if (configuration.StartFunctionUrl == null) ThrowMissing(nameof(configuration.StartFunctionUrl));
            if (configuration.PollFunctionUrl == null) ThrowMissing(nameof(configuration.PollFunctionUrl));
            if (string.IsNullOrEmpty(configuration.StorageAccountName)) ThrowMissing(nameof(configuration.StorageAccountName));
            if (string.IsNullOrEmpty(configuration.Container)) ThrowMissing(nameof(configuration.Container));
            if (string.IsNullOrEmpty(configuration.NetlistName)) ThrowMissing(nameof(configuration.NetlistName));
            if (string.IsNullOrEmpty(configuration.BlobContainerSas)) ThrowMissing(nameof(configuration.BlobContainerSas));
            if (string.IsNullOrEmpty(configuration.ClientTenantId)) ThrowMissing(nameof(configuration.ClientTenantId));
            if (string.IsNullOrEmpty(configuration.ClientSubscriptionId)) ThrowMissing(nameof(configuration.ClientSubscriptionId));
        }

        private static void ThrowMissing(string name) =>
            throw new InvalidOperationException(
                $"The property '{name}' is missing or empty. It is required to deal with the attestation service. " +
                $"Please specify it in the appsettings.json or otherwise set the " +
                $"{nameof(IHardwareGenerationConfiguration)}. See the readme of the Hast.Vitis.Abstractions library " +
                $"for further details.");
    }
}
