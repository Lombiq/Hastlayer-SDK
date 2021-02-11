﻿using Hast.Vitis.Abstractions.Models;
using RestEase;
using System.Threading.Tasks;

namespace Hast.Vitis.Abstractions.Services
{
    /// <summary>
    /// Web API for the attestation service required to run xclbin on Azure servers.
    /// </summary>
    public interface IAzureAttestationApi
    {
        /// <summary>
        /// Submits the xclbin file (in blob storage) for attestation.
        /// </summary>
        [Post("{path}")]
        Task<AzureStartResponseData> Start([Path(UrlEncode = false)] string path, [Body] AzureStartPostData data);

        /// <summary>
        /// Gets the status of the request launched via <see cref="Start"/>.
        /// </summary>
        [Post("{path}")]
        Task<AzurePollResponseData> Poll([Path(UrlEncode = false)] string path, [Body] AzurePollPostData data);
    }
}
