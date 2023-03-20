using Hast.Common.Services;
using Hast.Layer;
using Hast.Transformer.Helpers;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Services;

/// <summary>
/// Converts usages of <see cref="System.Collections.Immutable.ImmutableArray"/> to standard arrays. This is to make
/// transformation easier, because the compile-time .NET checks being already done there is no need to handle such
/// arrays differently.
/// </summary>
public class ImmutableArraysToStandardArraysConverter : IConverter
{
    private readonly IHashProvider _hashProvider;
    public IEnumerable<string> Dependencies { get; } = new[] { nameof(ReadonlyToConstConverter) };

    public ImmutableArraysToStandardArraysConverter(IHashProvider hashProvider) => _hashProvider = hashProvider;

    public void Convert(
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable) =>
        // Note that ImmutableArrays can only be single-dimensional in themselves so the whole code here is built around
        // that assumption (though it's possible to create ImmutableArrays of ImmutableArrays, but this is not supported
        // yet).

        // This implementation is partial, to the extent needed for unum and posit support. More value holders, like
        // fields, could also be handled for example.

        syntaxTree.AcceptVisitor(new ImmutableArraysToStandardArraysConvertingVisitor(_hashProvider, knownTypeLookupTable));

    private sealed class ImmutableArraysToStandardArraysConvertingVisitor : DepthFirstAstVisitor
    {
        private readonly IHashProvider _hashProvider;
        private readonly IKnownTypeLookupTable _knownTypeLookupTable;

        public ImmutableArraysToStandardArraysConvertingVisitor(
            IHashProvider hashProvider,
            IKnownTypeLookupTable knownTypeLookupTable)
        {
            _hashProvider = hashProvider;
            _knownTypeLookupTable = knownTypeLookupTable;
        }

        public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
        {
            base.VisitPropertyDeclaration(propertyDeclaration);

            var type = propertyDeclaration.GetActualType();

            if (!IsImmutableArray(type)) return;

            var member = propertyDeclaration.GetMemberResolveResult().Member;
            // Re-wiring types to use a standard array instead.
            var arrayType = CreateArrayType(type.TypeArguments.Single(), member);

            propertyDeclaration.ReturnType = CreateArrayAstTypeFromImmutableArrayAstType(propertyDeclaration.ReturnType, arrayType);
            propertyDeclaration.RemoveAnnotations<ILVariableResolveResult>();
            propertyDeclaration.AddAnnotation(new MemberResolveResult(
                propertyDeclaration.FindFirstParentTypeDeclaration().GetResolveResult(), member, arrayType));

            ThrowIfMultipleMembersWithTheNameExist(propertyDeclaration);
        }

