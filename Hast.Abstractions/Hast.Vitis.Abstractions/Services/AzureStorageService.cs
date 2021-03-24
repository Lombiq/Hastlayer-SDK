using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Hast.Vitis.Abstractions.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Hast.Vitis.Abstractions.Services
{
    public class AzureStorageService
        : IAzureStorageService
    {
        private readonly ILogger _logger;
        private readonly AzureStorageConfiguration _storageConfiguration;
        private readonly string _blobContainerName;

        private bool _containerChecked;


        public AzureStorageService(
            ILogger<AzureStorageService> logger,
            AzureStorageConfiguration storageConfiguration,
            string blobContainerName)
        {
            _logger = logger;
            _storageConfiguration = storageConfiguration;
            _blobContainerName = blobContainerName;
        }

        public async Task UploadAsync(string localFilePath)
        {
            var fileName = Path.GetFileName(localFilePath);

            var containerClient = await GetBlobContainerClientAsync();
            var blobClient = containerClient.GetBlobClient(fileName);

            if ((await blobClient.ExistsAsync()).Value)
            {
                _logger.LogInformation($"The file \"{fileName}\" is already uploaded to the storage container!");
                return;
            }

            _logger.LogInformation($"Uploading file \"{fileName}\"...");
            await using var fileToUploadStream = File.OpenRead(localFilePath);
            await blobClient.UploadAsync(fileToUploadStream, true, CancellationToken.None);
            _logger.LogInformation($"File \"{fileName}\" has been uploaded...");
        }

        public Task<IEnumerable<string>> DownloadAsync(string localDirectoryPath, params string[] remoteFileNames) =>
            DownloadInnerAsync(localDirectoryPath, remoteFileNames, true);

        public Task<IEnumerable<string>> DownloadMaybeAsync(string localDirectoryPath, params string[] remoteFileNames) =>
            DownloadInnerAsync(localDirectoryPath, remoteFileNames, false);

        public async ValueTask<string> GetSharedAccessSignatureAsync()
        {
            // Directly build BlobContainerClient, then pass it to GetServiceSasUriForContainer() method.
            var blobContainer = await GetBlobContainerClientAsync();

            var sasUri = GetServiceSasUriForContainer(blobContainer);
            return sasUri.Query.Substring(1);
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

        private async Task<IEnumerable<string>> DownloadInnerAsync(
            string localDirectoryPath,
            IEnumerable<string> remoteFileNames,
            bool throwOnError)
        {
            var containerClient = await GetBlobContainerClientAsync();
            var localFilePaths = new List<string>();

            localDirectoryPath = Path.GetFullPath(localDirectoryPath);

            foreach (var remoteFileName in remoteFileNames)
            {
                var blobClient = containerClient.GetBlobClient(remoteFileName);
                var localPath = Path.Combine(localDirectoryPath, remoteFileName);

                try
                {
                    await DownloadAsync(blobClient, localPath);
                }
                catch (Exception ex) when (!throwOnError)
                {
                    _logger.LogWarning(ex, "Failed to download file \"{0}\".", remoteFileName);
                    continue;
                }

                localFilePaths.Add(localPath);
            }

            return localFilePaths;
        }

        private ValueTask<BlobContainerClient> GetBlobContainerClientAsync()
        {
            var blobServiceClient = new BlobServiceClient(_storageConfiguration.GenerateStorageConnectionString());

            var client = _storageConfiguration.CreateBlobContainerClient(_blobContainerName);
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
                var containerClientResponse = await blobServiceClient.CreateBlobContainerAsync(_blobContainerName);
                client = containerClientResponse.Value;
            }

            _containerChecked = true;
            return client;
        }

        /// <summary>
        /// Downloads the remote file identified by the <paramref name="blobClient"/> and saves it to the <paramref name="localFilePath"/>.
        /// </summary>
        public static async Task DownloadAsync(BlobClient blobClient, string localFilePath)
        {
            var download = await blobClient.DownloadAsync();
            await using var stream = File.OpenWrite(localFilePath);
            await download.Value.Content.CopyToAsync(stream);
            stream.Close();
        }
    }
}
