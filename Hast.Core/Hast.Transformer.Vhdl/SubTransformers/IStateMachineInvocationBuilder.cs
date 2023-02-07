using Hast.Common.Interfaces;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Expression;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.SubTransformers;

/// <summary>
/// A service for creating the VHDL state machine for method invocations.
/// </summary>
public interface IStateMachineInvocationBuilder : IDependency
{
    /// <summary>
    /// Uses the <paramref name="context"/>'s <see cref="SubTransformerContext.Scope"/> to insert an invocation into the
    /// <see cref="CurrentBlock"/>.
    /// </summary>
    IBuildInvocationResult BuildInvocation(
        MethodDeclaration targetDeclaration,
        IEnumerable<TransformedInvocationParameter> transformedParameters,
        int instanceCount,
        SubTransformerContext context);

    /// <summary>
    /// Creates VHDL elements for each of the method's invocations and inserts them into the <paramref
    /// name="context"/>'s <see cref="CurrentBlock"/>.
    /// </summary>
    IEnumerable<IVhdlElement> BuildMultiInvocationWait(
        MethodDeclaration targetDeclaration,
        int instanceCount,
        bool waitForAll,
        SubTransformerContext context);

    /// <summary>
    /// Same as <see cref="BuildMultiInvocationWait"/> but there is only one invocation.
    /// </summary>
    IVhdlElement BuildSingleInvocationWait(
        MethodDeclaration targetDeclaration,
        int targetIndex,
        SubTransformerContext context);
}

/// <summary>
/// The result of a <see cref="IStateMachineInvocationBuilder.BuildInvocation"/> call.
/// </summary>
public interface IBuildInvocationResult
{
    /// <summary>
    /// Gets the collection of assignments that result from outflowing ( <see langword="out"/>) parameters.
    /// </summary>
    IEnumerable<Assignment> OutParameterBackAssignments { get; }
}
