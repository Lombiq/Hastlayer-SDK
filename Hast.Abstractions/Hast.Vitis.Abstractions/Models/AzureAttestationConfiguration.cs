using System;

namespace Hast.Vitis.Abstractions.Models
{
    public class AzureAttestationConfiguration
    {
        /// <summary>
        /// Gets or sets the Azure function URL for blob attestation submission.
        /// </summary>
        public Uri StartFunctionUrl { get; set; }

        /// <summary>
        /// Gets or sets the Azure function URL for polling the status of the attestation process.
        /// </summary>
        public Uri PollFunctionUrl { get; set; }

        /// <summary>
        /// Gets or sets the storage account name.
        /// </summary>
        public string StorageAccountName { get; set; }

        /// <summary>
        /// Gets or sets the container name for the storage account name.
        /// </summary>
        public string Container { get; set; }

        /// <summary>
        /// Gets or sets the netlist name to be validated.
        /// </summary>
        public string NetlistName { get; set; }

        /// <summary>
        /// Gets or sets the SAS token for the blob container.
        /// </summary>
        public string BlobContainerSas { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user hosting the storage blob.
        /// </summary>
        public string ClientSubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the tenant hosting the storage blob.
        /// </summary>
        public string ClientTenantId { get; set; }
    }
}
