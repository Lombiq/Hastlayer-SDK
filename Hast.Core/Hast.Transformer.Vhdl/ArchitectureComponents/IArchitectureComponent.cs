using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.VhdlBuilder.Representation.Expression;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.ArchitectureComponents;

/// <summary>
/// Represents an operation with a known max cycle count that's more than one.
/// </summary>
public interface IMultiCycleOperation
{
    /// <summary>
    /// Gets the data object for this operation's result.
    /// </summary>
    IDataObject OperationResultReference { get; }

    /// <summary>
    /// Gets the maximum number of clock cycles this operation should take.
    /// </summary>
    int RequiredClockCyclesCeiling { get; }
}

/// <summary>
/// Represents a VHDL component <see href="https://www.ics.uci.edu/~jmoorkan/vhdlref/compinst.html"/>.
/// </summary>
public interface IArchitectureComponent
{
    /// <summary>
    /// Gets the name of the component. This is a standard name, not a VHDL identifier.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the variables local to the component.
    /// </summary>
    IList<Variable> LocalVariables { get; }

    /// <summary>
    /// Gets the aliases local to the component.
    /// </summary>
    IList<Alias> LocalAliases { get; }

    /// <summary>
    /// Gets the attribute specifications local to the component.
    /// </summary>
    IList<AttributeSpecification> LocalAttributeSpecifications { get; }

    /// <summary>
    /// Gets the variables corresponding to the component that are in the global namespace.
    /// </summary>
    IList<Variable> GlobalVariables { get; }

    /// <summary>
    /// Gets the global signals declared for the component that are driven from inside the component.
    /// </summary>
    IList<Signal> InternallyDrivenSignals { get; }

    /// <summary>
    /// Gets the global signals declared for the component that are driven from outside the component.
    /// </summary>
    IList<Signal> ExternallyDrivenSignals { get; }

    /// <summary>
    /// Gets the attribute specifications corresponding to the component that are in the global namespace.
    /// </summary>
    IList<AttributeSpecification> GlobalAttributeSpecifications { get; }

    /// <summary>
    /// Gets the track which other members are called from this component and in how many instances at a given time.
    /// I.e. if this FSM starts another FSM (which was originally e.g. a method call) then it will be visible here. If
    /// parallelization happens then the call instance count will be greater than 1 (i.e. the other member is called in
    /// more than one instance at a given time).
    /// </summary>
    IDictionary<EntityDeclaration, int> OtherMemberMaxInvocationInstanceCounts { get; }

    /// <summary>
    /// Gets the dependency relations between VHDL types, for custom types declared in this component that need this.
    /// </summary>
    DependentTypesTable DependentTypesTable { get; }

    /// <summary>
    /// Gets the operations that take multiple clock cycles and are thus awaited in their own state. This is to be used
    /// when multi-cycle paths need to be defined in the hardware design tool.
    /// </summary>
    IEnumerable<IMultiCycleOperation> MultiCycleOperations { get; }

    /// <summary>
    /// Produces the declarations corresponding to the component that should be inserted into the head of the
    /// architecture element.
    /// </summary>
    IVhdlElement BuildDeclarations();

    /// <summary>
    /// Produces the body of the component that should be inserted into the body of the architecture element.
    /// </summary>
    IVhdlElement BuildBody();
}
