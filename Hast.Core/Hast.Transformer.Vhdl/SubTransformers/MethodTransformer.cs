using Hast.Common.Configuration;
using Hast.Transformer.Models;
using Hast.Transformer.Vhdl.ArchitectureComponents;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.VhdlBuilder.Representation.Expression;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Transformer.Vhdl.SubTransformers;

public class MethodTransformer : IMethodTransformer
{
    private readonly IMemberStateMachineFactory _memberStateMachineFactory;
    private readonly IStatementTransformer _statementTransformer;
    private readonly IDeclarableTypeCreator _declarableTypeCreator;

    public MethodTransformer(
        IMemberStateMachineFactory memberStateMachineFactory,
        IStatementTransformer statementTransformer,
        IDeclarableTypeCreator declarableTypeCreator)
    {
        _memberStateMachineFactory = memberStateMachineFactory;
        _statementTransformer = statementTransformer;
        _declarableTypeCreator = declarableTypeCreator;
    }

    public Task<IMemberTransformerResult> TransformAsync(MethodDeclaration method, IVhdlTransformationContext context) =>
        Task.Run(async () =>
        {
            if (method.Modifiers.HasFlag(Modifiers.Extern))
            {
                throw new InvalidOperationException(
                    $"The {nameof(method)} {method.GetFullName()} can't be transformed because it's extern. Only " +
                    $"managed code can be transformed.");
            }

            var stateMachineCount = context
                .GetTransformerConfiguration()
                .GetMaxInvocationInstanceCountConfigurationForMember(method).MaxInvocationInstanceCount;
            var stateMachineResults = new IArchitectureComponentResult[stateMachineCount];

            // Not much use to parallelize computation unless there are a lot of state machines to create or the method
            // is very complex. We'll need to examine when to parallelize here and determine it in runtime.
            if (stateMachineCount > 50)
            {
                var stateMachineComputingTasks = new List<Task<IArchitectureComponentResult>>();

                for (int i = 0; i < stateMachineCount; i++)
                {
                    var task = new Task<IArchitectureComponentResult>(
                        index => BuildStateMachineFromMethod(method, context, (int)index),
                        i);
                    task.Start();
                    stateMachineComputingTasks.Add(task);
                }

                stateMachineResults = await Task.WhenAll(stateMachineComputingTasks);
            }
            else
            {
                for (int i = 0; i < stateMachineCount; i++)
                {
                    stateMachineResults[i] = BuildStateMachineFromMethod(method, context, i);
                }
            }

            return (IMemberTransformerResult)new MemberTransformerResult
            {
                Member = method,
                IsHardwareEntryPointMember = method.IsHardwareEntryPointMember(),
                ArchitectureComponentResults = stateMachineResults,
            };
        });

    private IArchitectureComponentResult BuildStateMachineFromMethod(
        MethodDeclaration method,
        IVhdlTransformationContext context,
        int stateMachineIndex)
    {
        var methodFullName = method.GetFullName();
        var stateMachine = _memberStateMachineFactory
            .CreateStateMachine(ArchitectureComponentNameHelper.CreateIndexedComponentName(methodFullName, stateMachineIndex));

        // Adding the opening state's block.
        var openingBlock = new InlineBlock();

        // Handling the return type.
        var returnType = _declarableTypeCreator.CreateDeclarableType(method, method.ReturnType, context);
        // If the return type is a Task then that means it's one of the supported simple TPL scenarios, corresponding to
        // void in VHDL.
        if (returnType == SpecialTypes.Task) returnType = KnownDataTypes.Void;
        var isVoid = returnType.Name == "void";
        if (!isVoid)
        {
            stateMachine.InternallyDrivenSignals.Add(new Signal
            {
                Name = stateMachine.CreateReturnSignalReference().Name,
                DataType = returnType,
            });
        }

        // Handling in/out method parameters.
        var isFirstOutFlowingParameter = true;
        foreach (var parameter in method.GetNonSimpleMemoryParameters())
        {
            // Since input parameters are assigned to from the outside but they could be attempted to be also assigned
            // to from the inside (since in .NET a method argument can also be assigned to from inside the
            // method) we need to have intermediary input variables, then copy their values to local variables.

            var parameterDataType = _declarableTypeCreator.CreateDeclarableType(parameter, parameter.Type, context);
            var parameterSignalReference = stateMachine
                .CreateParameterSignalReference(parameter.Name, ParameterFlowDirection.In);
            var parameterLocalVariableReference = stateMachine.CreatePrefixedObjectName(parameter.Name).ToVhdlVariableReference();

            stateMachine.ExternallyDrivenSignals.Add(new ParameterSignal(
                methodFullName,
                parameter.Name,
                0,
                isOwn: true)
            {
                DataType = parameterDataType,
                Name = parameterSignalReference.Name,
            });

            stateMachine.LocalVariables.Add(new Variable
            {
                DataType = parameterDataType,
                Name = parameterLocalVariableReference.Name,
            });

            openingBlock.Add(new Assignment
            {
                AssignTo = parameterLocalVariableReference,
                Expression = parameterSignalReference,
            });

            // If the parameter can be modified inside and those changes should be passed back then we need to write the
            // local variables back to parameters.
            if (!parameter.IsOutFlowing()) continue;

            if (isFirstOutFlowingParameter)
            {
                isFirstOutFlowingParameter = false;

                stateMachine.States[1].Body.Add(new LineComment(
                    "Writing back out-flowing parameters so any changes made in this state machine will be reflected in the invoking one too."));
            }

            var outParameterSignalReference = stateMachine
                .CreateParameterSignalReference(parameter.Name, ParameterFlowDirection.Out);

            stateMachine.InternallyDrivenSignals.Add(new ParameterSignal(
                methodFullName,
                parameter.Name,
                0,
                isOwn: true)
            {
                DataType = parameterDataType,
                Name = outParameterSignalReference.Name,
            });

            stateMachine.States[1].Body.Add(new Assignment
            {
                AssignTo = outParameterSignalReference,
                Expression = parameterLocalVariableReference,
            });
        }

        // Processing method body.
        var bodyContext = new SubTransformerContext
        {
            TransformationContext = context,
            Scope = new SubTransformerScope
            {
                Method = method,
                StateMachine = stateMachine,
                CurrentBlock = new CurrentBlock(stateMachine, openingBlock, stateMachine.AddState(openingBlock)),
            },
        };

        var labels = method.Body.FindAllChildrenOfType<LabelStatement>();
        foreach (var label in labels.Select(label => label.Label))
        {
            bodyContext.Scope.LabelsToStateIndicesMappings[label] = stateMachine
                .AddState(new InlineBlock(new LineComment($"State for the label {label}.")));
        }

        var lastStatementIsReturn = false;
        foreach (var statement in method.Body.Statements)
        {
            _statementTransformer.Transform(statement, bodyContext);
            lastStatementIsReturn = statement is ReturnStatement;
        }

        // If the last statement was a return statement then there is already a state change to the final state added.
        if (!lastStatementIsReturn)
        {
            bodyContext.Scope.CurrentBlock.Add(stateMachine.ChangeToFinalState());
        }

        // We need to return the declarations and body here too so their computation can be parallelized too. Otherwise
        // we'd add them directly to context.Module.Architecture but that would need that collection to be thread-safe.
        var result = new ArchitectureComponentResult(stateMachine);

        // Warnings would be repeated for each instance of the state machine otherwise.
        if (stateMachineIndex == 0)
        {
            result.Warnings = bodyContext.Scope.Warnings;
        }

        return result;
    }
}
