using Hast.Common.Configuration;
using Hast.Transformer.Models;
using Hast.Transformer.Vhdl.ArchitectureComponents;
using Hast.Transformer.Vhdl.Constants;
using Hast.Transformer.Vhdl.Helpers;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.VhdlBuilder.Representation.Expression;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using Enum = Hast.VhdlBuilder.Representation.Declaration.Enum;

namespace Hast.Transformer.Vhdl.InvocationProxyBuilders;

public class InvocationProxyBuilder : IInvocationProxyBuilder
{
    // So it's not cut off wrongly if names are shortened we need to use a name for this signal as it would look from a
    // generated state machine.
    private const string NamePrefix = "System.Void Hast::InternalInvocationProxy().";

    private readonly Value _waitingForStartedStateValue = "WaitingForStarted".ToVhdlIdValue();
    private readonly Value _waitingForFinishedStateValue = "WaitingForFinished".ToVhdlIdValue();
    private readonly Value _afterFinishedStateValue = "AfterFinished".ToVhdlIdValue();

    public IEnumerable<IArchitectureComponent> BuildInternalProxy(
        ICollection<IArchitectureComponent> components,
        IVhdlTransformationContext transformationContext)
    {
        var componentsByName = components.ToDictionary(component => component.Name);

        // [invoked member declaration][from component name][invocation instance count]
        var invokedMembers = new Dictionary<EntityDeclaration, List<KeyValuePair<string, int>>>();

        // Summarizing which member was invoked with how many instances from which component.
        CountInvocationInstances(components, invokedMembers);

        foreach (var invokedMember in invokedMembers)
        {
            var memberFullName = invokedMember.Key.GetFullName();

            // Is this a recursive member? If yes then remove the last component (i.e. the one with the highest index,
            // the deepest one in the call stack) from the list of invoking ones, because that won't invoke anything
            // else.
            var maxIndexComponent = invokedMember.Value
                .Where(component => component.Key.StartsWithOrdinal(memberFullName))
                .OrderByDescending(component => component.Key)
                .Cast<KeyValuePair<string, int>?>()
                .FirstOrDefault();

            if (maxIndexComponent is { } value) invokedMember.Value.Remove(value);
        }

        var proxyComponents = new List<IArchitectureComponent>(invokedMembers.Count);

        var booleanArrayType = GetBooleanArrayType();
        var runningStates = new Enum
        {
            Name = (NamePrefix + "_RunningStates").ToExtendedVhdlId(),
        };
        runningStates.Values.Add(_waitingForStartedStateValue);
        runningStates.Values.Add(_waitingForFinishedStateValue);
        runningStates.Values.Add(_afterFinishedStateValue);
        proxyComponents.Add(new BasicComponent((NamePrefix + "_CommonDeclarations").ToExtendedVhdlId())
        {
            Declarations = new InlineBlock(booleanArrayType, runningStates),
        });

        foreach (var invokedMember in invokedMembers)
        {
            var targetMember = invokedMember.Key;
            var invokedFromComponents = invokedMember.Value;
            var targetMemberName = targetMember.GetFullName();
            var proxyComponentName = NamePrefix + targetMemberName;

            // How many instances does this member have in form of components, e.g. how many state machines are there
            // for this member? This is not necessarily the same as the invocation instance count.
            var targetComponentCount = transformationContext
                .GetTransformerConfiguration()
                .GetMaxInvocationInstanceCountConfigurationForMember(targetMember)
                .MaxInvocationInstanceCount;

            // Is this member's component only invoked from a single other component? Because then we don't need a full
            // invocation proxy: local Start and Finished signals can be directly connected to the target component's
            // signals. (Every member at this point is invoked at least once.)
            var invokedFromSingleComponent = invokedFromComponents.Take(2).Count() == 1;

            // Is this member's component invoked from multiple components, but just once from each of them and there
            // are a sufficient number of target components available? Then we can pair them together.
            var invocationsCanBePaired =
                !invokedFromSingleComponent &&
                !invokedFromComponents.Any(componentInvocation => componentInvocation.Value > 1) &&
                invokedFromComponents.Sum(invokingComponent => invokingComponent.Value) <= targetComponentCount;

            if (invokedFromSingleComponent || invocationsCanBePaired)
            {
                var proxyComponent = new BasicComponent(proxyComponentName);
                var signalConnectionsBlock = new InlineBlock();
                proxyComponent.Body = signalConnectionsBlock;
                proxyComponents.Add(proxyComponent);

                BuildProxyInvokationFromSingleComponentOrPairable(
                    invokedFromComponents,
                    invokedFromSingleComponent,
                    targetMemberName,
                    signalConnectionsBlock,
                    componentsByName);
            }
            else
            {
                BuildProxyInvokationFromMultipleComponents(
                    proxyComponents,
                    targetComponentCount,
                    targetMemberName,
                    invokedFromComponents,
                    runningStates,
                    componentsByName);
            }
        }

        return proxyComponents;
    }

