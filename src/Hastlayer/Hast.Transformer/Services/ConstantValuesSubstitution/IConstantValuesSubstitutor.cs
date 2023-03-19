using Hast.Common.Interfaces;
using Hast.Layer;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Transformer.Services.ConstantValuesSubstitution;

/// <summary>
/// Substitutes variables, fields, etc. with constants if they can only ever have a compile-time defined value.
/// </summary>
public interface IConstantValuesSubstitutor : IDependency
{
    /// <summary>
    /// Substitutes variables, fields, etc. with constants if they can only ever have a compile-time defined value.
    /// </summary>
    void SubstituteConstantValues(
        SyntaxTree syntaxTree,
        IArraySizeHolder arraySizeHolder,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable);
}
