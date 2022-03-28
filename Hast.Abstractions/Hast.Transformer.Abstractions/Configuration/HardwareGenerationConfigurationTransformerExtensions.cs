using Hast.Synthesis.Abstractions;
using Hast.Transformer.Abstractions.Configuration;
using System.Collections.Generic;

namespace Hast.Layer;

public static class HardwareGenerationConfigurationTransformerExtensions
{
    public static TransformerConfiguration TransformerConfiguration(this IHardwareGenerationConfiguration hardwareConfiguration) =>
        hardwareConfiguration.GetOrAddCustomConfiguration<TransformerConfiguration>("Hast.Transformer.Configuration");

    public static IDictionary<string, object> GetOrAddReplacements(this IHardwareGenerationConfiguration hardwareConfiguration) =>
        hardwareConfiguration.GetOrAddCustomConfiguration<Dictionary<string, object>>(ReplaceableAttribute.Name);
}
