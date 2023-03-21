using Hast.Common.Interfaces;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation.Declaration;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.SubTransformers;

/// <summary>
/// Gathers the array types available in the transformed code.
/// </summary>
public interface IArrayTypesCreator : IDependency
{
    /// <summary>
    /// Returns a collection of <see cref="ArrayType"/>s found in <paramref name="syntaxTree"/>.
    /// </summary>
    IEnumerable<ArrayType> CreateArrayTypes(SyntaxTree syntaxTree, IVhdlTransformationContext context);
}
