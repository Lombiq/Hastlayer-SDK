using Hast.Layer;
using Hast.Transformer.Vhdl.ArchitectureComponents;
using Hast.Transformer.Vhdl.Models;
using Hast.Transformer.Vhdl.SubTransformers.ExpressionTransformers;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.VhdlBuilder.Representation.Expression;

namespace Hast.Xilinx.Services;

/// <summary>
/// Handles transforming multi-cycle operations for drivers that use the Vivado toolchain.
/// </summary>
/// <remarks>
/// <para>We need to add an attribute like the one below so Vivado won't merge this variable/signal with others, thus
/// allowing us to create XDC timing constraints for it.</para>
/// <code>attribute dont_touch of \PrimeCalculator::ArePrimeNumbers(SimpleMemory).0.binaryOperationResult.4\ : variable is "true";</code>
/// </remarks>
public class VivadoIBinaryOperatorExpressionTransformerMultiCycleHandler
    : IBinaryOperatorExpressionTransformerMultiCycleHandler
{
    public void Handle(
        SubTransformerContext context,
        IMemberStateMachine stateMachine,
        bool operationResultDataObjectIsVariable,
        IDataObject operationResultDataObjectReference)
    {
        if (!context.TransformationContext.DeviceDriver.DeviceManifest.UsesVivadoInToolChain())
        {
            return;
        }

        var attributes = operationResultDataObjectIsVariable
            ? stateMachine.LocalAttributeSpecifications
            : stateMachine.GlobalAttributeSpecifications;
        attributes.Add(new AttributeSpecification
        {
            Attribute = KnownDataTypes.DontTouchAttribute,
            Expression = new Value { DataType = KnownDataTypes.UnrangedString, Content = "true" },
            ItemClass = operationResultDataObjectReference.DataObjectKind.ToString(),
            Of = operationResultDataObjectReference,
        });
    }
}
