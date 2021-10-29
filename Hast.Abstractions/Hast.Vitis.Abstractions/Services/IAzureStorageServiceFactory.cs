using Hast.Common.Interfaces;
using Hast.Vitis.Abstractions.Models;

namespace Hast.Vitis.Abstractions.Services
{
    /// <summary>
    /// A factory service for <see cref="AzureStorageService"/>.
    /// </summary>
    public interface IAzureStorageServiceFactory : IDependency
    {
        /// <summary>
        /// Returns a service that implements <see cref="IAzureStorageService"/> for the given blob with the specified
        /// connection configuration.
        /// </summary>
        IAzureStorageService CreateForBlob(AzureStorageConfiguration storageConfiguration, string blobName);
    }
}
