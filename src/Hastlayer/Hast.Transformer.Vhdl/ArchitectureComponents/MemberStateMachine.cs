using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.VhdlBuilder.Representation.Expression;
using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.ArchitectureComponents;

internal sealed class MemberStateMachine : ArchitectureComponentBase, IMemberStateMachine
{
    private readonly Enum _statesEnum;
    private readonly Variable _stateVariable;

    private readonly List<IMemberStateMachineState> _states;
    public IReadOnlyList<IMemberStateMachineState> States => _states;

    public MemberStateMachine(string name)
        : base(name)
    {
        _statesEnum = new Enum { Name = this.CreatePrefixedObjectName("_States") };

        _stateVariable = new Variable
        {
            DataType = _statesEnum,
            Name = this.CreateStateVariableName(),
            InitialValue = this.CreateStateName(0).ToVhdlIdValue(),
        };
        LocalVariables.Add(_stateVariable);

        var startedSignal = new Signal
        {
            DataType = KnownDataTypes.Boolean,
            Name = this.CreateStartedSignalName(),
            InitialValue = Value.False,
        };
        ExternallyDrivenSignals.Add(startedSignal);

        var finishedSignal = new Signal
        {
            DataType = KnownDataTypes.Boolean,
            Name = this.CreateFinishedSignalName(),
            InitialValue = Value.False,
        };
        InternallyDrivenSignals.Add(finishedSignal);

        var startStateBlock = new InlineBlock(
            new LineComment("Start state"),
            new LineComment("Waiting for the start signal."),
            new IfElse
            {
                Condition = new Binary
                {
                    Left = startedSignal.Name.ToVhdlSignalReference(),
                    Operator = BinaryOperator.Equality,
                    Right = Value.True,
                },
                True = this.CreateStateChange(2),
            });

        var finalStateBlock = new InlineBlock(
            new LineComment("Final state"),
            new LineComment("Signaling finished until Started is pulled back to false, then returning to the start state."),
            new IfElse
            {
                Condition = new Binary
                {
                    Left = startedSignal.Name.ToVhdlSignalReference(),
                    Operator = BinaryOperator.Equality,
                    Right = Value.True,
                },
                True = new Assignment { AssignTo = finishedSignal, Expression = Value.True },
                Else = new InlineBlock(
                    new Assignment { AssignTo = finishedSignal, Expression = Value.False },
                    this.ChangeToStartState()),
            });

        _states = new List<IMemberStateMachineState>
        {
            new MemberStateMachineState { Body = startStateBlock },
            new MemberStateMachineState { Body = finalStateBlock },
        };
    }

    public int AddState(IBlockElement state)
    {
        _states.Add(new MemberStateMachineState { Body = state });
        return _states.Count - 1;
    }

    public void RecordMultiCycleOperation(IDataObject operationResultReference, int requiredClockCyclesCeiling) =>
        _multiCycleOperations.Add(new MultiCycleOperation
        {
            OperationResultReference = operationResultReference,
            RequiredClockCyclesCeiling = requiredClockCyclesCeiling,
        });

    public override IVhdlElement BuildDeclarations()
    {
        for (int i = 0; i < _states.Count; i++)
        {
            _statesEnum.Values.Add(this.CreateStateName(i).ToVhdlIdValue());
        }

        return BuildDeclarationsBlock(new InlineBlock(
            new LineComment("State machine states:"),
            _statesEnum));
    }

    public override IVhdlElement BuildBody()
    {
        var stateCase = new Case { Expression = _stateVariable.Name.ToVhdlIdValue() };

        for (int i = 0; i < _states.Count; i++)
        {
            var stateWhen = new CaseWhen { Expression = this.CreateStateName(i).ToVhdlIdValue() };
            stateWhen.Add(_states[i].Body);
            stateWhen.Add(new LineComment(
                "Clock cycles needed to complete this state (approximation): " +
                // The G29 format specifier will cut trailing zeros apart from x.0. See:
                // https://msdn.microsoft.com/en-us/library/dwhawy9k(v=VS.90).aspx
                _states[i].RequiredClockCycles.ToString("G29", System.Globalization.CultureInfo.InvariantCulture)));
            stateCase.Whens.Add(stateWhen);
        }

        var process = BuildProcess(stateCase);

        process.Name = this.CreatePrefixedObjectName("_StateMachine");

        return new LogicalBlock(
            new LineComment(Name + " state machine start"),
            process,
            new LineComment(Name + " state machine end"));
    }

    public sealed class MemberStateMachineState : IMemberStateMachineState
    {
        public IBlockElement Body { get; set; }
        public decimal RequiredClockCycles { get; set; }
    }

    private sealed class MultiCycleOperation : IMultiCycleOperation
    {
        public IDataObject OperationResultReference { get; set; }
        public int RequiredClockCyclesCeiling { get; set; }
    }
}
