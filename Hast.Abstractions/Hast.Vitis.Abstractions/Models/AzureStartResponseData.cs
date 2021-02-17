using Newtonsoft.Json;

namespace Hast.Vitis.Abstractions.Models
{
    public class AzureStartResponseData : AzureResponseData
    {
        public void Deconstruct(out string instanceId, out string errorMessage)
        {
            instanceId = InstanceId;
            errorMessage = ErrorMessage;
        }
    }
}
