using Hast.Common.Interfaces;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation;
using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.SubTransformers.ExpressionTransformers;

/// <summary>
/// A transformer specifically for binary operator expressions.
/// </summary>
public interface IBinaryOperatorExpressionTransformer : IDependency
{
    /// <summary>
    /// Transforms binary operator expressions that can be executed in parallel, with operation-level (SIMD-like)
    /// parallelism.
    /// </summary>
    IEnumerable<IVhdlElement> TransformParallelBinaryOperatorExpressions(
          IEnumerable<PartiallyTransformedBinaryOperatorExpression> partiallyTransformedExpressions,
          SubTransformerContext context);

    /// <summary>
    /// Transforms regular binary operator expressions.
    /// </summary>
    IVhdlElement TransformBinaryOperatorExpression(
        PartiallyTransformedBinaryOperatorExpression partiallyTransformedExpression,
        SubTransformerContext context);
}