    public IArchitectureComponent BuildExternalProxy(
        IEnumerable<IMemberTransformerResult> hardwareEntryPointMemberResults,
        MemberIdTable memberIdTable)
    {
        // So it's not cut off wrongly if names are shortened we need to use a name for this signal as it would look
        // from a generated state machine.
        var proxyComponent = new ConfigurableComponent("System.Void Hast::ExternalInvocationProxy()");

        // Since the Finished port is an out port, it can't be read. Adding an internal proxy signal so we can also read
        // it.
        var finishedSignal = new Signal
        {
            Name = "FinishedInternal".ToExtendedVhdlId(),
            DataType = KnownDataTypes.Boolean,
            InitialValue = Value.False,
        };
        proxyComponent.InternallyDrivenSignals.Add(finishedSignal);
        var finishedSignalReference = finishedSignal.ToReference();
        proxyComponent.BeginBodyWith = new Assignment
        {
            AssignTo = CommonPortNames.Finished.ToVhdlSignalReference(),
            Expression = finishedSignalReference,
        };

        var memberSelectingCase = new Case { Expression = CommonPortNames.MemberId.ToVhdlIdValue() };

        foreach (var member in hardwareEntryPointMemberResults.Select(result => result.Member))
        {
            var memberName = member.GetFullName();
            var memberId = memberIdTable.LookupMemberId(memberName);
            proxyComponent.OtherMemberMaxInvocationInstanceCounts[member] = 1;
            var when = new CaseWhen
            {
                Expression = memberId.ToVhdlValue(KnownDataTypes.UnrangedInt),
            };

            var waitForInvocationFinishedIfElse = InvocationHelper
                .CreateWaitForInvocationFinished(proxyComponent, memberName, 1);

            waitForInvocationFinishedIfElse.True.Add(new Assignment
            {
                AssignTo = finishedSignalReference,
                Expression = Value.True,
            });

            var ifElse = new IfElse
            {
                Condition = new Binary
                {
                    Left = InvocationHelper.CreateStartedSignalReference(proxyComponent, memberName, 0),
                    Operator = BinaryOperator.Equality,
                    Right = Value.False,
                },
                True = InvocationHelper.CreateInvocationStart(proxyComponent, memberName, 0),
            };
            ifElse.ElseIfs.Add(new If
            {
                Condition = waitForInvocationFinishedIfElse.Condition,
                True = waitForInvocationFinishedIfElse.True,
            });
            when.Add(ifElse);

            memberSelectingCase.Whens.Add(when);
        }

        memberSelectingCase.Whens.Add(CaseWhen.CreateOthers());

        var startedPortReference = CommonPortNames.Started.ToVhdlSignalReference();
        proxyComponent.ProcessNotInReset = new IfElse
        {
            Condition = new Binary
            {
                Left = new Binary
                {
                    Left = startedPortReference,
                    Operator = BinaryOperator.Equality,
                    Right = Value.True,
                },
                Operator = BinaryOperator.And,
                Right = new Binary
                {
                    Left = finishedSignalReference,
                    Operator = BinaryOperator.Equality,
                    Right = Value.False,
                },
            },
            True = new InlineBlock(
                new LineComment("Starting the state machine corresponding to the given member ID."),
                memberSelectingCase),
            Else = new InlineBlock(
                new LineComment("Waiting for Started to be pulled back to zero that signals the framework noting the finish."),
                new IfElse
                {
                    Condition = new Binary
                    {
                        Left = new Binary
                        {
                            Left = startedPortReference,
                            Operator = BinaryOperator.Equality,
                            Right = Value.False,
                        },
                        Operator = BinaryOperator.And,
                        Right = new Binary
                        {
                            Left = finishedSignalReference,
                            Operator = BinaryOperator.Equality,
                            Right = Value.True,
                        },
                    },
                    True = new Assignment
                    {
                        AssignTo = finishedSignalReference,
                        Expression = Value.False,
                    },
                }),
        };

        return proxyComponent;
    }

