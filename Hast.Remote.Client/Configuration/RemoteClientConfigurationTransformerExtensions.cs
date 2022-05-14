using Hast.Remote.Client.Constants;
using Hast.Remote.Configuration;

namespace Hast.Layer;

public static class RemoteClientConfigurationTransformerExtensions
{
    public static RemoteClientConfiguration RemoteClientConfiguration(
        this IHardwareGenerationConfiguration hardwareConfiguration) =>
        hardwareConfiguration.GetOrAddCustomConfiguration<RemoteClientConfiguration>(
            CustomConfigurationKeys.HastRemoteClientConfiguration);
}
