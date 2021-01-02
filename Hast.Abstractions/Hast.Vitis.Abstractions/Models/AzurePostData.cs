namespace Hast.Vitis.Abstractions.Models
{
    public abstract class AzurePostData
    {
        public string ClientSubscriptionId { get; set; }
        public string ClientTenantId { get; set; }

        protected AzurePostData(AzureAttestationConfiguration configuration)
        {
            if (configuration == null) return;

            ClientSubscriptionId = configuration.ClientSubscriptionId;
            ClientTenantId = configuration.ClientTenantId;
        }
    }
}
