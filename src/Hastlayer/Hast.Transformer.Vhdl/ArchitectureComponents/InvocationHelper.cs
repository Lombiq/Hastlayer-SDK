using Hast.Transformer.Vhdl.Helpers;
using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.VhdlBuilder.Representation.Expression;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Vhdl.ArchitectureComponents;

internal static class InvocationHelper
{
    public static IVhdlElement CreateInvocationStart(
        IArchitectureComponent component,
        string targetStateMachineName,
        int targetIndex)
    {
        var startedSignalReference = CreateStartedSignalReference(component, targetStateMachineName, targetIndex);

        component.InternallyDrivenSignals.AddIfNew(new Signal
        {
            DataType = KnownDataTypes.Boolean,
            Name = startedSignalReference.Name,
            InitialValue = Value.False,
        });

        // Set the start signal for the state machine.
        return new Assignment
        {
            AssignTo = startedSignalReference,
            Expression = Value.True,
        };
    }

    public static IfElse<IBlockElement> CreateWaitForInvocationFinished(
        IArchitectureComponent component,
        string targetStateMachineName,
        int degreeOfParallelism,
        bool waitForAll = true)
    {
        // Iteratively building a binary expression chain to OR or AND together all (Started = Finished) expressions.
        // Using (Started = Finished) so it will work even if not all state available state machines were started.

        var allInvokedStateMachinesFinishedIfElseTrue = new InlineBlock();

        for (int i = 0; i < degreeOfParallelism; i++)
        {
            component.ExternallyDrivenSignals.AddIfNew(new Signal
            {
                DataType = KnownDataTypes.Boolean,
                Name = CreateFinishedSignalReference(component, targetStateMachineName, i).Name,
                InitialValue = Value.False,
            });

            // Reset the start signal in the finished block.
            allInvokedStateMachinesFinishedIfElseTrue.Add(new Assignment
            {
                AssignTo = CreateStartedSignalReference(component, targetStateMachineName, i),
                Expression = Value.False,
            });
        }

        IVhdlElement CreateStartedEqualsFinishedBinary(int index) =>
            new Binary
            {
                Left = CreateStartedSignalReference(component, targetStateMachineName, index),
                Operator = BinaryOperator.Equality,
                Right = CreateFinishedSignalReference(component, targetStateMachineName, index),
            };

        return new IfElse<IBlockElement>
        {
            Condition = BinaryChainBuilder.BuildBinaryChain(
                Enumerable.Range(0, degreeOfParallelism).Select(CreateStartedEqualsFinishedBinary),
                waitForAll ? BinaryOperator.And : BinaryOperator.Or),
            True = allInvokedStateMachinesFinishedIfElseTrue,
        };
    }

    public static DataObjectReference CreateStartedSignalReference(
        IArchitectureComponent component,
        string targetStateMachineName,
        int index) => CreateStartedSignalReference(component.Name, targetStateMachineName, index);

    public static DataObjectReference CreateStartedSignalReference(
        string componentName,
        string targetStateMachineName,
        int index) => CreateSignalReference(componentName, targetStateMachineName, NameSuffixes.Started, index);

    public static DataObjectReference CreateFinishedSignalReference(
        IArchitectureComponent component,
        string targetStateMachineName,
        int index) => CreateFinishedSignalReference(component.Name, targetStateMachineName, index);

    public static DataObjectReference CreateFinishedSignalReference(
        string componentName,
        string targetStateMachineName,
        int index) => CreateSignalReference(componentName, targetStateMachineName, NameSuffixes.Finished, index);

    private static DataObjectReference CreateSignalReference(
        string componentName,
        string targetStateMachineName,
        string signalName,
        int index) => ArchitectureComponentNameHelper
            .CreatePrefixedSegmentedObjectName(componentName, targetStateMachineName, signalName, index.ToTechnicalString())
            .ToVhdlSignalReference();
}
