using Azure.Storage;
using Azure.Storage.Blobs;
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
using System.Threading;
using System.Threading.Tasks;

namespace Hast.Vitis.Abstractions.Services
{
    public class AzureHardwareImplementationComposerBuildProvider : IHardwareImplementationComposerBuildProvider
    {
        private const string BlobContainerName = "hastlayer-attestation";
        private readonly ILogger<AzureHardwareImplementationComposerBuildProvider> _logger;
        private bool _containerChecked;

        public ISet<string> Requirements { get; } = new HashSet<string>
        {
            nameof(VitisHardwareImplementationComposerBuildProvider)
        };

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
            var oldBinaryPath = implementation.BinaryPath;
            implementation.BinaryPath = Regex.Replace(implementation.BinaryPath, @"\.xclbin$", ".bit.xclbin");
            if (File.Exists(implementation.BinaryPath)) return;

            var configuration = context.Configuration.GetOrAddAzureAttestationConfiguration();
            configuration.SetupAndVerify();

            await UploadToStorageAsync(configuration, oldBinaryPath);
            await PerformAttestationAsync(configuration, oldBinaryPath);
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
            await blobClient.UploadAsync(xclbinToUploadStream, true, CancellationToken.None);
        }

        private async Task PerformAttestationAsync(AzureAttestationConfiguration configuration, string binaryPath)
        {
            _logger.LogInformation("Sending attestation start request...");
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
                orchestrationId);
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
            const string extensionRegexp = @"\.xclbin$";
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

        private async Task<string> GetSharedAccessSignatureAsync(AzureAttestationConfiguration configuration)
        {
            //directly build BlobContainerClient, then pass it to GetServiceSasUriForContainer() method
            var blobContainer = await GetBlobContainerClientAsync(configuration);

            var sasUri = GetServiceSasUriForContainer(blobContainer);
            return sasUri.AbsoluteUri;
        }

        private ValueTask<BlobContainerClient> GetBlobContainerClientAsync(AzureAttestationConfiguration configuration)
        {
            var blobServiceClient = new BlobServiceClient(configuration.GenerateStorageConnectionString());

            var client = new BlobContainerClient(
                new Uri($"https://{configuration.StorageAccountName}.blob.core.windows.net/{BlobContainerName}"),
                new StorageSharedKeyCredential(configuration.StorageAccountName, configuration.StorageAccountKey));
            return _containerChecked
                ? new ValueTask<BlobContainerClient>(client)
                : EnsureBlobContainerExistsAsync(client, blobServiceClient);
        }

        private async ValueTask<BlobContainerClient> EnsureBlobContainerExistsAsync(
            BlobContainerClient client,
            BlobServiceClient blobServiceClient)
        {
            if (!await client.ExistsAsync())
            {
                var containerClientResponse = await blobServiceClient.CreateBlobContainerAsync(BlobContainerName);
                client = containerClientResponse.Value;
            }

            _containerChecked = true;
            return client;
        }

        // Based on: https://docs.microsoft.com/en-us/azure/storage/blobs/sas-service-create?tabs=dotnet
        private Uri GetServiceSasUriForContainer(
            BlobContainerClient containerClient,
            string storedPolicyName = null)
        {
            // Check whether this BlobContainerClient object has been authorized with Shared Key.
            if (!containerClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException(
                    "BlobContainerClient must be authorized with Shared Key credentials to create a service SAS.");
            }

            // Create a SAS token that's valid for one hour.
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerClient.Name,
                Resource = "c",
            };

            if (storedPolicyName == null)
            {
                sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddHours(1);
                sasBuilder.SetPermissions(BlobContainerSasPermissions.Read);
            }
            else
            {
                sasBuilder.Identifier = storedPolicyName;
            }

            var sasUri = containerClient.GenerateSasUri(sasBuilder);
            _logger.LogInformation("SAS URI for blob container is: {0}", sasUri);

            return sasUri;
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

        private static async Task DownloadAsync(BlobClient blobClient, string filePath)
        {
            var download = await blobClient.DownloadAsync();
            await using var stream = File.OpenWrite(filePath);
            await download.Value.Content.CopyToAsync(stream);
            stream.Close();
        }
    }
}
