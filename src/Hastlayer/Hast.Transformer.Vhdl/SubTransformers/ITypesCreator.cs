using Hast.Common.Interfaces;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation.Declaration;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.SubTransformers;

/// <summary>
/// Retrieves missing dependent types and adds them to the architecture.
/// </summary>
public interface ITypesCreator : IDependency
{
    /// <summary>
    /// Retrieves missing dependent types in <paramref name="syntaxTree"/> and adds them to the <paramref
    /// name="dependentTypesTables"/> and <paramref name="hastIpArchitecture"/>.
    /// </summary>
    void CreateTypes(
        SyntaxTree syntaxTree,
        VhdlTransformationContext vhdlTransformationContext,
        ICollection<DependentTypesTable> dependentTypesTables,
        Architecture hastIpArchitecture);
}
