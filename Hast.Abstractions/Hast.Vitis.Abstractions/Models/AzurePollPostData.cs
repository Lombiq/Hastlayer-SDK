namespace Hast.Vitis.Abstractions.Models
{
    public class AzurePollPostData : AzurePostData
    {
        public string OrchestrationId { get; set; }

        public AzurePollPostData(AzureAttestationConfiguration configuration = null, string orchestrationId = null)
            : base(configuration) =>
            OrchestrationId = orchestrationId;
    }
}
