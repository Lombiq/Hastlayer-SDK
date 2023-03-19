using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;
using System.Linq;

namespace Hast.Transformer.Services.ConstantValuesSubstitution;

internal static class ConstantValueSubstitutionHelper
{
    public static bool IsInWhile(AstNode node) => node.IsIn<WhileStatement>();

    public static bool IsInIfElse(AstNode node) => node.IsIn<IfElseStatement>();

    public static bool IsInWhileOrIfElse(AstNode node) => IsInWhile(node) || IsInIfElse(node);

    public static bool IsMethodInvocation(MemberReferenceExpression memberReferenceExpression) =>
        memberReferenceExpression.Parent.Is<InvocationExpression>(invocation => invocation.Target == memberReferenceExpression);

    public static MemberReferenceExpression FindMemberReferenceInConstructor(
        MethodDeclaration constructorDeclaration,
        string memberFullName,
        ITypeDeclarationLookupTable typeDeclarationLookupTable) =>
        constructorDeclaration
            .FindFirstChildOfType<MemberReferenceExpression>(m =>
                m.FindMemberDeclaration(typeDeclarationLookupTable, findLeftmostMemberIfRecursive: true)?.GetFullName() == memberFullName &&
                m.Target.Is<IdentifierExpression>(identifier => identifier.Identifier == "this"));

    public static ParameterDeclaration FindConstructorParameterForPassedExpression(
        ObjectCreateExpression objectCreateExpression,
        Expression passedExpression,
        ITypeDeclarationLookupTable typeDeclarationLookupTable) =>
        FindParameterForExpressionPassedToCall(
            objectCreateExpression,
            objectCreateExpression.Arguments,
            passedExpression,
            typeDeclarationLookupTable);

    public static ParameterDeclaration FindMethodParameterForPassedExpression(
        InvocationExpression invocationExpression,
        Expression passedExpression,
        ITypeDeclarationLookupTable typeDeclarationLookupTable) =>
        FindParameterForExpressionPassedToCall(
            invocationExpression,
            invocationExpression.Arguments,
            passedExpression,
            typeDeclarationLookupTable);

    // This could be optimized not to look up everything every time when called from VisitObjectCreateExpression() and
    // VisitInvocationExpression().
    private static ParameterDeclaration FindParameterForExpressionPassedToCall(
        Expression callExpression,
        AstNodeCollection<Expression> invocationArguments,
        Expression passedExpression,
        ITypeDeclarationLookupTable typeDeclarationLookupTable)
    {
        var methodReference = callExpression.GetResolveResult<InvocationResolveResult>();

        if (methodReference == null) return null;

        var targetFullName = methodReference.Member.GetFullName();

        var targetType = typeDeclarationLookupTable.Lookup(methodReference.Member.DeclaringType.GetFullName());

        // This can happen e.g. with SimpleMemory calls: the type is not transformed.
        if (targetType == null) return null;

        var parameters = ((MethodDeclaration)targetType
            .Members
            .Single(member => member.GetFullName() == targetFullName))
            .Parameters
            .ToList();

        var arguments = invocationArguments.ToList();
        var argumentIndex = arguments.FindIndex(argumentExpression => argumentExpression == passedExpression);

        if (argumentIndex == -1) return null;

        // Depending on whether a @this parameter was added to the method or used during invocation we need to adjust
        // the argument's index if there is a mismatch between the invocation and the method.
        if (parameters.Count < arguments.Count) argumentIndex--;
        else if (parameters.Count > arguments.Count) argumentIndex++;

        return parameters[argumentIndex];
    }
}
