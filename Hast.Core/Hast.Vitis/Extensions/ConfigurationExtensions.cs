using Hast.Layer;
using Hast.Vitis.Models;

namespace Hast.Vitis.Extensions;

public static class ConfigurationExtensions
{
    public static IOpenClConfiguration GetOrAddOpenClConfiguration(this IHardwareGenerationConfiguration configuration) =>
        configuration.GetOrAddCustomConfiguration<OpenClConfiguration>(nameof(OpenClConfiguration));

    public static VitisBuildConfiguration GetOrAddVitisBuildConfiguration(this IHardwareGenerationConfiguration configuration) =>
        configuration.GetOrAddCustomConfiguration<VitisBuildConfiguration>(nameof(VitisBuildConfiguration));

    public static AzureAttestationConfiguration GetOrAddAzureAttestationConfiguration(this IHardwareGenerationConfiguration configuration) =>
        configuration.GetOrAddCustomConfiguration<AzureAttestationConfiguration>(nameof(AzureAttestationConfiguration));
}
