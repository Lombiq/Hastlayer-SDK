using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hast.Vitis.Abstractions.Services
{
    public interface IAzureStorageService
    {
        /// <summary>
        /// Uploads a file to the blob container.
        /// </summary>
        Task UploadAsync(string localFilePath);

        /// <summary>
        /// Downloads a files from the remote container given the file name and the directory where to store it. Returns
        /// the full local path of the downloaded file.
        /// </summary>
        Task<IEnumerable<string>> DownloadAsync(string localDirectoryPath, params string[] remoteFileNames);

        /// <summary>
        /// Downloads a files from the remote container given the file name and the directory where to store it. If the
        /// download fails it won't throw any exception (though it logs a warning), it just returns less items in the
        /// collection. Returns the full local path of the downloaded file.
        /// </summary>
        Task<IEnumerable<string>> DownloadMaybeAsync(string localDirectoryPath, params string[] remoteFileNames);

        /// <summary>
        /// Returns the SAS query string for this blob storage.
        /// </summary>
        ValueTask<string> GetSharedAccessSignatureAsync();
    }
}