    private void BuildProxyInvokationFromMultipleComponents(
        List<IArchitectureComponent> proxyComponents,
        int targetComponentCount,
        string targetMemberName,
        List<KeyValuePair<string, int>> invokedFromComponents,
        Enum runningStates,
        Dictionary<string, IArchitectureComponent> componentsByName)
    {
        var proxyComponentName = NamePrefix + targetMemberName;
        var booleanArrayType = GetBooleanArrayType();

        // Note that the below implementation does not work perfectly. As the number of components increases it becomes
        // unstable. For example the CalculateFibonacchiSeries sample without debug memory writes won't finish while the
        // CalculateFactorial with them will work properly.
        var proxyComponent = new ConfigurableComponent(proxyComponentName);
        proxyComponents.Add(proxyComponent);

        var proxyInResetBlock = new InlineBlock();
        proxyComponent.ProcessInReset = proxyInResetBlock;
        var bodyBlock = new InlineBlock();

        DataObjectReference targetAvailableIndicatorVariableReference = null;
        SizedDataType targetAvailableIndicatorDataType = null;

        if (targetComponentCount > 1)
        {
            // Creating a boolean vector where each of the elements will indicate whether the target component with that
            // index is available and can be started. I.e. targetAvailableIndicator(0) being true tells that the target
            // component with index 0 can be started. All this is necessary to avoid ifs with large conditions which
            // would cause timing errors with more than cca. 20 components. This implementation can be better
            // implemented with parallel paths.
            targetAvailableIndicatorVariableReference = proxyComponent
                .CreatePrefixedSegmentedObjectName("targetAvailableIndicator")
                .ToVhdlVariableReference();
            targetAvailableIndicatorDataType = new SizedDataType
            {
                Name = booleanArrayType.Name,
                TypeCategory = DataTypeCategory.Array,
                SizeNumber = targetComponentCount,
            };
            proxyComponent.LocalVariables.Add(new Variable
            {
                DataType = targetAvailableIndicatorDataType,
                Name = targetAvailableIndicatorVariableReference.Name,
                InitialValue = ("others => " + KnownDataTypes.Boolean.DefaultValue.ToVhdl())
                    .ToVhdlValue(targetAvailableIndicatorDataType),
            });

            bodyBlock.Add(new LineComment(
                "Building a boolean array where each of the elements will indicate whether the component with " +
                "the given index should be started next."));

            for (int c = 0; c < targetComponentCount; c++)
            {
                var selectorConditions = new List<IVhdlElement>
                        {
                            new Binary
                            {
                                Left = ArchitectureComponentNameHelper
                                .CreateStartedSignalName(GetTargetMemberComponentName(c, targetMemberName))
                                .ToVhdlSignalReference(),
                                Operator = BinaryOperator.Equality,
                                Right = Value.False,
                            },
                        };
                for (int s = c + 1; s < targetComponentCount; s++)
                {
                    selectorConditions.Add(new Binary
                    {
                        Left = ArchitectureComponentNameHelper
                            .CreateStartedSignalName(GetTargetMemberComponentName(s, targetMemberName))
                            .ToVhdlSignalReference(),
                        Operator = BinaryOperator.Equality,
                        Right = Value.True,
                    });
                }

                // Assignments to the boolean array where each element will indicate whether the component with the
                // given index can be started.
                bodyBlock.Add(
                    new Assignment
                    {
                        AssignTo = new ArrayElementAccess
                        {
                            ArrayReference = targetAvailableIndicatorVariableReference,
                            IndexExpression = c.ToVhdlValue(KnownDataTypes.UnrangedInt),
                        },
                        Expression = BinaryChainBuilder.BuildBinaryChain(selectorConditions, BinaryOperator.And),
                    });
            }
        }

        // Building the invocation handlers.
        foreach (var invocation in invokedFromComponents)
        {
            var invokerName = invocation.Key;
            var invocationInstanceCount = invocation.Value;

            for (int i = 0; i < invocationInstanceCount; i++)
            {
                var runningIndexName = proxyComponent
                    .CreatePrefixedSegmentedObjectName(invokerName, "runningIndex", i.ToTechnicalString());
                var runningIndexVariableReference = runningIndexName.ToVhdlVariableReference();
                proxyComponent.LocalVariables.Add(new Variable
                {
                    DataType = new RangedDataType(KnownDataTypes.UnrangedInt)
                    {
                        RangeMin = 0,
                        RangeMax = targetComponentCount - 1,
                    },
                    Name = runningIndexName,
                });

                var runningStateVariableName = proxyComponent
                    .CreatePrefixedSegmentedObjectName(invokerName, "runningState", i.ToTechnicalString());
                var runningStateVariableReference = runningStateVariableName.ToVhdlVariableReference();
                proxyComponent.LocalVariables.Add(new Variable
                {
                    DataType = runningStates,
                    Name = runningStateVariableName,
                    InitialValue = _waitingForStartedStateValue,
                });

                var invocationHandlerBlock = new LogicalBlock(
                    new LineComment(
                        "Invocation handler #" + i.ToTechnicalString() +
                        " out of " + invocationInstanceCount.ToTechnicalString() +
                        " corresponding to " + invokerName));
                bodyBlock.Add(invocationHandlerBlock);

                var runningStateCase = new Case
                {
                    Expression = runningStateVariableReference,
                };

                var waitContext = new WaitContext
                {
                    RunningStateVariableReference = runningStateVariableReference,
                    RunningIndexVariableReference = runningIndexVariableReference,
                    TargetAvailableIndicatorVariableReference = targetAvailableIndicatorVariableReference,
                    TargetComponentCount = targetComponentCount,
                    InvokerName = invokerName,
                    InvokerIndex = i,
                    TargetMemberName = targetMemberName,
                    ComponentsByName = componentsByName,
                    TargetAvailableIndicatorDataType = targetAvailableIndicatorDataType,
                    RunningStateCase = runningStateCase,
                };

                // WaitingForStarted state
                WaitForStarted(waitContext);

                // WaitingForFinished state
                WaitForFinished(waitContext);

                // AfterFinished state
                WhenFinished(waitContext);

                // Adding reset for the finished signal.
                proxyInResetBlock.Add(new Assignment
                {
                    AssignTo = InvocationHelper
                        .CreateFinishedSignalReference(invokerName, targetMemberName, i),
                    Expression = Value.False,
                });

                invocationHandlerBlock.Add(runningStateCase);
            }
        }

        proxyComponent.ProcessNotInReset = bodyBlock;
    }

