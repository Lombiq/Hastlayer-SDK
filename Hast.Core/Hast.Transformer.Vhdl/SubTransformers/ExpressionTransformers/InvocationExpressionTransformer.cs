using Hast.Common.Configuration;
using Hast.Transformer.Models;
using Hast.Transformer.Vhdl.ArchitectureComponents;
using Hast.Transformer.Vhdl.Helpers;
using Hast.Transformer.Vhdl.Models;
using Hast.Transformer.Vhdl.SimpleMemory;
using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.VhdlBuilder.Representation.Expression;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Hast.Transformer.Vhdl.SubTransformers.ExpressionTransformers;

// SimpleMemory and member invocation transformation are factored out into two methods so the class has some structure,
// not to have one giant TransformInvocationExpression method.
public class InvocationExpressionTransformer : IInvocationExpressionTransformer
{
    private static readonly IEnumerable<string> _arrayCopyToMethodNames = typeof(Array)
        .GetMethods()
        .Where(method => method.Name == nameof(Array.Copy) && method.GetParameters().Length == 3)
        .Select(method => method.GetFullName());

    private readonly IStateMachineInvocationBuilder _stateMachineInvocationBuilder;
    private readonly ITypeConverter _typeConverter;
    private readonly ISpecialOperationInvocationTransformer _specialOperationInvocationTransformer;
    private readonly ITypeConversionTransformer _typeConversionTransformer;

    public InvocationExpressionTransformer(
        IStateMachineInvocationBuilder stateMachineInvocationBuilder,
        ITypeConverter typeConverter,
        ISpecialOperationInvocationTransformer specialOperationInvocationTransformer,
        ITypeConversionTransformer typeConversionTransformer)
    {
        _stateMachineInvocationBuilder = stateMachineInvocationBuilder;
        _typeConverter = typeConverter;
        _specialOperationInvocationTransformer = specialOperationInvocationTransformer;
        _typeConversionTransformer = typeConversionTransformer;
    }

    public IVhdlElement TransformInvocationExpression(
        InvocationExpression expression,
        ICollection<TransformedInvocationParameter> transformedParameters,
        SubTransformerContext context)
    {
        var targetMemberReference = expression.Target as MemberReferenceExpression;

        // This is a SimpleMemory access.
        return expression.IsSimpleMemoryInvocation()
            ? TransformSimpleMemoryInvocation(expression, transformedParameters, targetMemberReference, context)
            : TransformMemberInvocation(expression, transformedParameters, targetMemberReference, context);
    }

