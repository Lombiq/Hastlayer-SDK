using Hast.Common.Services;
using Hast.Layer;
using Hast.Vitis.Abstractions.Models;

namespace Hast.Vitis.Abstractions.Extensions
{
    public static class ConfigurationExtensions
    {
        public static IOpenClConfiguration GetOrAddOpenClConfiguration(this IHardwareGenerationConfigurationHolder holder) =>
            holder.Configuration.GetOrAddCustomConfiguration<OpenClConfiguration>(nameof(OpenClConfiguration));
    }
}