        public override void VisitInvocationExpression(InvocationExpression invocationExpression)
        {
            base.VisitInvocationExpression(invocationExpression);

            // Handling the various ImmutableArray methods here.

            var memberReference = invocationExpression.Target as MemberReferenceExpression;

            var invocationType = invocationExpression.GetActualType();

            IEnumerable<IMember> GetArrayMembers() => _knownTypeLookupTable.Lookup(KnownTypeCode.Array).GetMembers();

            MemberReferenceExpression CreateArrayLengthExpression() =>
                new MemberReferenceExpression(memberReference.Target.Clone(), "Length")
                    .WithAnnotation(new MemberResolveResult(
                        memberReference.Target.GetResolveResult(),
                        GetArrayMembers().Single(member => member.Name == "Length")));

            InvocationExpression CreateArrayCopyExpression(Expression destinationValueHolder, MemberReferenceExpression arrayLengthExpression)
            {
                var arrayType = _knownTypeLookupTable.Lookup(KnownTypeCode.Array);
                return new InvocationExpression(
                    new MemberReferenceExpression(
                        new TypeReferenceExpression(new SimpleType("System.Array").WithAnnotation(arrayType.ToResolveResult()))
                            .WithAnnotation(new TypeResolveResult(arrayType)),
                        "Copy"),
                    memberReference.Target.Clone(),
                    destinationValueHolder.Clone(),
                    arrayLengthExpression.Clone())
                .WithAnnotation(new MemberResolveResult(
                    memberReference.Target.GetResolveResult(),
                    GetArrayMembers().Single(member =>
                        {
                            var parameters = (member as IMethod)?.Parameters;
                            return member.Name == "Copy" &&
                                parameters?.Count == 3
                                && parameters[2].Type.FullName == "System.Int32";
                        })));
            }

            if (!IsImmutableArray(invocationType) || memberReference == null)
            {
                var invocationResolveResult = invocationExpression.GetResolveResult<InvocationResolveResult>();

                if (invocationResolveResult != null &&
                    IsImmutableArray(invocationResolveResult.TargetResult.Type) &&
                    invocationResolveResult.Member.Name == "CopyTo")
                {
                    invocationExpression.ReplaceWith(CreateArrayCopyExpression(
                        invocationExpression.Arguments.Single(),
                        CreateArrayLengthExpression()));
                }
            }
            else
            {
                if (memberReference.MemberName == "CreateRange")
                {
                    // ImmutableArray.CreateRange() can be substituted with an array assignment. Since the array won't
                    // be changed (because it's immutable in .NET) this is not dangerous.
                    if (invocationExpression.Arguments.Count > 1)
                    {
                        throw new NotSupportedException(
                            "Only the ImmutableArray.CreateRange() overload with a single argument is supported. The supplied expression was: " +
                            invocationExpression.ToString().AddParentEntityName(memberReference));
                    }

                    invocationExpression.ReplaceWith(invocationExpression.Arguments.Single().Clone());
                }
                else if (memberReference.MemberName == "SetItem")
                {
                    // SetItem can be converted into a simple array element assignment to a newly created copy of the
                    // array.

                    var elementType = invocationType.TypeArguments.Single();
                    var elementAstType = TypeHelper.CreateAstType(elementType);
                    var parentStatement = invocationExpression.FindFirstParentStatement();
                    var arrayType = CreateArrayType(elementType);

                    var variableIdentifier = VariableHelper.DeclareAndReferenceArrayVariable(
                        memberReference.Target,
                        elementAstType,
                        arrayType,
                        _hashProvider);
                    var arrayLengthExpression = CreateArrayLengthExpression();

                    var arrayCreate = new ArrayCreateExpression { Type = elementAstType };
                    arrayCreate.Arguments.Add(arrayLengthExpression);
                    arrayCreate.AddAnnotation(new ArrayCreateResolveResult(
                        arrayType,
                        new[] { arrayLengthExpression.GetResolveResult() }.ToList(),
                        Enumerable.Empty<ResolveResult>().ToList()));
                    var arrayCreateAssignment = new AssignmentExpression(
                        variableIdentifier,
                        arrayCreate);
                    AstInsertionHelper.InsertStatementBefore(parentStatement, new ExpressionStatement(arrayCreateAssignment));

                    AstInsertionHelper.InsertStatementBefore(
                        parentStatement,
                        new ExpressionStatement(CreateArrayCopyExpression(variableIdentifier, arrayLengthExpression)));

                    var valueArgument = invocationExpression.Arguments.Skip(1).Single();

                    var assignment = new AssignmentExpression(
                        new IndexerExpression(variableIdentifier.Clone(), invocationExpression.Arguments.First().Clone()),
                        valueArgument.Clone())
                        .WithAnnotation(new OperatorResolveResult(
                            valueArgument.GetActualType(),
                            System.Linq.Expressions.ExpressionType.Assign,
                            memberReference.Target.GetResolveResult(),
                            valueArgument.GetResolveResult()));

                    AstInsertionHelper.InsertStatementBefore(parentStatement, new ExpressionStatement(assignment));

                    invocationExpression.ReplaceWith(variableIdentifier.Clone());
                }
                else if (memberReference.MemberName == "Create")
                {
                    // ImmutableArray.Create() just creates an empty array.

                    var elementAstType = memberReference.TypeArguments.Single();

                    var arrayCreate = new ArrayCreateExpression { Type = elementAstType.Clone() };
                    var sizeExpression = new PrimitiveExpression(0)
                        .WithAnnotation(_knownTypeLookupTable.Lookup(KnownTypeCode.Int32).ToResolveResult());
                    arrayCreate.Arguments.Add(sizeExpression);

                    arrayCreate.AddAnnotation(new ArrayCreateResolveResult(
                        CreateArrayType(elementAstType.GetActualType()),
                        new[] { sizeExpression.GetResolveResult() }.ToList(),
                        Enumerable.Empty<ResolveResult>().ToList()));

                    invocationExpression.ReplaceWith(arrayCreate);
                }
                else
                {
                    throw new NotSupportedException(
                        "The member \"" + memberReference.MemberName +
                        "\" is not supported on ImmutableArray. The supplied expression was: " +
                        invocationExpression.ToString().AddParentEntityName(memberReference));
                }
            }
        }