    private IVhdlElement TransformSimpleMemoryInvocation(
        InvocationExpression expression,
        IEnumerable<TransformedInvocationParameter> transformedParameters,
        MemberReferenceExpression targetMemberReference,
        SubTransformerContext context)
    {
        var stateMachine = context.Scope.StateMachine;
        var currentBlock = context.Scope.CurrentBlock;
        var customProperties = context.Scope.CustomProperties;

        const string lastWriteFinishedKey = "SimpleMemory.LastWriteFinsihedStateIndex";
        const string lastReadFinishedKey = "SimpleMemory.LastReadFinsihedStateIndex";

        stateMachine.AddSimpleMemorySignalsIfNew();

        var memberName = targetMemberReference.MemberName;

        var isWrite = memberName.StartsWithOrdinal("Write");
        var invocationParameters = transformedParameters.ToList();

        var operationPropertyKey = isWrite ? lastWriteFinishedKey : lastReadFinishedKey;
        if (customProperties.ContainsKey(operationPropertyKey) &&
            customProperties[operationPropertyKey] == currentBlock.StateMachineStateIndex)
        {
            var operationNoun = isWrite ? "write" : "read";
            currentBlock.Add(new LineComment(
                "The last SimpleMemory " + operationNoun + " just finished, so need to start the next one in the next state."));

            stateMachine.AddNewStateAndChangeCurrentBlock(context);
        }

        currentBlock.Add(isWrite ? new LineComment("Begin SimpleMemory write.") : new LineComment("Begin SimpleMemory read."));

        // Setting CellIndex.
        currentBlock.Add(new Assignment
        {
            AssignTo = stateMachine.CreateSimpleMemoryCellIndexSignalReference(),
            // CellIndex is conventionally the first invocation parameter.
            Expression = _typeConversionTransformer.ImplementTypeConversion(
                invocationParameters[0].DataType,
                SimpleMemoryTypes.CellIndexInternalSignalDataType,
                invocationParameters[0].Reference)
                .ConvertedFromExpression,
        });

        // Setting the write/read enable signal.
        var enableSignalReference = isWrite ?
            stateMachine.CreateSimpleMemoryWriteEnableSignalReference() :
            stateMachine.CreateSimpleMemoryReadEnableSignalReference();
        currentBlock.Add(new Assignment
        {
            AssignTo = enableSignalReference,
            Expression = Value.True,
        });

        var is4BytesOperation = targetMemberReference.MemberName.EndsWithOrdinal("4Bytes");
        var operationDataTypeName = memberName
            .Replace("Write", string.Empty, StringComparison.InvariantCulture)
            .Replace("Read", string.Empty, StringComparison.InvariantCulture);

        TransformSimpleMemoryInvocationWrite(
            expression,
            context,
            is4BytesOperation,
            invocationParameters,
            currentBlock,
            operationDataTypeName,
            memberName);

        // The memory operation should be initialized in this state, then finished in another one.
        var memoryOperationFinishedBlock = new InlineBlock();
        var endMemoryOperationBlock = new InlineBlock(
            new LineComment("Waiting for the SimpleMemory operation to finish."),
            new IfElse
            {
                Condition = new Binary
                {
                    Left = (isWrite ? SimpleMemoryPortNames.WritesDone : SimpleMemoryPortNames.ReadsDone)
                        .ToExtendedVhdlId()
                        .ToVhdlSignalReference(),
                    Operator = BinaryOperator.Equality,
                    Right = Value.True,
                },
                True = memoryOperationFinishedBlock,
            });
        var memoryOperationFinishedStateIndex = stateMachine.AddState(endMemoryOperationBlock);

        memoryOperationFinishedBlock.Add(new Assignment
        {
            AssignTo = enableSignalReference,
            Expression = Value.False,
        });

        currentBlock.Add(stateMachine.CreateStateChange(memoryOperationFinishedStateIndex));

        if (isWrite)
        {
            memoryOperationFinishedBlock.Body.Insert(0, new LineComment("SimpleMemory write finished."));

            currentBlock.ChangeBlockToDifferentState(memoryOperationFinishedBlock, memoryOperationFinishedStateIndex);
            customProperties[lastWriteFinishedKey] = memoryOperationFinishedStateIndex;

            return Empty.Instance;
        }

        memoryOperationFinishedBlock.Body.Insert(0, new LineComment("SimpleMemory read finished."));

        currentBlock.ChangeBlockToDifferentState(memoryOperationFinishedBlock, memoryOperationFinishedStateIndex);
        customProperties[lastReadFinishedKey] = memoryOperationFinishedStateIndex;

        var dataInTemporaryVariableReference = stateMachine
                .CreateVariableWithNextUnusedIndexedName("dataIn", SimpleMemoryTypes.DataSignalsDataType)
                .ToReference();
        currentBlock.Add(new Assignment
        {
            AssignTo = dataInTemporaryVariableReference,
            Expression = SimpleMemoryPortNames.DataIn.ToExtendedVhdlId().ToVhdlSignalReference(),
        });

        return ImplementSimpleMemoryTypeConversion(
            dataInTemporaryVariableReference,
            directionIsLogicVectorToType: true,
            operationDataTypeName,
            invocationParameters,
            is4BytesOperation,
            memberName);
    }

