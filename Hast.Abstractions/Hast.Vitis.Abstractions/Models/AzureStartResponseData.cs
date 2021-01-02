using Newtonsoft.Json;

namespace Hast.Vitis.Abstractions.Models
{
    public class AzureStartResponseData
    {
        // The C# name is the same passed into AzurePollPostData.
        [JsonProperty("instanceId")]
        public string OrchestrationId { get; set; }

        public string ErrorMessage { get; set; }

        public void Deconstruct(out string orchestrationId, out string errorMessage)
        {
            orchestrationId = OrchestrationId;
            errorMessage = ErrorMessage;
        }
    }
}
