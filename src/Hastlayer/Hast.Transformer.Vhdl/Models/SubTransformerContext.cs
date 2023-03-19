using Hast.Layer;
using Hast.Transformer.Vhdl.ArchitectureComponents;
using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hast.Transformer.Vhdl.Models;

public class SubTransformerContext
{
    public IVhdlTransformationContext TransformationContext { get; set; }
    public SubTransformerScope Scope { get; set; }
}

public class SubTransformerScope
{
    /// <summary>
    /// Gets or sets the method's declaration.
    /// </summary>
    public MethodDeclaration Method { get; set; }

    /// <summary>
    /// Gets or sets the state machine created by <see cref="MemberStateMachineFactory"/>.
    /// </summary>
    public IMemberStateMachine StateMachine { get; set; }

    /// <summary>
    /// Gets or sets the block of the method body.
    /// </summary>
    public CurrentBlock CurrentBlock { get; set; }

    /// <summary>
    /// Gets the names of variables that store object references to compiler-generated DisplayClasses (created for
    /// lambda expressions) to full DisplayClass names.
    /// </summary>
    public IDictionary<string, string> VariableNameToDisplayClassNameMappings { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets the dictionary that keeps track of the name of those variables that store references to Tasks and then
    /// later the Task results fetched from them via <see cref="Task{T}.Result"/>.
    /// </summary>
    public IDictionary<string, MethodDeclaration> TaskVariableNameToDisplayClassMethodMappings { get; } =
        new Dictionary<string, MethodDeclaration>();

    /// <summary>
    /// Gets the dictionary that Keeps track of which invoked state machines were finished in which states. This is
    /// needed not to immediately restart a component in the state it was finished.
    /// </summary>
    public IDictionary<int, ISet<string>> FinishedInvokedStateMachinesForStates { get; } = new Dictionary<int, ISet<string>>();

    /// <summary>
    /// Gets the label statements to state machine state indices. This is necessary because each label should have its
    /// own state (so it's possible to jump to it).
    /// </summary>
    public IDictionary<string, int> LabelsToStateIndicesMappings { get; } = new Dictionary<string, int>();

    /// <summary>
    /// Gets any other custom values for the scope.
    /// </summary>
    public IDictionary<string, dynamic> CustomProperties { get; } = new Dictionary<string, dynamic>();

    /// <summary>
    /// Gets the warnings issued during transformation.
    /// </summary>
    public IList<ITransformationWarning> Warnings { get; } = new List<ITransformationWarning>();
}

public class CurrentBlock
{
    private readonly IMemberStateMachine _stateMachine;
    private IBlockElement _currentBlock;

    public int StateMachineStateIndex { get; private set; }

    public decimal RequiredClockCycles
    {
        get => _stateMachine.States[StateMachineStateIndex].RequiredClockCycles;

        set => _stateMachine.States[StateMachineStateIndex].RequiredClockCycles = value;
    }

    public CurrentBlock(IMemberStateMachine stateMachine, IBlockElement currentBlock, int stateMachineStateIndex)
        : this(stateMachine)
    {
        _currentBlock = currentBlock;
        StateMachineStateIndex = stateMachineStateIndex;
    }

    public CurrentBlock(IMemberStateMachine stateMachine)
    {
        _currentBlock = new InlineBlock();
        _stateMachine = stateMachine;
    }

    public void Add(IVhdlElement element) => _currentBlock.Add(element);

    public void ChangeBlockToDifferentState(IBlockElement newBlock, int stateMachineStateIndex)
    {
        StateMachineStateIndex = stateMachineStateIndex;
        ChangeBlock(newBlock);
    }

    public void ChangeBlock(IBlockElement newBlock) => _currentBlock = newBlock;
}
