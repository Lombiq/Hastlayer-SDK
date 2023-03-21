using Hast.VhdlBuilder.Representation.Declaration;
using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.ArchitectureComponents;

/// <summary>
/// The current state of the <see cref="IMemberStateMachine"/>.
/// </summary>
public interface IMemberStateMachineState
{
    /// <summary>
    /// Gets the block body of the member the state machine represents.
    /// </summary>
    IBlockElement Body { get; }

    /// <summary>
    /// Gets or sets the clock cycles required to invoke the member the state machine represents.
    /// </summary>
    decimal RequiredClockCycles { get; set; }
}

/// <summary>
/// A state machine generated from a .NET member.
/// </summary>
public interface IMemberStateMachine : IArchitectureComponent
{
    /// <summary>
    /// Gets the states of the state machine. The state with the index 0 is the start state, the one with the index 1 is
    /// the final state.
    /// </summary>
    IReadOnlyList<IMemberStateMachineState> States { get; }

    /// <summary>
    /// Adds a new state to the state machine.
    /// </summary>
    /// <param name="state">The state's VHDL element.</param>
    /// <returns>The index of the state.</returns>
    int AddState(IBlockElement state);

    /// <summary>
    /// Makes note of a new multi-cycle operations. See <see cref="IArchitectureComponent.MultiCycleOperations"/>.
    /// </summary>
    /// <param name="operationResultReference">Reference to the result data object of the operation.</param>
    /// <param name="requiredClockCyclesCeiling">The clock cycles needed to complete the operation, rounded up.</param>
    void RecordMultiCycleOperation(IDataObject operationResultReference, int requiredClockCyclesCeiling);
}
