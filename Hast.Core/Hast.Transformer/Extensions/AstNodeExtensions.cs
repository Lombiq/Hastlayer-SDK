using ICSharpCode.Decompiler.CSharp.Resolver;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ICSharpCode.Decompiler.CSharp.Syntax;

public static class AstNodeExtensions
{
    /// <summary>
    /// Retrieves the node's full name, including (if applicable) information about return type, type parameters,
    /// arguments...
    /// </summary>
    public static string GetFullName(this AstNode node)
    {
        switch (node)
        {
            case PrimitiveType primitiveType:
                return primitiveType.Keyword;
            case ComposedType composedType:
                var name = composedType.BaseType.GetFullName();
                if (!composedType.ArraySpecifiers.Any()) return name;

                var nameBuilder = new StringBuilder(name);
                foreach (var arraySpecifier in composedType.ArraySpecifiers.Select(nameBuilder.Append)) nameBuilder.Append(arraySpecifier);
                return nameBuilder.ToString();
            case TypeDeclaration:
            case AstType:
                return node.GetActualTypeFullName();
            case EntityDeclaration entityDeclaration:
                return node.GetMemberResolveResult()?.GetFullName() ??
                       CreateParentEntityBasedName(node, entityDeclaration.Name);
            case MemberReferenceExpression memberReferenceExpression:
                return memberReferenceExpression.Target.GetFullName() + "." + memberReferenceExpression.MemberName;
            case ObjectCreateExpression:
                return node.CreateNameForUnnamedNode();
            case InvocationExpression:
                return node.CreateNameForUnnamedNode();
            case IdentifierExpression expression:
                return CreateParentEntityBasedName(node, expression.Identifier);
            case IndexerExpression expression1:
                return expression1.Target.GetFullName();
            case Identifier identifier:
                return identifier.Name;
            case Attribute attribute:
                return attribute.Type.GetFullName();
            case TypeReferenceExpression typeReferenceExpression:
                return typeReferenceExpression.Type.GetFullName();
            case PrimitiveExpression primitiveExpression:
                return primitiveExpression.Value.ToString();
            case VariableInitializer variableInitializer:
                return node.Parent is FieldDeclaration
                    ? node.FindFirstParentEntityDeclaration().GetFullName()
                    : CreateParentEntityBasedName(node, variableInitializer.Name);
            case AssignmentExpression assignment:
                return node.CreateNameForUnnamedNode() + assignment.Left.GetFullName() + assignment.Right.GetFullName();
            default:
                return GetFullNameOfUnknownNode(node);
        }
    }

    public static string GetILRangeName(this AstNode node)
    {
        var ilInstruction = node.Annotation<ILInstruction>();
        return ilInstruction != null ? ilInstruction.ILRanges.First().ToString() : string.Empty;
    }

    /// <summary>
    /// Retrieves the simple dot-delimited name of a type, including the parent types' and the wrapping namespace's
    /// name.
    /// </summary>
    public static string GetSimpleName(this AstNode node) =>
        // Unlike formerly with Cecil, the FullName property is in a simple format.
        node.GetMemberResolveResult()?.Member.ReflectionName ?? node.GetActualType().FullName;

    public static T GetResolveResult<T>(this AstNode node)
        where T : ResolveResult =>
        node.GetResolveResult() as T;

    public static MemberResolveResult GetMemberResolveResult(this AstNode node) =>
        node.GetResolveResult<MemberResolveResult>();

    public static bool IsIn<T>(this AstNode node)
        where T : AstNode =>
        node.FindFirstParentOfType<T>() != null;

    public static TypeDeclaration FindFirstParentTypeDeclaration(this AstNode node) =>
        node.FindFirstParentOfType<TypeDeclaration>();

    public static EntityDeclaration FindFirstParentEntityDeclaration(this AstNode node) =>
        node.FindFirstParentOfType<EntityDeclaration>();

    public static Statement FindFirstParentStatement(this AstNode node) =>
        node.FindFirstParentOfType<Statement>();

    public static BlockStatement FindFirstParentBlockStatement(this AstNode node) =>
        node.FindFirstParentOfType<BlockStatement>();

