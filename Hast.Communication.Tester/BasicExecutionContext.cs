using Hast.Layer;
using System.Collections.Generic;
using System.Reflection;

namespace Hast.Communication.Tester
{
    public class BasicExecutionContext : Models.IHardwareExecutionContext
    {
        public IProxyGenerationConfiguration ProxyGenerationConfiguration { get; set; }

        public IHardwareRepresentation HardwareRepresentation { get; set; }

        public BasicExecutionContext(IHastlayer hastlayer,
            string deviceName,
            string communicationChannelName,
            Dictionary<string, object> customConfiguration = null)
        {
            var assemblies = new[] { Assembly.GetExecutingAssembly() };
            var configuration = new HardwareGenerationConfiguration(deviceName, null)
            {
                EnableHardwareTransformation = false,
            };
            HardwareRepresentation = hastlayer.GenerateHardwareAsync(assemblies, configuration).Result;

            ProxyGenerationConfiguration = new ProxyGenerationConfiguration()
            {
                CommunicationChannelName = communicationChannelName,
                CustomConfiguration = customConfiguration ?? new Dictionary<string, object>(),
                VerifyHardwareResults = false
            };
        }
    }
}