    private void WhenFinished(WaitContext context) =>
        context
            .RunningStateCase
            .Whens
            .Add(new CaseWhen(
                expression: _afterFinishedStateValue,
                body: new List<IVhdlElement>
                {
                    new LineComment("Invoking components need to pull down the Started signal to false."),
                    new IfElse
                    {
                        Condition = new Binary
                        {
                            Left = InvocationHelper.CreateStartedSignalReference(
                                context.InvokerName,
                                context.TargetMemberName,
                                context.InvokerIndex),
                            Operator = BinaryOperator.Equality,
                            Right = Value.False,
                        },
                        True = new InlineBlock(
                            new Assignment
                            {
                                AssignTo = context.RunningStateVariableReference,
                                Expression = _waitingForStartedStateValue,
                            },
                            new Assignment
                            {
                                AssignTo = InvocationHelper.CreateFinishedSignalReference(
                                    context.InvokerName,
                                    context.TargetMemberName,
                                    context.InvokerIndex),
                                Expression = Value.False,
                            }),
                    },
                }));

    private void WaitForFinished(WaitContext context)
    {
        var runningIndexCase = new Case
        {
            Expression = context.RunningIndexVariableReference,
        };

        for (int componentIndex = 0; componentIndex < context.TargetComponentCount; componentIndex++)
        {
            var caseWhenBody = CreateNullOperationIfTargetComponentEqualsInvokingComponent(
                componentIndex,
                context.TargetMemberName,
                context.InvokerName);

            if (caseWhenBody != null) continue;

            var isFinishedIfTrue = new InlineBlock(
                new Assignment
                {
                    AssignTo = context.RunningStateVariableReference,
                    Expression = _afterFinishedStateValue,
                },
                new Assignment
                {
                    AssignTo = InvocationHelper
                        .CreateFinishedSignalReference(
                            context.InvokerName,
                            context.TargetMemberName,
                            context.InvokerIndex),
                    Expression = Value.True,
                },
                new Assignment
                {
                    AssignTo = ArchitectureComponentNameHelper
                        .CreateStartedSignalName(GetTargetMemberComponentName(
                            componentIndex,
                            context.TargetMemberName))
                        .ToVhdlSignalReference(),
                    Expression = Value.False,
                });

            var returnAssignment = BuildReturnAssigment(
                context.InvokerName,
                context.InvokerIndex,
                componentIndex,
                context.TargetMemberName,
                context.ComponentsByName);
            if (returnAssignment != null) isFinishedIfTrue.Add(returnAssignment);

            isFinishedIfTrue.Body.AddRange(BuildOutParameterAssignments(
                context.InvokerName,
                context.InvokerIndex,
                componentIndex,
                context.TargetMemberName,
                context.ComponentsByName));

            caseWhenBody = new If
            {
                Condition = ArchitectureComponentNameHelper
                    .CreateFinishedSignalName(GetTargetMemberComponentName(
                        componentIndex,
                        context.TargetMemberName))
                    .ToVhdlSignalReference(),
                True = isFinishedIfTrue,
            };

            runningIndexCase.Whens.Add(new CaseWhen(
                expression: componentIndex.ToVhdlValue(KnownDataTypes.UnrangedInt),
                body: new List<IVhdlElement> { { caseWhenBody } }));
        }

        context.RunningStateCase.Whens.Add(new CaseWhen(
            expression: _waitingForFinishedStateValue,
            body: new List<IVhdlElement> { runningIndexCase }));
    }

