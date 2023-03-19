using Hast.Layer;
using Hast.Synthesis.Attributes;
using System.Collections.Generic;

namespace Hast.Transformer.Configuration;

public static class HardwareGenerationConfigurationTransformerExtensions
{
    public static TransformerConfiguration TransformerConfiguration(this IHardwareGenerationConfiguration hardwareConfiguration) =>
        hardwareConfiguration.GetOrAddCustomConfiguration<TransformerConfiguration>("Hast.Transformer.Configuration");

    public static IDictionary<string, object> GetOrAddReplacements(this IHardwareGenerationConfiguration hardwareConfiguration) =>
        hardwareConfiguration.GetOrAddCustomConfiguration<Dictionary<string, object>>(ReplaceableAttribute.Name);
}
