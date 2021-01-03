using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
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
using static Azure.Storage.Sas.BlobContainerSasPermissions;

namespace Hast.Vitis.Abstractions.Services
{
    public class AzureHardwareImplementationComposerBuildProvider : IHardwareImplementationComposerBuildProvider
    {
        private const string BlobContainerName = "hastlayer-attestation";
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

            await UploadToStorageAsync(configuration, implementation.BinaryPath);
            await PerformAttestationAsync(configuration, implementation.BinaryPath);
            await DownloadValidatedFileAsync(configuration, implementation.BinaryPath);
        }

        private async Task UploadToStorageAsync(
            AzureAttestationConfiguration configuration,
            string binaryPath)
        {
            _logger.LogInformation("Setting up storage client...");
            var containerClient = await GetBlobContainerClientAsync(configuration);
            var blobClient = containerClient.GetBlobClient(Path.GetFileName(binaryPath));

            if ((await blobClient.ExistsAsync()).Value)
            {
                _logger.LogInformation("The xclbin file is already uploaded to the storage container!");
                return;
            }

            _logger.LogInformation("Uploading file...");
            await using var xclbinToUploadStream = File.OpenRead(binaryPath);
            await blobClient.UploadAsync(xclbinToUploadStream);
            xclbinToUploadStream.Close();
        }

        private async Task PerformAttestationAsync(AzureAttestationConfiguration configuration, string binaryPath)
        {
            var (orchestrationId, errorMessage) = await GetResponseAsync<AzureStartResponseData>(
                configuration.StartFunctionUrl,
                new AzureStartPostData(configuration)
                {
                    BlobContainerSignature = await GetSharedAccessSignatureAsync(configuration),
                    Container = BlobContainerName,
                    NetlistName = Path.GetFileName(binaryPath),
                });
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

                        await DownloadLogsAsync(configuration, binaryPath);
                        throw new InvalidOperationException($"Attestation failed: {runStatus ?? "Unknown issue"}");
                    default:
                        throw new InvalidOperationException($"Unknown polled status: {statusText}");
                }
            }
        }

        private async Task DownloadLogsAsync(AzureAttestationConfiguration configuration, string binaryPath)
        {
            const string extensionRegexp = @"\.bit\.xclbin$";
            var logFiles = new []
            {
                Regex.Replace(binaryPath, extensionRegexp, "-log.txt"),
                Regex.Replace(binaryPath, extensionRegexp, "-logPhase0.txt"),
                Regex.Replace(binaryPath, extensionRegexp, "-logPhase1.txt"),
                Regex.Replace(binaryPath, extensionRegexp, "-logPhase2.txt"),
                Regex.Replace(binaryPath, extensionRegexp, "-logPhase3.txt"),
            }; // The logPhase files are either 0-2 or 1-3, the doc doesn't make this clear.

            var containerClient = await GetBlobContainerClientAsync(configuration);
            foreach (var logFile in logFiles)
            {
                var logFileName = Path.GetFileName(logFile);
                var blobClient = containerClient.GetBlobClient(logFileName);
                if (!await blobClient.ExistsAsync()) continue;

                try
                {
                    if (File.Exists(logFile)) File.Delete(logFile);

                    await DownloadAsync(blobClient, logFile);
                    _logger.LogInformation("Log file downloaded: {0}", logFile);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to download log file: {0}", logFileName);
                }
            }
        }

        private async Task DownloadValidatedFileAsync(AzureAttestationConfiguration configuration, string binaryPath)
        {
            var containerClient = await GetBlobContainerClientAsync(configuration);
            var blobClient = containerClient.GetBlobClient(Path.GetFileName(binaryPath));

            await DownloadAsync(blobClient, binaryPath);
        }

        private static async Task<string> GetSharedAccessSignatureAsync(AzureAttestationConfiguration configuration)
        {
            var client = await GetBlobContainerClientAsync(configuration);
            var sasUri = await GetUserDelegationSasContainerAsync(client);
            return sasUri.AbsoluteUri;
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

        private static async Task<BlobContainerClient> GetBlobContainerClientAsync(
            AzureAttestationConfiguration configuration)
        {
            var blobServiceClient = new BlobServiceClient(configuration.GenerateStorageConnectionString());
            var containerClientResponse = await blobServiceClient.CreateBlobContainerAsync(BlobContainerName);
            return containerClientResponse.Value;
        }

        private static async Task<Uri> GetUserDelegationSasContainerAsync(BlobContainerClient blobContainerClient)
        {
            var blobServiceClient = blobContainerClient.GetParentBlobServiceClient();

            // Values configured as per Running FPGA Attestation for Azure Private Preview document.
            var startOffset = DateTimeOffset.UtcNow;
            var expirationOffset = startOffset.AddHours(6);
            var userDelegationKey = await blobServiceClient.GetUserDelegationKeyAsync(startOffset, expirationOffset);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = blobContainerClient.Name,
                Resource = "c",
                ExpiresOn = expirationOffset,
            };
            sasBuilder.SetPermissions(Read | Write | Create);

            var uriBuilder = new BlobUriBuilder(blobContainerClient.Uri)
            {
                Sas = sasBuilder.ToSasQueryParameters(userDelegationKey, blobServiceClient.AccountName),
            };
            return uriBuilder.ToUri();
        }

        private static async Task DownloadAsync(BlobClient blobClient, string filePath)
        {
            var download = await blobClient.DownloadAsync();
            await using var stream = File.OpenWrite(filePath);
            await download.Value.Content.CopyToAsync(stream);
            stream.Close();
        }
    }
}