    private void WaitForStarted(WaitContext context)
    {
        var waitingForStartedInnnerBlock = new InlineBlock();

        IVhdlElement CreateComponentAvailableBody(int targetIndex)
        {
            var componentAvailableBody = CreateNullOperationIfTargetComponentEqualsInvokingComponent(
                targetIndex,
                context.TargetMemberName,
                context.InvokerName);

            if (componentAvailableBody != null) return componentAvailableBody;

            var componentAvailableBodyBlock = new InlineBlock(
                new Assignment
                {
                    AssignTo = context.RunningStateVariableReference,
                    Expression = _waitingForFinishedStateValue,
                },
                new Assignment
                {
                    AssignTo = context.RunningIndexVariableReference,
                    Expression = targetIndex.ToVhdlValue(KnownDataTypes.UnrangedInt),
                },
                new Assignment
                {
                    AssignTo = ArchitectureComponentNameHelper
                        .CreateStartedSignalName(GetTargetMemberComponentName(targetIndex, context.TargetMemberName))
                        .ToVhdlSignalReference(),
                    Expression = Value.True,
                });

            if (context.TargetComponentCount > 1)
            {
                componentAvailableBodyBlock.Add(new Assignment
                {
                    AssignTo = new ArrayElementAccess
                    {
                        ArrayReference = context.TargetAvailableIndicatorVariableReference,
                        IndexExpression = targetIndex.ToVhdlValue(KnownDataTypes.UnrangedInt),
                    },
                    Expression = Value.False,
                });
            }

            componentAvailableBodyBlock.Body.AddRange(BuildInParameterAssignments(
                context.InvokerName,
                context.InvokerIndex,
                targetIndex,
                context.ComponentsByName,
                context.TargetMemberName));

            return componentAvailableBodyBlock;
        }

        if (context.TargetComponentCount == 1)
        {
            // If there is only a single target component then the implementation can be simpler. Also having a case
            // targetAvailableIndicator is (true) when =>... isn't syntactically correct, single-element arrays can't be
            // matched like this. "[Synth 8-2778] type error near true ; expected type
            // internalinvocationproxy_boolean_array".

            waitingForStartedInnnerBlock.Add(CreateComponentAvailableBody(0));
        }
        else
        {
            var availableTargetSelectingCase = new Case
            {
                Expression = context.TargetAvailableIndicatorVariableReference,
            };

            for (int c = 0; c < context.TargetComponentCount; c++)
            {
                availableTargetSelectingCase.Whens.Add(new CaseWhen(
                    expression: CreateBooleanIndicatorValue(context.TargetAvailableIndicatorDataType, c),
                    body: new List<IVhdlElement> { { CreateComponentAvailableBody(c) } }));
            }

            availableTargetSelectingCase.Whens.Add(new CaseWhen(
                expression: "others".ToVhdlIdValue(),
                body: new List<IVhdlElement> { { Null.Instance.Terminate() } }));

            waitingForStartedInnnerBlock.Add(availableTargetSelectingCase);
        }

        context.RunningStateCase.Whens.Add(new CaseWhen(
            expression: _waitingForStartedStateValue,
            body: new List<IVhdlElement>
            {
                new If
                {
                    Condition = InvocationHelper
                        .CreateStartedSignalReference(
                            context.InvokerName,
                            context.TargetMemberName,
                            context.InvokerIndex),
                    True = new InlineBlock(
                        new Assignment
                        {
                            AssignTo = InvocationHelper
                                .CreateFinishedSignalReference(
                                    context.InvokerName,
                                    context.TargetMemberName,
                                    context.InvokerIndex),
                            Expression = Value.False,
                        },
                        waitingForStartedInnnerBlock),
                },
            }));
    }

