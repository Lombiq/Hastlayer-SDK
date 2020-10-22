using Hast.Common.Services;
using Hast.Layer;
using Hast.Vitis.Abstractions.Models;

namespace Hast.Vitis.Abstractions.Extensions
{
    public static class ConfigurationExtensions
    {
        public static IOpenClConfiguration GetOrAddOpenClConfiguration(this IHardwareGenerationConfiguration configuration) =>
            configuration.GetOrAddCustomConfiguration<OpenClConfiguration>(nameof(OpenClConfiguration));

        public static VitisBuildConfiguration GetOrAddVitisBuildConfiguration(this IHardwareGenerationConfiguration configuration) =>
            configuration.GetOrAddCustomConfiguration<VitisBuildConfiguration>(nameof(VitisBuildConfiguration));
    }
}