    public static T FindFirstParentOfType<T>(this AstNode node)
        where T : AstNode =>
        node.FindFirstParentOfType<T>(_ => true);

    public static T FindFirstParentOfType<T>(this AstNode node, Predicate<T> predicate)
        where T : AstNode =>
        node.FindFirstParentOfType(predicate, out _);

    public static T FindFirstParentOfType<T>(this AstNode node, Predicate<T> predicate, out int height)
        where T : AstNode
    {
        height = 1;
        node = node.Parent;

        while (node != null && !(node is T nodeOfType && predicate(nodeOfType)))
        {
            node = node.Parent;
            height++;
        }

        return (T)node;
    }

    public static T FindFirstChildOfType<T>(this AstNode node)
        where T : AstNode =>
        node.FindFirstChildOfType<T>(_ => true);

    public static T FindFirstChildOfType<T>(this AstNode node, Predicate<T> predicate)
        where T : AstNode =>
        node.FindAllChildrenOfType(predicate, stopAfterFirst: true).SingleOrDefault();

    public static IEnumerable<T> FindAllChildrenOfType<T>(
        this AstNode node,
        Predicate<T> predicate = null,
        bool stopAfterFirst = false)
        where T : AstNode
    {
        var children = new Queue<AstNode>(node.Children);
        var matchingChildren = new List<T>();
        predicate ??= _ => true;

        while (children.Count != 0)
        {
            var currentChild = children.Dequeue();

            if (currentChild is T castCurrentChild && predicate(castCurrentChild))
            {
                matchingChildren.Add(castCurrentChild);
                if (stopAfterFirst) return matchingChildren;
            }

            foreach (var child in currentChild.Children)
            {
                children.Enqueue(child);
            }
        }

        return matchingChildren;
    }

    public static bool Is<T>(this AstNode node, Predicate<T> predicate)
        where T : AstNode =>
        node.Is(predicate, out _);

    public static bool Is<T>(this AstNode node, out T castNode)
        where T : AstNode =>
        node.Is(_ => true, out castNode);

    public static bool Is<T>(this AstNode node, Predicate<T> predicate, out T castNode)
        where T : AstNode
    {
        castNode = node as T;
        if (castNode == null) return false;
        return predicate(castNode);
    }

    public static T As<T>(this AstNode node)
        where T : AstNode =>
        node as T;

    public static AstNode FindFirstNonParenthesizedExpressionParent(this AstNode node)
    {
        var parent = node.Parent;

        while (parent is ParenthesizedExpression)
        {
            parent = parent.Parent;
        }

        return parent;
    }

    public static IType GetActualType(this AstNode node)
    {
        if (node is ParenthesizedExpression parenthesizedExpression)
        {
            return parenthesizedExpression.Expression.GetActualType();
        }

        if (node is IndexerExpression indexerExpression)
        {
            return indexerExpression.Target.GetActualType().GetElementType();
        }

        if (node is AssignmentExpression assignmentExpression)
        {
            return assignmentExpression.Left.GetActualType();
        }

        if (node is UnaryOperatorExpression unaryOperatorExpression)
        {
            return unaryOperatorExpression.Expression.GetActualType();
        }

        if (node is DefaultValueExpression defaultValueExpression)
        {
            return defaultValueExpression.Type.GetActualType();
        }

        var type = node.GetResolveResult().Type;
        return type.Equals(SpecialType.UnknownType) ? null : type;
    }

    public static string GetActualTypeFullName(this AstNode node) => node.GetActualType().GetFullName();

    public static ResolveResult CreateResolveResultFromActualType(this AstNode node) =>
        node.GetActualType().ToResolveResult();

    public static T Clone<T>(this AstNode node)
        where T : AstNode =>
        (T)node.Clone();

    /// <summary>
    /// Remove the node from the syntax tree and mark it as such so later it can be determined that it was removed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// While a node being orphaned in the syntax tree can be determined by checking whether its Parent, PrevSibling,
    /// and NextSibling are all null but that's only useful if such dangling nodes can't normally be created otherwise
    /// (like it is the case during const substitution).
    /// </para>
    /// </remarks>
    public static void RemoveAndMark(this AstNode node)
    {
        node.AddAnnotation(new WasRemoved());
        node.Remove();
    }