    private static string GetTargetMemberComponentName(int index, string targetMemberName) =>
        ArchitectureComponentNameHelper.CreateIndexedComponentName(targetMemberName, index);

    /// <summary>
    /// Check if the component would invoke itself. This can happen with recursive calls.
    /// </summary>
    private static IVhdlElement CreateNullOperationIfTargetComponentEqualsInvokingComponent(
        int index,
        string targetMemberName,
        string invokerName)
    {
        if (GetTargetMemberComponentName(index, targetMemberName) != invokerName) return null;

        return new InlineBlock(
            new LineComment("The component can't invoke itself, so not putting anything here."),
            new Terminated(Null.Instance));
    }

    private static IEnumerable<IVhdlElement> BuildOutParameterAssignments(
        string invokerName,
        int invokerIndex,
        int targetIndex,
        string targetMemberName,
        Dictionary<string, IArchitectureComponent> componentsByName)
    {
        var targetComponentName = ArchitectureComponentNameHelper
            .CreateIndexedComponentName(targetMemberName, targetIndex);
        var passedBackParameters =
            componentsByName[targetComponentName]
            .GetOutParameterSignals()
            .Where(parameter =>
                parameter.TargetMemberFullName == targetMemberName &&
                parameter.IsOwn);

        var receivingParameters = componentsByName[invokerName]
                    .GetInParameterSignals()
                    .Where(parameter =>
                        parameter.TargetMemberFullName == targetMemberName &&
                        parameter.Index == invokerIndex &&
                        !parameter.IsOwn)
                    .ToList();

        if (!receivingParameters.Any()) return Enumerable.Empty<IVhdlElement>();

        return passedBackParameters.Select(parameter => new Assignment
        {
            AssignTo = receivingParameters.Single(p => p.TargetParameterName == parameter.TargetParameterName),
            Expression = parameter.ToReference(),
        });
    }

