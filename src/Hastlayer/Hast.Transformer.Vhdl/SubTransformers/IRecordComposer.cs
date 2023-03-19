using Hast.Common.Interfaces;
using Hast.Transformer.Vhdl.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Transformer.Vhdl.SubTransformers;

/// <summary>
/// A service for creating VHDL records from <see cref="PropertyDeclaration"/> and <see cref="FieldDeclaration"/> nodes.
/// </summary>
public interface IRecordComposer : IDependency, ISpecificNodeTypeTransformer
{
    /// <summary>
    /// Transforms a <paramref name="typeDeclaration"/> into a VHDL record.
    /// </summary>
    NullableRecord CreateRecordFromType(TypeDeclaration typeDeclaration, IVhdlTransformationContext context);
}
