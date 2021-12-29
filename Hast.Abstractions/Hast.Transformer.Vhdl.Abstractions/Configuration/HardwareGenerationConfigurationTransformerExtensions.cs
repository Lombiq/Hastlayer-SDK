using Hast.Transformer.Vhdl.Abstractions.Configuration;

namespace Hast.Layer
{
    public static class HardwareGenerationConfigurationTransformerExtensions
    {
        public static VhdlTransformerConfiguration VhdlTransformerConfiguration(this IHardwareGenerationConfiguration hardwareConfiguration) =>
            hardwareConfiguration.GetOrAddCustomConfiguration<VhdlTransformerConfiguration>("Hast.Transformer.Vhdl.Configuration");
    }
}