    private static void BuildProxyInvokationFromSingleComponentOrPairable(
        List<KeyValuePair<string, int>> invokedFromComponents,
        bool invokedFromSingleComponent,
        string targetMemberName,
        InlineBlock signalConnectionsBlock,
        Dictionary<string, IArchitectureComponent> componentsByName)
    {
        string GetTargetMemberComponentNameLocal(int index) =>
            ArchitectureComponentNameHelper.CreateIndexedComponentName(targetMemberName, index);

        for (int i = 0; i < invokedFromComponents.Count; i++)
        {
            var invokedFromComponent = invokedFromComponents[i];
            var invokerName = invokedFromComponent.Key;

            for (int j = 0; j < invokedFromComponent.Value; j++)
            {
                var targetIndex = invokedFromSingleComponent ? j : i;
                var targetComponentName = GetTargetMemberComponentNameLocal(targetIndex);
                var invokerIndex = invokedFromSingleComponent ? j : 0;

                signalConnectionsBlock.Add(new LineComment(FormattableString.Invariant(
                    $"Signal connections for {invokerName} (#{targetIndex}):")));

                signalConnectionsBlock.Add(new Assignment
                {
                    AssignTo = ArchitectureComponentNameHelper
                        .CreateStartedSignalName(targetComponentName)
                        .ToVhdlSignalReference(),
                    Expression = InvocationHelper
                            .CreateStartedSignalReference(invokerName, targetMemberName, invokerIndex),
                });

                signalConnectionsBlock.Body.AddRange(
                    BuildInParameterAssignments(invokerName, invokerIndex, targetIndex, componentsByName, targetMemberName));

                signalConnectionsBlock.Add(new Assignment
                {
                    AssignTo = InvocationHelper
                            .CreateFinishedSignalReference(invokerName, targetMemberName, invokerIndex),
                    Expression = ArchitectureComponentNameHelper
                        .CreateFinishedSignalName(targetComponentName)
                        .ToVhdlSignalReference(),
                });

                var returnAssignment = BuildReturnAssigment(invokerName, invokerIndex, targetIndex, targetMemberName, componentsByName);
                if (returnAssignment != null) signalConnectionsBlock.Add(returnAssignment);
                signalConnectionsBlock.Body.AddRange(
                    BuildOutParameterAssignments(invokerName, invokerIndex, targetIndex, targetMemberName, componentsByName));
            }
        }
    }

    public static IVhdlElement BuildReturnAssigment(
        string invokerName,
        int invokerIndex,
        int targetIndex,
        string targetMemberName,
        IDictionary<string, IArchitectureComponent> componentsByName)
    {
        // Does the target have a return value?
        var targetComponentName = ArchitectureComponentNameHelper
            .CreateIndexedComponentName(targetMemberName, targetIndex);
        var targetComponent = componentsByName[targetComponentName];
        var returnSignal =
            targetComponent
            .InternallyDrivenSignals
            .SingleOrDefault(signal => signal.Name == targetComponent.CreateReturnSignalReference().Name);

        if (returnSignal == null) return null;

        return new Assignment
        {
            AssignTo = componentsByName[invokerName]
                    .CreateReturnSignalReferenceForTargetComponent(targetMemberName, invokerIndex),
            Expression = returnSignal.ToReference(),
        };
    }

