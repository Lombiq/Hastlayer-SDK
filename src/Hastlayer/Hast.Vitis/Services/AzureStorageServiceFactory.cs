using Hast.Vitis.Models;
using Microsoft.Extensions.Logging;

namespace Hast.Vitis.Services;

public class AzureStorageServiceFactory : IAzureStorageServiceFactory
{
#pragma warning disable S6672 // Logger is intentionally typed to AzureStorageService for its logging category.
    private readonly ILogger<AzureStorageService> _logger;

    public AzureStorageServiceFactory(ILogger<AzureStorageService> logger) => _logger = logger;
#pragma warning restore S6672

    public IAzureStorageService CreateForBlob(AzureStorageConfiguration storageConfiguration, string blobName) =>
        new AzureStorageService(_logger, storageConfiguration, blobName);
}
