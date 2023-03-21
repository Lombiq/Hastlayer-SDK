using Hast.Vitis.Models;
using Microsoft.Extensions.Logging;

namespace Hast.Vitis.Services;

public class AzureStorageServiceFactory : IAzureStorageServiceFactory
{
    private readonly ILogger<AzureStorageService> _logger;

    public AzureStorageServiceFactory(ILogger<AzureStorageService> logger) => _logger = logger;

    public IAzureStorageService CreateForBlob(AzureStorageConfiguration storageConfiguration, string blobName) =>
        new AzureStorageService(_logger, storageConfiguration, blobName);
}
