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
            // Update xclbin file path. Skip if the validated bit file already exists.
            implementation.BinaryPath = Regex.Replace(implementation.BinaryPath, @"\.xclbin$", ".bit.xclbin");
            if (File.Exists(implementation.BinaryPath)) return;

            var configuration = context.Configuration.GetOrAddAzureAttestationConfiguration();
            configuration.SetupAndVerify();

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
    }
}
