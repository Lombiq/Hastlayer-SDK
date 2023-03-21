using Hast.Transformer.Vhdl.ArchitectureComponents;
using Hast.Transformer.Vhdl.Helpers;
using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.VhdlBuilder.Representation.Expression;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Vhdl.SimpleMemory;

public class SimpleMemoryOperationProxyBuilder : ISimpleMemoryOperationProxyBuilder
{
    public IArchitectureComponent BuildProxy(IEnumerable<IArchitectureComponent> components)
    {
        var simpleMemoryUsingComponents = components.Where(component => component.AreSimpleMemorySignalsAdded());

        const string proxyComponentName = "System.Void Hast::SimpleMemoryOperationProxy()";

        if (!simpleMemoryUsingComponents.Any()) return new BasicComponent(proxyComponentName);

        var signalsAssignmentBlock = new InlineBlock();

        signalsAssignmentBlock.Add(BuildConditionalPortAssignment(
            SimpleMemoryPortNames.CellIndex,
            simpleMemoryUsingComponents,
            component => new Binary
            {
                Left = component.CreateSimpleMemoryReadEnableSignalReference(),
                Operator = BinaryOperator.Or,
                Right = component.CreateSimpleMemoryWriteEnableSignalReference(),
            }));

        signalsAssignmentBlock.Add(BuildConditionalPortAssignment(
            SimpleMemoryPortNames.DataOut,
            simpleMemoryUsingComponents,
            component => component.CreateSimpleMemoryWriteEnableSignalReference()));

        signalsAssignmentBlock.Add(BuildConditionalOrPortAssignment(
            SimpleMemoryPortNames.ReadEnable,
            simpleMemoryUsingComponents));

        signalsAssignmentBlock.Add(BuildConditionalOrPortAssignment(
            SimpleMemoryPortNames.WriteEnable,
            simpleMemoryUsingComponents));

        // So it's not cut off wrongly if names are shortened we need to use a name for this signal as it would look
        // from a generated state machine.
        return new BasicComponent(proxyComponentName)
        {
            Body = signalsAssignmentBlock,
        };
    }

    private static ConditionalSignalAssignment BuildConditionalPortAssignment(
        string portName,
        IEnumerable<IArchitectureComponent> components,
        Func<IArchitectureComponent, IVhdlElement> expressionBuilderForComponentsAssignment)
    {
        var assignment = new ConditionalSignalAssignment
        {
            AssignTo = portName.ToExtendedVhdlId().ToVhdlSignalReference(),
        };

        foreach (var component in components)
        {
            IVhdlElement value = component.CreateSimpleMemorySignalName(portName).ToVhdlIdValue();

            // Since CellIndex is an integer but all ints are handled as unsigned types internally we need to do a type
            // conversion.
            if (portName == SimpleMemoryPortNames.CellIndex)
            {
                value = Invocation.ToInteger(value);
            }

            assignment.Whens.Add(new SignalAssignmentWhen
            {
                Expression = expressionBuilderForComponentsAssignment(component),
                Value = value,
            });
        }

        assignment.Whens.Add(new SignalAssignmentWhen
        {
            Value = portName == SimpleMemoryPortNames.CellIndex ?
                KnownDataTypes.UnrangedInt.DefaultValue :
                SimpleMemoryTypes.DataSignalsDataType.DefaultValue,
        });

        return assignment;
    }

    private static Assignment BuildConditionalOrPortAssignment(
        string portName,
        IEnumerable<IArchitectureComponent> components) => new()
        {
            AssignTo = portName.ToExtendedVhdlId().ToVhdlSignalReference(),
            Expression = BinaryChainBuilder.BuildBinaryChain(
                components.Select(component => component.CreateSimpleMemorySignalReference(portName)),
                BinaryOperator.Or),
        };
}
