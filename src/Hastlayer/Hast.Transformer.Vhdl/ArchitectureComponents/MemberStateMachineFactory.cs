using Hast.Common.Interfaces;

namespace Hast.Transformer.Vhdl.ArchitectureComponents;

/// <summary>
/// Creates new <see cref="IMemberStateMachine"/> objects.
/// </summary>
public interface IMemberStateMachineFactory : IDependency
{
    /// <summary>
    /// Creates a new <see cref="IMemberStateMachine"/> object.
    /// </summary>
    /// <param name="name">
    /// The name of the state machine, i.e. the name of the member to create the state machine for. Use the real name,
    /// not an extended VHDL ID.
    /// </param>
    IMemberStateMachine CreateStateMachine(string name);
}

public class MemberStateMachineFactory : IMemberStateMachineFactory
{
    public IMemberStateMachine CreateStateMachine(string name) => new MemberStateMachine(name);
}