        public override void VisitParameterDeclaration(ParameterDeclaration parameterDeclaration)
        {
            base.VisitParameterDeclaration(parameterDeclaration);

            ProcessIfIsImmutableArray(parameterDeclaration, arrayType =>
            {
                parameterDeclaration.Type = CreateArrayAstTypeFromImmutableArrayAstType(parameterDeclaration.Type, arrayType);
                parameterDeclaration.AddAnnotation(VariableHelper.CreateILVariableResolveResult(
                    VariableKind.Parameter,
                    arrayType,
                    parameterDeclaration.Name));

                ThrowIfMultipleMembersWithTheNameExist(parameterDeclaration.FindFirstParentEntityDeclaration());
            });
        }

        public override void VisitConditionalExpression(ConditionalExpression conditionalExpression)
        {
            base.VisitConditionalExpression(conditionalExpression);

            ProcessIfIsImmutableArray(conditionalExpression, arrayType =>
                conditionalExpression.AddAnnotation(arrayType.ToResolveResult()));
        }

        public override void VisitIdentifierExpression(IdentifierExpression identifierExpression)
        {
            base.VisitIdentifierExpression(identifierExpression);

            ProcessIfIsImmutableArray(identifierExpression, arrayType =>
                identifierExpression.AddAnnotation(VariableHelper.CreateILVariableResolveResult(
                    VariableKind.Parameter,
                    arrayType,
                    identifierExpression.Identifier)));
        }

        public override void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
        {
            base.VisitMemberReferenceExpression(memberReferenceExpression);

            var originalResolveResult = memberReferenceExpression.GetMemberResolveResult();

            ProcessIfIsImmutableArray(memberReferenceExpression, arrayType =>
                memberReferenceExpression.AddAnnotation(new MemberResolveResult(
                    originalResolveResult.TargetResult,
                    originalResolveResult.Member,
                    arrayType)));
        }

        public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            base.VisitAssignmentExpression(assignmentExpression);

            ProcessIfIsImmutableArray(assignmentExpression, arrayType =>
                assignmentExpression.AddAnnotation(new OperatorResolveResult(
                    arrayType,
                    System.Linq.Expressions.ExpressionType.Assign,
                    assignmentExpression.Left.GetResolveResult(),
                    assignmentExpression.Right.GetResolveResult())));
        }

        public override void VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
        {
            base.VisitVariableDeclarationStatement(variableDeclarationStatement);

            ProcessIfIsImmutableArray(variableDeclarationStatement, arrayType =>
            {
                variableDeclarationStatement.Type = CreateArrayAstTypeFromImmutableArrayAstType(variableDeclarationStatement.Type, arrayType);

                var variable = variableDeclarationStatement.Variables.Single();
                variable.AddAnnotation(VariableHelper.CreateILVariableResolveResult(
                    VariableKind.Parameter,
                    arrayType,
                    variable.Name));
            });
        }

        private static void ProcessIfIsImmutableArray<T>(T node, Action<ArrayType> processor)
            where T : AstNode
        {
            var type = node.GetActualType();

            if (!IsImmutableArray(type)) return;

            node.RemoveAnnotations<ResolveResult>();
            processor(CreateArrayType(type.TypeArguments.Single()));
        }

        private static bool IsImmutableArray(IType type) =>
            type?.GetFullName().StartsWithOrdinal("System.Collections.Immutable.ImmutableArray") == true;

        private static ArrayType CreateArrayType(IType elementType, ICompilationProvider compilationProvider = null) =>
            new(compilationProvider?.Compilation ?? ((ICompilationProvider)elementType).Compilation, elementType);

        private static AstType GetClonedElementTypeFromImmutableArrayAstType(AstType astType) =>
            ((SimpleType)astType).TypeArguments.Single().Clone();

        private static ComposedType CreateArrayAstTypeFromImmutableArrayAstType(AstType astType, ArrayType arrayType)
        {
            var arrayAstType = new ComposedType
            {
                BaseType = GetClonedElementTypeFromImmutableArrayAstType(astType),
            };
            arrayAstType.ArraySpecifiers.Add(new ArraySpecifier(1));
            arrayAstType.AddAnnotation(arrayType.ToResolveResult());
            return arrayAstType;
        }

        private static void ThrowIfMultipleMembersWithTheNameExist(EntityDeclaration entityDeclaration)
        {
            var fullName = entityDeclaration.GetFullName();
            if (entityDeclaration.FindFirstParentTypeDeclaration().Members.Count(member => member.GetFullName() == fullName) > 1)
            {
                throw new NotSupportedException(
                    $"ImmutableArrays are converted into standard arrays. After such conversions a new member " +
                    $"with the signature {fullName} was created, tough a previously existing member has the " +
                    $"same signature. Change the members so even after converting ImmutableArrays they will " +
                    $"have unique signatures. The full declaration of the converted member: " +
                    $"{Environment.NewLine}{entityDeclaration}");
            }
        }
    }
}
