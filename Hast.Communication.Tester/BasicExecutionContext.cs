using Hast.Layer;
using System.Collections.Generic;

namespace Hast.Communication.Tester;

public class BasicExecutionContext : Models.IHardwareExecutionContext
{
    public IProxyGenerationConfiguration ProxyGenerationConfiguration { get; set; }

    public IHardwareRepresentation HardwareRepresentation { get; set; }

    public BasicExecutionContext(
        IHastlayer hastlayer,
        string deviceName,
        string communicationChannelName,
        Dictionary<string, object> customConfiguration = null)
    {
        var assemblies = new[] { typeof(Program).Assembly };
        var configuration = new HardwareGenerationConfiguration(deviceName, hardwareFrameworkPath: null)
        {
            EnableHardwareTransformation = false,
        };

        // This runs synchronously anyway.
#pragma warning disable VSTHRD104 // Offer async methods
        HardwareRepresentation = hastlayer.GenerateHardwareAsync(assemblies, configuration).Result;
#pragma warning restore VSTHRD104 // Offer async methods

        ProxyGenerationConfiguration = new ProxyGenerationConfiguration
        {
            CommunicationChannelName = communicationChannelName,
            VerifyHardwareResults = false,
        };

        if (customConfiguration != null)
        {
            foreach (var (key, value) in customConfiguration)
            {
                ProxyGenerationConfiguration.CustomConfiguration[key] = value;
            }
        }
    }
}
