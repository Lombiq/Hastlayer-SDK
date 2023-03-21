using Hast.Transformer.Configuration;

namespace Hast.Transformer.Models;

public static class TransformationContextExtensions
{
    public static TransformerConfiguration GetTransformerConfiguration(this ITransformationContext transformationContext) =>
        transformationContext.HardwareGenerationConfiguration.TransformerConfiguration();
}