    private void TransformSimpleMemoryInvocationWrite(
        InvocationExpression expression,
        SubTransformerContext context,
        bool is4BytesOperation,
        List<TransformedInvocationParameter> invocationParameters,
        CurrentBlock currentBlock,
        string operationDataTypeName,
        string memberName)
    {
        var isWrite = memberName.StartsWithOrdinal("Write");
        if (!isWrite) return;

        var stateMachine = context.Scope.StateMachine;

        var dataOutReference = stateMachine.CreateSimpleMemoryDataOutSignalReference();
        if (is4BytesOperation)
        {
            var arrayReference = (IDataObject)invocationParameters[1].Reference;

            void AddSlice(int elementIndex) =>
                currentBlock.Add(new Assignment
                {
                    AssignTo = new ArraySlice
                    {
                        ArrayReference = dataOutReference,
                        IsDownTo = true,
                        IndexFrom = ((elementIndex + 1) * 8) - 1,
                        IndexTo = elementIndex * 8,
                    },
                    // The data to write is conventionally the second parameter.
                    Expression = new Invocation(
                        "std_logic_vector",
                        new ArrayElementAccess
                        {
                            ArrayReference = arrayReference,
                            IndexExpression = elementIndex.ToVhdlValue(KnownDataTypes.UnrangedInt),
                        }),
                });

            // Arrays smaller than 4 elements can be written with Write4Bytes(), so need to take care of them.
            var arrayLength = context
                .TransformationContext
                .ArraySizeHolder
                .GetSizeOrThrow(expression.Arguments.Skip(1).First())
                .Length;
            for (int i = 0; i < arrayLength; i++)
            {
                AddSlice(i);
            }
        }
        else
        {
            currentBlock.Add(new Assignment
            {
                AssignTo = dataOutReference,
                // The data to write is conventionally the second parameter.
                Expression = ImplementSimpleMemoryTypeConversion(
                    invocationParameters[1].Reference,
                    directionIsLogicVectorToType: false,
                    operationDataTypeName,
                    invocationParameters,
                    is4BytesOperation,
                    memberName),
            });
        }
    }

    private IVhdlElement ImplementSimpleMemoryTypeConversion(
        IVhdlElement variableToConvert,
        bool directionIsLogicVectorToType,
        string operationDataTypeName,
        List<TransformedInvocationParameter> invocationParameters,
        bool is4BytesOperation,
        string memberName)
    {
        // Using the built-in conversion functions to handle known data types.
        if (operationDataTypeName is "UInt32" or "Int32" or "Boolean" or "Char")
        {
            string dataConversionInvocationTarget;
            if (directionIsLogicVectorToType)
            {
                dataConversionInvocationTarget = "ConvertStdLogicVectorTo" + operationDataTypeName;
            }
            else
            {
                dataConversionInvocationTarget = "Convert" + operationDataTypeName + "ToStdLogicVector";

                DataType operationDataType = KnownDataTypes.Int32;
                if (operationDataTypeName == "UInt32") operationDataType = KnownDataTypes.UInt32;
                else if (operationDataTypeName == "Boolean") operationDataType = KnownDataTypes.Boolean;
                else if (operationDataTypeName == "Char") operationDataType = KnownDataTypes.Character;

                var invocationDataType = invocationParameters[1].DataType;
                // The two data types should be the case almost all the time but sometimes a type conversion is needed.
                if (invocationDataType != operationDataType)
                {
                    var conversionResult = _typeConversionTransformer
                        .ImplementTypeConversion(invocationDataType, operationDataType, variableToConvert);

                    variableToConvert = conversionResult.ConvertedFromExpression;
                }
            }

            return new Invocation(dataConversionInvocationTarget, variableToConvert);
        }

        if (is4BytesOperation)
        {
            Invocation CreateSlice(int indexFrom, int indexTo) =>
                new(
                    "unsigned",
                    new ArraySlice
                    {
                        ArrayReference = (IDataObject)variableToConvert,
                        IsDownTo = true,
                        IndexFrom = indexFrom,
                        IndexTo = indexTo,
                    });

            return new Value
            {
                DataType = ArrayHelper.CreateArrayInstantiation(KnownDataTypes.UInt8, 4),
                EvaluatedContent = new InlineBlock(
                    CreateSlice(7, 0), CreateSlice(15, 8), CreateSlice(23, 16), CreateSlice(31, 24)),
            };
        }

        throw new InvalidOperationException($"Invalid SimpleMemory operation: {memberName}.");
    }

