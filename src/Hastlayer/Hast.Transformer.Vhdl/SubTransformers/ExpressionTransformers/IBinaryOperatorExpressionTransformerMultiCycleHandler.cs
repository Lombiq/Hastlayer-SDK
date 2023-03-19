using Hast.Common.Interfaces;
using Hast.Transformer.Vhdl.ArchitectureComponents;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation.Declaration;

namespace Hast.Transformer.Vhdl.SubTransformers.ExpressionTransformers;

/// <summary>
/// An extension point for handling multi-cycle operations in  <see
/// cref="BinaryOperatorExpressionTransformer.TransformBinaryOperatorExpression"/>.
/// </summary>
public interface IBinaryOperatorExpressionTransformerMultiCycleHandler : IDependency
{
    /// <summary>
    /// Performs device-specific actions to handle multi-cycle operations, e.g. based on the <see
    /// cref="SubTransformerContext.TransformationContext"/>.
    /// </summary>
    void Handle(
        SubTransformerContext context,
        IMemberStateMachine stateMachine,
        bool operationResultDataObjectIsVariable,
        IDataObject operationResultDataObjectReference);
}
