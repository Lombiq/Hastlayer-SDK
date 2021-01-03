using Newtonsoft.Json;

namespace Hast.Vitis.Abstractions.Models
{
    internal class AzureStartPostData : AzurePostData
    {
        public string StorageAccountName { get; set; }
        public string Container { get; set; }
        public string NetlistName { get; set; }

        [JsonProperty("BlobContainerSAS")]
        public string BlobContainerSignature { get; set; }

        public AzureStartPostData(AzureAttestationConfiguration configuration, string blobContainerSignature)
            : base(configuration)
        {
            StorageAccountName = configuration?.StorageAccountName;
            Container = configuration?.Container;
            NetlistName = configuration?.NetlistName;
            BlobContainerSignature = blobContainerSignature;
        }
    }
}