    public static bool IsMarkedAsRemoved(this AstNode node) => node.Annotation<WasRemoved>() != null;

    internal static string GetReferencedMemberFullName(this AstNode node)
    {
        var memberResolveResult = node.GetMemberResolveResult();

        if (node is not MemberReferenceExpression memberReferenceExpression)
        {
            return memberResolveResult?.GetFullName();
        }

        var property = memberResolveResult?.Member as IProperty;
        var isCustomProperty =
            memberReferenceExpression.IsPropertyReference() &&
            property != null &&
            !property.Getter.IsCompilerGenerated() &&
            !property.Setter.IsCompilerGenerated();

        // For certain members only their parent invocation will contain usable ResolveResult (see:
        // https://github.com/icsharpcode/ILSpy/issues/1407). For properties this is the case every time since from the
        // IMember of the property we can't see whether the getter or setter is invoked, only from the parent invocation
        // (this is only true for custom properties, not for auto-properties).
        if (node.Parent is InvocationExpression invocationExpression &&
            (memberResolveResult == null || isCustomProperty))
        {
            if (invocationExpression.Target == node)
            {
                return node.Parent.GetResolveResult<InvocationResolveResult>()?.GetFullName();
            }

            if (memberReferenceExpression.IsPropertyReference())
            {
                // This is not necessarily true in all cases but seems to be OK for now: If the property is not the
                // target but an argument of an invocation then the property reference is most possibly a getter.
                return property?.Getter.GetFullName();
            }
        }

        // Heuristics on when a property is used as a getter or setter.
        if (isCustomProperty && GetCustomPropertyFullName(node, property) is { } customPropertyFullName)
        {
            return customPropertyFullName;
        }

        //// This will only be the case for Task.Factory.StartNew calls made from lambdas, e.g. the <Run>b__0
        //// method here:
        //// Task.Factory.StartNew (<>c__DisplayClass3_.<>9__0 ?? (<>c__DisplayClass3_.<>9__0 = <>c__DisplayClass3_.<Run>b__0), num);
        var methodGroupResolveResult = node.GetResolveResult<MethodGroupResolveResult>();
        return methodGroupResolveResult != null
            ? methodGroupResolveResult.Methods.Single().GetFullName()
            : memberResolveResult?.GetFullName();
    }

    private static string GetCustomPropertyFullName(AstNode node, IProperty property)
    {
        if (node.Parent is not AssignmentExpression parentAssignment)
        {
            return property.Getter.GetFullName();
        }

        // Can't use if-else because theoretically the node can't just be in these two properties.
        if (parentAssignment.Left == node) return property.Setter.GetFullName();
        if (parentAssignment.Right == node) return property.Getter.GetFullName();

        return null;
    }

    private static string CreateNameForUnnamedNode(this AstNode node) =>
        // The node doesn't really have a name so give it one that is suitably unique.
        CreateParentEntityBasedName(node, node + node.GetILRangeName());

    private static string CreateParentEntityBasedName(AstNode node, string name) =>
        node.FindFirstParentEntityDeclaration().GetFullName() + "." + name;

    private static string GetFullNameOfUnknownNode(AstNode node)
    {
        var referencedMemberFullName = node.GetReferencedMemberFullName();
        if (!string.IsNullOrEmpty(referencedMemberFullName)) return referencedMemberFullName;

        if (node.GetResolveResult<ILVariableResolveResult>() is { } iLVariableResolveResult)
        {
            return CreateParentEntityBasedName(node, iLVariableResolveResult.Variable.Name);
        }

        if (node.Annotation<ILVariable>() is { } ilVariable)
        {
            return CreateParentEntityBasedName(node, ilVariable.Name);
        }

        if (node == Expression.Null || node == Statement.Null)
        {
            return string.Empty;
        }

        return node.CreateNameForUnnamedNode();
    }

    private sealed class WasRemoved
    {
    }
}
