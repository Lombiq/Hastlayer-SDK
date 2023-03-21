using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Syntax;

public static class AssignmentExpressionExtension
{
    /// <summary>
    /// Checks whether an assignment is for an "alias", i.e. the left side will just be used as an alternate name for
    /// the right side.
    /// </summary>
    public static bool IsPotentialAliasAssignment(this AssignmentExpression assignmentExpression)
    {
        var left = assignmentExpression.Left;
        var right = assignmentExpression.Right;

        var leftType = left.GetActualType();

        // Need to use GetFullName() as sometimes type equality will be false even if really the types are the same.
        return
            leftType.GetFullName() == right.GetActualType().GetFullName() &&
            leftType.IsReferenceType == true &&
            left is IdentifierExpression &&
            (right is IdentifierExpression ||
                right.Is<MemberReferenceExpression>(reference => reference.IsFieldReference()) ||
                right.Is<MemberReferenceExpression>(reference => reference.IsPropertyReference()) ||
                right.Is<IndexerExpression>(indexer => indexer.Target.GetActualType().IsArray()));
    }
}
