using Hast.Common.Interfaces;
using Hast.VhdlBuilder.Representation;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.SubTransformers;

/// <summary>
/// Retrieves the enum types available in the source code.
/// </summary>
public interface IEnumTypesCreator : IDependency
{
    /// <summary>
    /// Returns a collection of enum types found in the <paramref name="syntaxTree"/>.
    /// </summary>
    IEnumerable<IVhdlElement> CreateEnumTypes(SyntaxTree syntaxTree);
}
