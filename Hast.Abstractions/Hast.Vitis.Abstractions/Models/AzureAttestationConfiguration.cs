using Hast.Layer;
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
        /// Gets or sets the storage account name. See <see href="https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet#copy-your-credentials-from-the-azure-portal">
        /// here</see> for instructions.
        /// </summary>
        public string StorageAccountName { get; set; }

        /// <summary>
        /// Gets or sets the key to access the account of <see cref="StorageAccountName"/>.
        /// </summary>
        public string StorageAccountKey { get; set; }

        /// <summary>
        /// Gets or sets the container name for the storage account name.
        /// </summary>
        public string Container { get; set; }

        /// <summary>
        /// Gets or sets the netlist name to be validated.
        /// </summary>
        public string NetlistName { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user hosting the storage blob.
        /// </summary>
        public string ClientSubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the tenant hosting the storage blob.
        /// </summary>
        public string ClientTenantId { get; set; }

        public string GenerateStorageConnectionString() =>
            string.Join(
                separator: ";",
                "DefaultEndpointsProtocol=https",
                $"AccountName={StorageAccountName}",
                $"AccountKey={StorageAccountKey}",
                "EndpointSuffix=core.windows.net");

        public void SetupAndVerify()
        {
            VerifyConfiguration();
            Environment.SetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING", GenerateStorageConnectionString());
        }

        private void VerifyConfiguration()
        {
            if (StartFunctionUrl == null) ThrowMissing(nameof(StartFunctionUrl));
            if (PollFunctionUrl == null) ThrowMissing(nameof(PollFunctionUrl));
            if (string.IsNullOrEmpty(StorageAccountName)) ThrowMissing(nameof(StorageAccountName));
            if (string.IsNullOrEmpty(Container)) ThrowMissing(nameof(Container));
            if (string.IsNullOrEmpty(NetlistName)) ThrowMissing(nameof(NetlistName));
            if (string.IsNullOrEmpty(ClientTenantId)) ThrowMissing(nameof(ClientTenantId));
            if (string.IsNullOrEmpty(ClientSubscriptionId)) ThrowMissing(nameof(ClientSubscriptionId));
        }

        private static void ThrowMissing(string name) =>
            throw new InvalidOperationException(
                $"The property '{name}' is missing or empty. It is required to deal with the attestation service. " +
                $"Please specify it in the appsettings.json or otherwise set the " +
                $"{nameof(IHardwareGenerationConfiguration)}. See the readme of the Hast.Vitis.Abstractions library " +
                $"for further details.");
    }
}