    private IVhdlElement TransformMemberInvocation(
        InvocationExpression expression,
        ICollection<TransformedInvocationParameter> transformedParameters,
        MemberReferenceExpression targetMemberReference,
        SubTransformerContext context)
    {
        var targetMethodName = expression.GetTargetMemberFullName();

        // This is a Task.FromResult() method call.
        if (targetMethodName.IsTaskFromResultMethodName())
        {
            return transformedParameters.Single().Reference;
        }

        // This is a Task.Wait() call so needs special care.
        if (HandleTaskWait(targetMethodName, expression, context) is (true, { } resultBlock))
        {
            return resultBlock;
        }

        // Handling special operations here.
        if (_specialOperationInvocationTransformer.IsSupported(expression))
        {
            return _specialOperationInvocationTransformer.TransformSpecialOperationInvocation(
                expression,
                transformedParameters.Select(transformedParameter => transformedParameter.Reference),
                context);
        }

        // Support for Array.Copy().
        if (_arrayCopyToMethodNames.Contains(targetMethodName))
        {
            return TransformArrayCopy(expression, context, transformedParameters);
        }

        EntityDeclaration targetDeclaration = null;

        // Is this a reference to a member of the parent class from a compiler-generated DisplayClass? These look like
        // following: this.<>4__this.IsPrimeNumberInternal()
        if (targetMemberReference.Target is MemberReferenceExpression referenceExpression)
        {
            var targetTargetFullName = referenceExpression.GetFullName();
            if (targetTargetFullName.IsDisplayOrClosureClassMemberName())
            {
                // We need to find the corresponding member in the parent class of this expression's class.
                targetDeclaration = expression
                    .FindFirstParentTypeDeclaration() // This is the level of the DisplayClass.
                    .FindFirstParentTypeDeclaration() // The parent class of the DisplayClass.
                    .Members
                    .Single(member => member.GetFullName() == targetMethodName);
            }
        }
        else
        {
            targetDeclaration = targetMemberReference.FindMemberDeclaration(context.TransformationContext.TypeDeclarationLookupTable);
        }

        if (targetDeclaration is not MethodDeclaration)
        {
            throw new InvalidOperationException(
                $"The invoked method {targetMethodName} can't be found and thus can't be transformed. Did you " +
                $"forget to add an assembly to the list of the assemblies to generate hardware from?"
                    .AddParentEntityName(expression));
        }

        var methodDeclaration = (MethodDeclaration)targetDeclaration;

        var buildInvocationResult = _stateMachineInvocationBuilder
            .BuildInvocation(methodDeclaration, transformedParameters, 1, context);
        var invocationWait = _stateMachineInvocationBuilder.BuildSingleInvocationWait(methodDeclaration, 0, context);
        context.Scope.CurrentBlock.Add(new InlineBlock(buildInvocationResult.OutParameterBackAssignments));
        return invocationWait;
    }

