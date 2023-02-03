using Hast.Common.Interfaces;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.SubTransformers;

/// <summary>
/// Transformer for dealing with special Hastlayer-supported computational operations.
/// </summary>
public interface ISpecialOperationInvocationTransformer : IDependency, ISpecificNodeTypeTransformer
{
    /// <summary>
    /// Transforms special operations such as SIMD operations into their optimized versions.
    /// </summary>
    IVhdlElement TransformSpecialOperationInvocation(
        InvocationExpression expression,
        IEnumerable<IVhdlElement> transformedParameters,
        SubTransformerContext context);
}
