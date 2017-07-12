using Hast.Remote.Configuration;

namespace Hast.Layer
{
    public static class RemoteClientConfigurationTransformerExtensions
    {
        public static RemoteClientConfiguration RemoteClientConfiguration(this IHardwareGenerationConfiguration hardwareConfiguration)
        {
            return hardwareConfiguration.GetOrAddCustomConfiguration<RemoteClientConfiguration>("Hast.Remote.Client.Configuration");
        }
    }
}
