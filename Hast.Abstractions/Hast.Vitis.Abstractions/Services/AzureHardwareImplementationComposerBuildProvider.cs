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
using System.Text;
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

            _logger.LogInformation(
                "Attestation request was submitted successfully with Orchestration ID: {0}",
                orchestrationId);
            _logger.LogInformation("Checking the status of attestation using {0}", configuration.PollFunctionUrl);

            while (true)
            {
                var (statusText, output) = await GetResponseAsync<AzurePollResponseData>(
                    configuration.PollFunctionUrl,
                    new AzurePollPostData(configuration, orchestrationId));
                var statusUpper = statusText.ToUpperInvariant();

                _logger.LogInformation("Polled status: {0}", statusText);

                if (statusUpper == "PENDING" || statusUpper == "RUNNING")
                {
                    await Task.Delay(TimeSpan.FromSeconds(30));
                    continue;
                }

                var outputList = output?.ToArray() ?? Array.Empty<string>();
                if (outputList.Contains("Attestation process succeeded")) return;

                var outputString = outputList.Any()
                    ? string.Join("\n", outputList.Select(item => "- " + item.Trim()))
                    : "- Unknown issue";
                _logger.LogError("Attestation failed:\n{0}", outputString);

                await DownloadLogsAsync(configuration, binaryPath);
                throw new InvalidOperationException("Attestation failed.");
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

        private async ValueTask<string> GetSharedAccessSignatureAsync(AzureAttestationConfiguration configuration)
        {
            //directly build BlobContainerClient, then pass it to GetServiceSasUriForContainer() method
            var blobContainer = await GetBlobContainerClientAsync(configuration);

            var sasUri = GetServiceSasUriForContainer(blobContainer);
            return sasUri.Query.Substring(1);
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
            string storedPolicyName = null,
            BlobContainerSasPermissions permissions = BlobContainerSasPermissions.All)
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
                sasBuilder.SetPermissions(permissions);
            }
            else
            {
                sasBuilder.Identifier = storedPolicyName;
            }

            var sasUri = containerClient.GenerateSasUri(sasBuilder);
            _logger.LogInformation("SAS URI for blob container is: {0}", sasUri);

            return sasUri;
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
            if (string.IsNullOrWhiteSpace(content) || JsonConvert.DeserializeObject<T>(content) is not { } result)
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

        private static async Task DownloadAsync(BlobClient blobClient, string filePath)
        {
            var download = await blobClient.DownloadAsync();
            await using var stream = File.OpenWrite(filePath);
            await download.Value.Content.CopyToAsync(stream);
            stream.Close();
        }
    }
}
