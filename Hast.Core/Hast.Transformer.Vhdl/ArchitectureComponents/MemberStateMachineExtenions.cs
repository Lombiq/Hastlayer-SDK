using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.VhdlBuilder.Representation.Expression;
using System;

namespace Hast.Transformer.Vhdl.ArchitectureComponents;

public static class MemberStateMachineExtenions
{
    public static IVhdlElement ChangeToStartState(this IMemberStateMachine stateMachine) => stateMachine.CreateStateChange(0);

    public static IVhdlElement ChangeToFinalState(this IMemberStateMachine stateMachine) => stateMachine.CreateStateChange(1);

    /// <summary>
    /// Implements a change from the current state to the state with the given index in VHDL.
    /// </summary>
    /// <param name="destinationStateIndex">The index of the state to change to.</param>
    /// <returns>The state change implemented in VHDL.</returns>
    public static IVhdlElement CreateStateChange(this IMemberStateMachine stateMachine, int destinationStateIndex) => new Assignment
    {
        AssignTo = stateMachine.CreateStateVariableName().ToVhdlVariableReference(),
        Expression = stateMachine.CreateStateName(destinationStateIndex).ToVhdlIdValue(),
    };

    /// <summary>
    /// Adds a state change to the current block if the current block wouldn't fit into one clock cycle with the given
    /// operation.
    /// </summary>
    public static int AddNewStateAndChangeCurrentBlockIfOverOneClockCycle(
        this IMemberStateMachine stateMachine,
        SubTransformerContext context,
        decimal clockCyclesNeededForNewOperation)
    {
        var currentBlock = context.Scope.CurrentBlock;

        // No state change needed.
        if (currentBlock.RequiredClockCycles + clockCyclesNeededForNewOperation <= 1)
        {
            return currentBlock.StateMachineStateIndex;
        }

        var nextStateBlock = new InlineBlock(new LineComment(
            "This state was added because the previous state would go over one clock cycle with any more operations."));
        return stateMachine.AddNewStateAndChangeCurrentBlock(context, nextStateBlock);
    }

    public static int AddNewStateAndChangeCurrentBlock(
        this IMemberStateMachine stateMachine,
        SubTransformerContext context,
        IBlockElement newBlock = null) => stateMachine.AddNewStateAndChangeCurrentBlock(context.Scope, newBlock);

    public static int AddNewStateAndChangeCurrentBlock(
        this IMemberStateMachine stateMachine,
        SubTransformerScope scope,
        IBlockElement newBlock = null)
    {
        newBlock ??= new InlineBlock();
        var newStateIndex = scope.StateMachine.AddState(newBlock);
        scope.CurrentBlock.Add(scope.StateMachine.CreateStateChange(newStateIndex));
        scope.CurrentBlock.ChangeBlockToDifferentState(newBlock, newStateIndex);
        return newStateIndex;
    }

    /// <summary>
    /// Generates the name for the state with the given index.
    /// </summary>
    /// <param name="index">The index of the state.</param>
    /// <returns>The name of the state.</returns>
    public static string CreateStateName(this IMemberStateMachine stateMachine, int index) =>
        // This doesn't need a static helper method because we deliberately don't want to generate state names for other
        // state machines, since we don't want to directly set other state machines' states.
        ArchitectureComponentNameHelper.CreatePrefixedObjectName(
            stateMachine.Name,
            "_State_" + index.ToTechnicalString());

    public static string CreateStateVariableName(this IMemberStateMachine stateMachine) =>
        ArchitectureComponentNameHelper.CreatePrefixedObjectName(stateMachine.Name, "_State");

    public static string CreateInvocationIndexVariableName(this IMemberStateMachine stateMachine, string targetMethodName) =>
        stateMachine.CreatePrefixedSegmentedObjectName(targetMethodName, "invocationIndex");
}
