using Azure.Storage;
using Azure.Storage.Blobs;
using System;

namespace Hast.Vitis.Models;

public class AzureStorageConfiguration
{
    /// <summary>
    /// Gets or sets the storage account name. See <see
    /// href="https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet#copy-your-credentials-from-the-azure-portal">
    /// here</see> for instructions.
    /// </summary>
    public string StorageAccountName { get; set; }

    /// <summary>
    /// Gets or sets the key to access the account of <see cref="StorageAccountName"/>.
    /// </summary>
    public string StorageAccountKey { get; set; }

    public string GenerateStorageConnectionString() =>
        string.Join(
            separator: ";",
            "DefaultEndpointsProtocol=https",
            $"AccountName={StorageAccountName}",
            $"AccountKey={StorageAccountKey}",
            "EndpointSuffix=core.windows.net");

    public BlobContainerClient CreateBlobContainerClient(string blobContainerName) =>
        new(
            new Uri($"https://{StorageAccountName}.blob.core.windows.net/{blobContainerName}"),
            new StorageSharedKeyCredential(StorageAccountName, StorageAccountKey));
}
