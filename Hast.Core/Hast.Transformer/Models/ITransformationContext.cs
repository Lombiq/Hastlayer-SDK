using Hast.Layer;
using Hast.Synthesis;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Transformer.Models;

/// <summary>
/// The full context of a hardware transformation, including the syntax tree to transform.
/// </summary>
public interface ITransformationContext
{
    /// <summary>
    /// Gets the hash string suitable to identify the given transformation.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the syntax tree of the code to transform.
    /// </summary>
    SyntaxTree SyntaxTree { get; }

    /// <summary>
    /// Gets the configuration for how the hardware generation should happen.
    /// </summary>
    IHardwareGenerationConfiguration HardwareGenerationConfiguration { get; }

    /// <summary>
    /// Gets the table to look up type declarations in the syntax tree.
    /// </summary>
    ITypeDeclarationLookupTable TypeDeclarationLookupTable { get; }

    /// <summary>
    /// Gets the table to look up known types.
    /// </summary>
    IKnownTypeLookupTable KnownTypeLookupTable { get; }

    /// <summary>
    /// Gets the container for the sizes of statically sized arrays.
    /// </summary>
    IArraySizeHolder ArraySizeHolder { get; }

    /// <summary>
    /// Gets the driver of the currently targeted hardware device.
    /// </summary>
    IDeviceDriver DeviceDriver { get; }
}
