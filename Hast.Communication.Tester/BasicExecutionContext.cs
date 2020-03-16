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
            HardwareRepresentation = hastlayer.GenerateHardware(new Assembly[] { Assembly.GetExecutingAssembly() },
                new HardwareGenerationConfiguration(deviceName, null)).Result;
            ProxyGenerationConfiguration = new ProxyGenerationConfiguration()
            {
                CommunicationChannelName = communicationChannelName,
                CustomConfiguration = customConfiguration ?? new Dictionary<string, object>(),
                VerifyHardwareResults = false
            };
        }
    }
}
