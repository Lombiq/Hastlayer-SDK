using Hast.Layer;
using Hast.Synthesis;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Transformer.Models;

public class TransformationContext : ITransformationContext
{
    public string Id { get; set; }
    public SyntaxTree SyntaxTree { get; set; }
    public IHardwareGenerationConfiguration HardwareGenerationConfiguration { get; set; }
    public ITypeDeclarationLookupTable TypeDeclarationLookupTable { get; set; }
    public IKnownTypeLookupTable KnownTypeLookupTable { get; set; }
    public IArraySizeHolder ArraySizeHolder { get; set; }
    public IDeviceDriver DeviceDriver { get; set; }

    public TransformationContext(ITransformationContext previousContext)
        : this()
    {
        Id = previousContext.Id;
        SyntaxTree = previousContext.SyntaxTree;
        HardwareGenerationConfiguration = previousContext.HardwareGenerationConfiguration;
        TypeDeclarationLookupTable = previousContext.TypeDeclarationLookupTable;
        KnownTypeLookupTable = previousContext.KnownTypeLookupTable;
        ArraySizeHolder = previousContext.ArraySizeHolder;
        DeviceDriver = previousContext.DeviceDriver;
    }

    public TransformationContext()
    {
    }
}
