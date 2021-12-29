using Hast.Layer;
using System;

namespace Hast.Vitis.Abstractions.Models
{
    public class AzureAttestationConfiguration : AzureStorageConfiguration
    {
        /// <summary>
        /// Gets or sets the Azure function URL for blob attestation submission.
        /// </summary>
        public Uri StartFunctionUrl { get; set; } = new("https://fpga-attestation.azurewebsites.net/api/ComputeFPGA_HttpStart");

        /// <summary>
        /// Gets or sets the Azure function URL for polling the status of the attestation process.
        /// </summary>
        public Uri PollFunctionUrl { get; set; } = new("https://fpga-attestation.azurewebsites.net/api/ComputeFPGA_HttpGetStatus");

        /// <summary>
        ///     <para>
        ///         Gets or sets the ID of the user hosting the storage blob. You can retrieve it using:
        ///     </para>
        ///     <code>
        ///         az account show --output tsv --query id
        ///     </code>
        /// </summary>
        public string ClientSubscriptionId { get; set; }

        /// <summary>
        ///     <para>
        ///         Gets or sets the ID of the tenant hosting the storage blob. You can retrieve it using:
        ///     </para>
        ///     <code>
        ///         az account show --output tsv --query tenantId
        ///     </code>
        /// </summary>
        public string ClientTenantId { get; set; }

        public AzureAttestationConfiguration Verify()
        {
            if (StartFunctionUrl == null) ThrowMissing(nameof(StartFunctionUrl));
            if (PollFunctionUrl == null) ThrowMissing(nameof(PollFunctionUrl));
            if (string.IsNullOrEmpty(StorageAccountName)) ThrowMissing(nameof(StorageAccountName));
            if (string.IsNullOrEmpty(ClientTenantId)) ThrowMissing(nameof(ClientTenantId));
            if (string.IsNullOrEmpty(ClientSubscriptionId)) ThrowMissing(nameof(ClientSubscriptionId));

            return this;
        }

        private static void ThrowMissing(string name) =>
            throw new InvalidOperationException(
                $"The property \"{name}\" is missing or empty. It is required to deal with the attestation service. " +
                $"Please specify it in the appsettings.json or otherwise set the " +
                $"{nameof(IHardwareGenerationConfiguration)}. See the Readme of the Hast.Vitis.Abstractions library " +
                $"for further details.");
    }
}
