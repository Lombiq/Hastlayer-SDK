using Hast.Common.Interfaces;
using Hast.Transformer.Vhdl.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hast.Transformer.Vhdl.SubTransformers;

/// <summary>
/// Handles the transformation of members via sub-transformers (eg. <see cref="IMethodTransformer"/>, <see
/// cref="IDisplayClassFieldTransformer"/>, <see cref="IPocoTransformer"/>).
/// </summary>
public interface IMemberTransformer : IDependency
{
    /// <summary>
    /// Returns a collection of transformation tasks added via the sub-transformers.
    /// </summary>
    IEnumerable<Task<IMemberTransformerResult>> TransformMembers(
        AstNode node,
        VhdlTransformationContext transformationContext,
        ICollection<Task<IMemberTransformerResult>> memberTransformerTasks = null);
}