    private (bool HasResult, InlineBlock ResultBlock) HandleTaskWait(
        string targetMethodName,
        InvocationExpression expression,
        SubTransformerContext context)
    {
        var empty = (HasResult: false, ResultBlock: (InlineBlock)null);
        if (targetMethodName != "System.Void System.Threading.Tasks.Task::Wait()") return empty;

        // Tasks aren't awaited where they're started so we only need to await the already started state machines here.
        var waitTarget = ((MemberReferenceExpression)expression.Target).Target;

        // Is it a Task.Something().Wait() call?
        MemberReferenceExpression memberReference = null;
        if (!waitTarget.Is<InvocationExpression>(invocation =>
            invocation.Target.Is(
                member =>
                    member.Target.Is<TypeReferenceExpression>(type =>
                        _typeConverter.ConvertAstType(
                            type.Type,
                            context.TransformationContext) == SpecialTypes.Task),
                out memberReference)))
        {
            return empty;
        }

        var memberName = memberReference.MemberName;
        if (memberName is not (nameof(Task.WhenAll) or nameof(Task.WhenAny))) return empty;

        // Since it's used in a WhenAll() or WhenAny() call the argument should be an array.
        var taskArrayIdentifier =
            ((IdentifierExpression)((InvocationExpression)waitTarget).Arguments.Single()).Identifier;

        // This array originally stored the Task<T> objects but now is just for the results, so we have to move the
        // results to its elements.
        if (!context.Scope.TaskVariableNameToDisplayClassMethodMappings.TryGetValue(taskArrayIdentifier, out var targetMethod))
        {
            throw new InvalidOperationException(
                $"You declared a Task array with the name \"{taskArrayIdentifier}\" but didn't actually start " +
                $"any tasks. Temporarily remove/comment out the array if you'll only use it in the future."
                    .AddParentEntityName(expression));
        }

        var resultReferences = _stateMachineInvocationBuilder.BuildMultiInvocationWait(
            targetMethod,
            context
                .TransformationContext
                .GetTransformerConfiguration()
                .GetMaxInvocationInstanceCountConfigurationForMember(targetMethod)
                .MaxDegreeOfParallelism,
            memberName == nameof(Task.WhenAll),
            context);

        var index = 0;
        var stateMachine = context.Scope.StateMachine;
        var arrayReference = stateMachine.CreatePrefixedObjectName(taskArrayIdentifier).ToVhdlVariableReference();
        var resultBlock = new InlineBlock();
        foreach (var resultReference in resultReferences)
        {
            resultBlock.Add(
                new Assignment
                {
                    AssignTo = new ArrayElementAccess
                    {
                        ArrayReference = arrayReference,
                        IndexExpression = index.ToVhdlValue(KnownDataTypes.UnrangedInt),
                    },
                    Expression = resultReference,
                });
            index++;
        }

        return (HasResult: true, ResultBlock: resultBlock);
    }

    private static IVhdlElement TransformArrayCopy(
        InvocationExpression expression,
        SubTransformerContext context,
        ICollection<TransformedInvocationParameter> transformedParameters)
    {
        IDataObject sourceArrayReference;
        var targetArrayLength = context.TransformationContext.ArraySizeHolder
            .GetSizeOrThrow(expression.Arguments.Skip(1).First()).Length;
        int sourceArrayLength;

        if (expression.Arguments.Skip(2).Single().Is<PrimitiveExpression>(out var lengthExpression))
        {
            if (lengthExpression.Value.ToString() == "0")
            {
                // If the whole array is copied then it can be transformed into a simple assignment.
                sourceArrayReference = (IDataObject)transformedParameters.First().Reference;
                sourceArrayLength = context.TransformationContext.ArraySizeHolder
                    .GetSizeOrThrow(expression.Arguments.First()).Length;
            }
            else
            {
                sourceArrayLength = Convert.ToInt32(lengthExpression.Value, CultureInfo.InvariantCulture);

                // Otherwise slicing the array.
                sourceArrayReference = new ArraySlice
                {
                    ArrayReference = (IDataObject)transformedParameters.First().Reference,
                    IndexFrom = 0,
                    IndexTo = sourceArrayLength - 1,
                };
            }
        }
        else
        {
            throw new NotSupportedException(
                "Array.Copy() is only supported if the second argument can be determined compile-time."
                .AddParentEntityName(expression));
        }

        var targetArrayReference = (IDataObject)transformedParameters.Skip(1).First().Reference;

        if (targetArrayLength > sourceArrayLength)
        {
            targetArrayReference = new ArraySlice
            {
                ArrayReference = targetArrayReference,
                IndexFrom = 0,
                IndexTo = sourceArrayLength - 1,
            };
        }
        else if (sourceArrayLength > targetArrayLength)
        {
            sourceArrayReference = new ArraySlice
            {
                ArrayReference = sourceArrayReference,
                IndexFrom = 0,
                IndexTo = targetArrayLength - 1,
            };
        }

        return new Assignment
        {
            AssignTo = targetArrayReference,
            Expression = sourceArrayReference,
        };
    }
}