    private static IEnumerable<IVhdlElement> BuildInParameterAssignments(
        string invokerName,
        int invokerIndex,
        int targetIndex,
        Dictionary<string, IArchitectureComponent> componentsByName,
        string targetMemberName)
    {
        var passedParameters = componentsByName[invokerName]
            .GetOutParameterSignals()
            .Where(parameter =>
                parameter.TargetMemberFullName == targetMemberName &&
                parameter.Index == invokerIndex &&
                !parameter.IsOwn);

        var targetComponentName = ArchitectureComponentNameHelper
            .CreateIndexedComponentName(targetMemberName, targetIndex);
        var targetParameters =
            componentsByName[targetComponentName]
            .GetInParameterSignals()
            .Where(parameter =>
                parameter.TargetMemberFullName == targetMemberName &&
                parameter.IsOwn)
            .ToList();

        if (!targetParameters.Any()) return Enumerable.Empty<IVhdlElement>();

        return passedParameters.Select(parameter => new Assignment
        {
            AssignTo = targetParameters
                        .Single(p => p.TargetParameterName == parameter.TargetParameterName),
            Expression = parameter.ToReference(),
        });
    }

    private static void CountInvocationInstances(
        IEnumerable<IArchitectureComponent> components,
        Dictionary<EntityDeclaration, List<KeyValuePair<string, int>>> invokedMembers)
    {
        foreach (var component in components.Where(component => component.OtherMemberMaxInvocationInstanceCounts.Any()))
        {
            foreach (var memberInvocationCount in component.OtherMemberMaxInvocationInstanceCounts)
            {
                var targetMember = memberInvocationCount.Key;

                if (!invokedMembers.TryGetValue(targetMember, out var invokedFromList))
                {
                    invokedMembers[targetMember] = invokedFromList = new List<KeyValuePair<string, int>>();
                }

                invokedFromList.Add(new KeyValuePair<string, int>(component.Name, memberInvocationCount.Value));
            }
        }
    }

    private static IVhdlElement CreateBooleanIndicatorValue(SizedDataType targetAvailableIndicatorDataType, int indicatedIndex)
    {
        // This will create a boolean array where the everything is false except for the element with the given index.

        var booleanArray = Enumerable
            .Repeat((IVhdlElement)Value.False, targetAvailableIndicatorDataType.SizeNumber)
            .ToArray();
        // Since the bit vector is downto the rightmost element is the 0th.
        booleanArray[targetAvailableIndicatorDataType.SizeNumber - 1 - indicatedIndex] = Value.True;
        return new Value
        {
            DataType = targetAvailableIndicatorDataType,
            EvaluatedContent = new InlineBlock(booleanArray),
        };
    }

    private static ArrayType GetBooleanArrayType() =>
        new()
        {
            ElementType = KnownDataTypes.Boolean,
            // Prefixing the name so it won't clash with boolean arrays created by the transforming logic.
            Name = ("InternalInvocationProxy_" +
                    ArrayHelper.CreateArrayTypeName(KnownDataTypes.Boolean).TrimExtendedVhdlIdDelimiters())
                .ToExtendedVhdlId(),
        };

    private sealed class WaitContext
    {
        public DataObjectReference RunningStateVariableReference { get; set; }
        public DataObjectReference RunningIndexVariableReference { get; set; }
        public DataObjectReference TargetAvailableIndicatorVariableReference { get; set; }
        public int TargetComponentCount { get; set; }
        public string InvokerName { get; set; }
        public int InvokerIndex { get; set; }
        public string TargetMemberName { get; set; }
        public Dictionary<string, IArchitectureComponent> ComponentsByName { get; set; }
        public SizedDataType TargetAvailableIndicatorDataType { get; set; }
        public Case RunningStateCase { get; set; }
    }
}
