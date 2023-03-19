using Hast.Layer;

namespace Hast.Transformer.Vhdl.Configuration;

public static class HardwareGenerationConfigurationTransformerExtensions
{
    public static VhdlTransformerConfiguration VhdlTransformerConfiguration(this IHardwareGenerationConfiguration hardwareConfiguration) =>
        hardwareConfiguration.GetOrAddCustomConfiguration<VhdlTransformerConfiguration>("Hast.Transformer.Vhdl.Configuration");
}
