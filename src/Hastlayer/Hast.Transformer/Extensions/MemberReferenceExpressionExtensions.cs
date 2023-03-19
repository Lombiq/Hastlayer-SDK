using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Resolver;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;
using System.Linq;

namespace ICSharpCode.Decompiler.CSharp.Syntax;

public static class MemberReferenceExpressionExtensions
{
    /// <summary>
    /// Find the referenced member's declaration.
    /// </summary>
    /// <param name="typeDeclarationLookupTable">
    /// The <see cref="ITypeDeclarationLookupTable"/> instance corresponding to the current scope.
    /// </param>
    /// <param name="findLeftmostMemberIfRecursive">
    /// If the member reference references another member (like <c>this.Property1.Property2.Property3</c>) then if set
    /// to <see langword="true"/> the member corresponding to the leftmost member ( <c>this.Property1</c> in this case)
    /// will be looked up.
    /// </param>
    public static EntityDeclaration FindMemberDeclaration(
        this MemberReferenceExpression memberReferenceExpression,
        ITypeDeclarationLookupTable typeDeclarationLookupTable,
        bool findLeftmostMemberIfRecursive = false)
    {
        TypeDeclaration type;

        if (memberReferenceExpression.Target is MemberReferenceExpression)
        {
            if (findLeftmostMemberIfRecursive)
            {
                return memberReferenceExpression.Target
                    .As<MemberReferenceExpression>()
                    .FindMemberDeclaration(typeDeclarationLookupTable, findLeftmostMemberIfRecursive: true);
            }

            type = typeDeclarationLookupTable.Lookup(memberReferenceExpression.Target.GetActualTypeFullName());
        }
        else
        {
            type = memberReferenceExpression.FindTargetTypeDeclaration(typeDeclarationLookupTable);
        }

        if (type == null) return null;

        var fullName = memberReferenceExpression.GetReferencedMemberFullName();

        if (string.IsNullOrEmpty(fullName)) return null;

        var isPropertyReference = memberReferenceExpression.IsPropertyReference();
        return type
            .Members
            .SingleOrDefault(member =>
            {
                if (member.GetFullName() == fullName) return true;

                if (isPropertyReference)
                {
                    var property = member.GetMemberResolveResult()?.Member as IProperty;
                    return property?.Getter?.GetFullName() == fullName || property?.Setter?.GetFullName() == fullName;
                }

                return false;
            });
    }

    public static TypeDeclaration FindTargetTypeDeclaration(
        this MemberReferenceExpression memberReferenceExpression,
        ITypeDeclarationLookupTable typeDeclarationLookupTable)
    {
        var target = memberReferenceExpression.Target;

        if (target is TypeReferenceExpression typeReferenceExpression)
        {
            // The member is in a different class.
            return typeDeclarationLookupTable.Lookup(typeReferenceExpression);
        }

        if (target is BaseReferenceExpression)
        {
            // The member is in the base class (because of single class inheritance in C#, there can be only one base
            // class).
            return memberReferenceExpression.FindFirstParentTypeDeclaration().BaseTypes
                .Select(type => typeDeclarationLookupTable.Lookup(type))
                .SingleOrDefault(typeDeclaration => typeDeclaration != null && typeDeclaration.ClassType == ClassType.Class);
        }

        if (target is IdentifierExpression or IndexerExpression)
        {
            var type = target.GetActualType();
            return type == null ? null : typeDeclarationLookupTable.Lookup(type.GetFullName());
        }

        if (target is MemberReferenceExpression)
        {
            return target.As<MemberReferenceExpression>().FindTargetTypeDeclaration(typeDeclarationLookupTable);
        }

        if (target is ObjectCreateExpression expression)
        {
            // The member is referenced in an object initializer.
            return typeDeclarationLookupTable.Lookup(expression.Type);
        }

        if (target is InvocationExpression)
        {
            var memberResolveResult = memberReferenceExpression.GetMemberResolveResult();
            if (memberResolveResult != null)
            {
                return typeDeclarationLookupTable.Lookup(memberResolveResult.Member.DeclaringType.GetFullName());
            }
        }

        // The member is within this class.
        return memberReferenceExpression.FindFirstParentTypeDeclaration();
    }

    public static string GetMemberFullName(this MemberReferenceExpression memberReferenceExpression) =>
        memberReferenceExpression.GetReferencedMemberFullName();

    /// <summary>
    /// Determines if the member reference is an access to an array's Length property.
    /// </summary>
    public static bool IsArrayLengthAccess(this MemberReferenceExpression memberReferenceExpression) =>
        memberReferenceExpression.MemberName == "Length" && memberReferenceExpression.Target.GetActualType().IsArray();

    public static bool IsTaskStartNew(this MemberReferenceExpression memberReferenceExpression) =>
        memberReferenceExpression.MemberName == "StartNew" &&
        memberReferenceExpression.Target.GetActualTypeFullName() == typeof(System.Threading.Tasks.TaskFactory).FullName;

    public static bool IsMethodReference(this MemberReferenceExpression memberReferenceExpression) =>
        // False positive.
#pragma warning disable IDE0078 // Use pattern matching.
        memberReferenceExpression.GetMemberDirectlyOrFromParentInvocation() is IMethod ||
        memberReferenceExpression.GetResolveResult<MethodGroupResolveResult>() != null;
#pragma warning restore IDE0078 // Use pattern matching.

    public static bool IsFieldReference(this MemberReferenceExpression memberReferenceExpression) =>
        memberReferenceExpression.GetMemberDirectlyOrFromParentInvocation() is IField;

    public static bool IsPropertyReference(this MemberReferenceExpression memberReferenceExpression) =>
        memberReferenceExpression.GetMemberDirectlyOrFromParentInvocation() is IProperty;

    private static IMember GetMemberDirectlyOrFromParentInvocation(this MemberReferenceExpression memberReferenceExpression) =>
        memberReferenceExpression.GetMemberResolveResult()?.Member ??
            memberReferenceExpression.Parent.GetResolveResult<InvocationResolveResult>()?.Member;
}
