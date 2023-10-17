using Hast.Transformer.Helpers;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;
using System.Linq;
using static Hast.Transformer.Services.ConstantValuesSubstitution.ConstantValuesSubstitutingAstProcessor;

namespace Hast.Transformer.Services.ConstantValuesSubstitution;

internal sealed class ConstantValuesSubstitutingVisitor : DepthFirstAstVisitor
{
    private readonly ConstantValuesSubstitutingAstProcessor _constantValuesSubstitutingAstProcessor;
    private readonly ConstantValuesTable _constantValuesTable;
    private readonly ITypeDeclarationLookupTable _typeDeclarationLookupTable;
    private readonly IArraySizeHolder _arraySizeHolder;
    private readonly IKnownTypeLookupTable _knownTypeLookupTable;

    public ConstantValuesSubstitutingVisitor(
        ConstantValuesSubstitutingAstProcessor constantValuesSubstitutingAstProcessor)
    {
        _constantValuesSubstitutingAstProcessor = constantValuesSubstitutingAstProcessor;
        _constantValuesTable = constantValuesSubstitutingAstProcessor.ConstantValuesTable;
        _typeDeclarationLookupTable = constantValuesSubstitutingAstProcessor.TypeDeclarationLookupTable;
        _arraySizeHolder = constantValuesSubstitutingAstProcessor.ArraySizeHolder;
        _knownTypeLookupTable = constantValuesSubstitutingAstProcessor.KnownTypeLookupTable;
    }

    public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
    {
        base.VisitAssignmentExpression(assignmentExpression);

        // Assignments should be handled here because those should only take effect "after this line" ("after this"
        // works because the visitor visits nodes in topological order and thus possible substitutions will come after
        // this method).

        var parentBlock = assignmentExpression.FindFirstParentBlockStatement();

        // Indexed assignments with a constant index could also be handled eventually, but not really needed now. There
        // won't be a parent block for attributes for example.
        if (assignmentExpression.Right is PrimitiveExpression expression &&
            assignmentExpression.Left is not IndexerExpression &&
            parentBlock != null)
        {
            _constantValuesSubstitutingAstProcessor.ConstantValuesTable.MarkAsPotentiallyConstant(
                assignmentExpression.Left,
                expression,
                parentBlock);
        }

        // If this is assignment is in a while or an if-else then every assignment to it shouldn't affect anything in
        // the outer scope after this. Neither if this is assigning a non-constant value. Note that the expression can
        // be both in a while or if (in which case it can't affect the parent scopes) or have a non- constant assignment
        // (and thus can't have a const value for the current scope).

        if (assignmentExpression.Left is not IdentifierExpression) return;

        if (ConstantValueSubstitutionHelper.IsInWhileOrIfElse(assignmentExpression))
        {
            // Finding all outer scopes. The current parentBlock block will be the if-else or the while statement
            // itself.

            var currentParentBlock = parentBlock.FindFirstParentBlockStatement();

            while (currentParentBlock != null)
            {
                _constantValuesTable.MarkAsNonConstant(assignmentExpression.Left, currentParentBlock);
                currentParentBlock = currentParentBlock.FindFirstParentBlockStatement();
            }
        }

        if (assignmentExpression.Right is not PrimitiveExpression &&
            !assignmentExpression.Right.Is<BinaryOperatorExpression>(binary =>
                (binary.Left.GetFullName() == assignmentExpression.Left.GetFullName() &&
                    binary.Right is PrimitiveExpression) ||
                (binary.Right.GetFullName() == assignmentExpression.Left.GetFullName() &&
                    binary.Left is PrimitiveExpression)))
        {
            _constantValuesTable.MarkAsNonConstant(assignmentExpression.Left, parentBlock);
        }
    }

    public override void VisitIdentifierExpression(IdentifierExpression identifierExpression)
    {
        base.VisitIdentifierExpression(identifierExpression);

        // If it's used as a ref or out parameter in a method invocation then it can't be substituted after this line.
        if (identifierExpression.FindFirstNonParenthesizedExpressionParent() is DirectionExpression)
        {
            _constantValuesTable.MarkAsNonConstant(identifierExpression, identifierExpression.FindFirstParentBlockStatement());
        }

        TrySubstituteValueHolderInExpressionIfInSuitableAssignment(identifierExpression);
    }

    public override void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
    {
        base.VisitMemberReferenceExpression(memberReferenceExpression);

        // Method invocations that have a constant value are substituted in VisitInvocationExpression(), not to mess up
        // the AST upwards.
        if (ConstantValueSubstitutionHelper.IsMethodInvocation(memberReferenceExpression)) return;

        // Is the target an array or some other indexer? We don't handle those.
        if (memberReferenceExpression.Target is IndexerExpression) return;

        if (memberReferenceExpression.IsArrayLengthAccess())
        {
            var arraySize = _arraySizeHolder.GetSize(memberReferenceExpression.Target);

            if (arraySize == null && memberReferenceExpression.Target is MemberReferenceExpression expression)
            {
                arraySize = _arraySizeHolder.GetSize(expression.FindMemberDeclaration(_typeDeclarationLookupTable));
            }

            if (arraySize != null)
            {
                var newExpression = new PrimitiveExpression(arraySize.Length)
                    .WithAnnotation(memberReferenceExpression.CreateResolveResultFromActualType());
                memberReferenceExpression.ReplaceWith(newExpression);
            }

            return;
        }

        TrySubstituteValueHolderInExpressionIfInSuitableAssignment(memberReferenceExpression);
    }

    public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
    {
        base.VisitBinaryOperatorExpression(binaryOperatorExpression);

        if (binaryOperatorExpression.FindFirstParentOfType<AttributeSection>() != null ||
            ConstantValueSubstitutionHelper.IsInWhile(binaryOperatorExpression))
        {
            return;
        }

        if (_constantValuesTable.RetrieveAndDeleteConstantValue(binaryOperatorExpression, out var valueExpression))
        {
            binaryOperatorExpression.ReplaceWith(valueExpression.Clone());
        }
    }

    public override void VisitIfElseStatement(IfElseStatement ifElseStatement)
    {
        base.VisitIfElseStatement(ifElseStatement);

        if (ConstantValueSubstitutionHelper.IsInWhile(ifElseStatement)) return;
        if (ifElseStatement.Condition is not PrimitiveExpression primitiveCondition) return;

        ReturnStatement replacingReturnStatement = null;

        if (primitiveCondition.Value.Equals(true))
        {
            ReplaceIfElse(ifElseStatement.TrueStatement, ifElseStatement, ref replacingReturnStatement);
        }
        else if (ifElseStatement.FalseStatement != Statement.Null)
        {
            ReplaceIfElse(ifElseStatement.FalseStatement, ifElseStatement, ref replacingReturnStatement);
        }

        ifElseStatement.RemoveAndMark();

        // Is this a return statement in the root level of a method or something similar? Because then any other
        // statement after it should be removed too.
        if (replacingReturnStatement == null ||
            (replacingReturnStatement.Parent is not EntityDeclaration &&
             !replacingReturnStatement.Parent.Is<BlockStatement>(block => block.Parent is EntityDeclaration)))
        {
            return;
        }

        var currentStatement = replacingReturnStatement.GetNextStatement();
        while (currentStatement != null)
        {
            var nextStatement = currentStatement.GetNextStatement();
            currentStatement.RemoveAndMark();
            currentStatement = nextStatement;
        }
    }

    public override void VisitInvocationExpression(InvocationExpression invocationExpression)
    {
        base.VisitInvocationExpression(invocationExpression);

        // Substituting method invocations that have a constant return value.

        // This shouldn't really happen, all targets should be member reference expressions.
        if (invocationExpression.Target is not MemberReferenceExpression) return;

        // If the member reference was substituted then we should also substitute the whole invocation.
        if (TrySubstituteValueHolderInExpressionIfInSuitableAssignment(invocationExpression.Target))
        {
            invocationExpression.ReplaceWith(invocationExpression.Target);
        }
    }

    public override void VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression)
    {
        base.VisitUnaryOperatorExpression(unaryOperatorExpression);

        if (unaryOperatorExpression.Expression is not PrimitiveExpression) return;

        if (_constantValuesTable.RetrieveAndDeleteConstantValue(unaryOperatorExpression, out var valueExpression))
        {
            unaryOperatorExpression.ReplaceWith(valueExpression.Clone());
        }
    }

    public override void VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression)
    {
        base.VisitArrayCreateExpression(arrayCreateExpression);

        if (arrayCreateExpression.Arguments.Count > 1)
        {
            ExceptionHelper.ThrowOnlySingleDimensionalArraysSupporterException(arrayCreateExpression);
        }

        var lengthArgument = arrayCreateExpression.Arguments.Single();
        var existingSize = arrayCreateExpression.Parent is AssignmentExpression parentAssignment
            ? _arraySizeHolder.GetSize(parentAssignment.Left)
            : null;

        var noExistingSize = existingSize == null; // Because of IDE0078 false positive.
        if (noExistingSize || lengthArgument is PrimitiveExpression) return;

        // If the array creation doesn't have a static length but the value holder the array is assigned to has the
        // array size defined then just substitute the array length too.
        lengthArgument.ReplaceWith(
            new PrimitiveExpression(existingSize.Length)
            .WithAnnotation(_knownTypeLookupTable.Lookup(KnownTypeCode.Int32).ToResolveResult()));
    }

    protected override void VisitChildren(AstNode node)
    {
        // Deactivating constructor mappings for the identifier after this line if it's again assigned to, e.g.: var x =
        // new MyClass(); // OK x = GetValue(); // Starting with this line no more ctor mapping.

        if ((node is IdentifierExpression || node is MemberReferenceExpression) &&
            node.GetActualType()?.IsArray() == false)
        {
            var fullName = node.GetFullName();

            if (_constantValuesSubstitutingAstProcessor.ObjectHoldersToConstructorsMappings
                    .TryGetValue(fullName, out var constructorReference) &&
                node.FindFirstNonParenthesizedExpressionParent()
                    .Is<AssignmentExpression>(assignment =>
                        assignment.Left == node ||
                            assignment.Left.FindFirstChildOfType<AstNode>(child => child == node) != null) &&
                // Only if this is not the original assignment.
                node != constructorReference.OriginalAssignmentTarget)
            {
                _constantValuesSubstitutingAstProcessor.ObjectHoldersToConstructorsMappings.Remove(fullName);
            }
        }

        // Is this a const expression? Then we can just substitute it directly.
        var resolveResult = node.GetResolveResult();
        if (node is Expression &&
            resolveResult?.IsCompileTimeConstant == true &&
            resolveResult.ConstantValue != null &&
            node is not PrimitiveExpression)
        {
            var type = node.GetActualType();

            if (!type.IsEnum() && node is not NullReferenceExpression)
            {
                node.ReplaceWith(new PrimitiveExpression(resolveResult.ConstantValue)
                    .WithAnnotation(new ConstantResolveResult(type, resolveResult.ConstantValue)));
            }
        }

        // Attributes can slip in here but we don't care about those. Also, due to eliminating branches nodes can be
        // removed on the fly.
        if (node is not Attribute && !node.IsMarkedAsRemoved())
        {
            base.VisitChildren(node);
        }
    }

    private bool TrySubstituteValueHolderInExpressionIfInSuitableAssignment(Expression expression)
    {
        // If this is a value holder on the left side of an assignment then nothing to do.
        if (expression.Parent.Is<AssignmentExpression>(assignment => assignment.Left == expression)) return false;

        // If the value holder is inside a unary operator that can mutate its state then it can't be substituted.
        var mutatingUnaryOperators = new[]
        {
            UnaryOperatorType.Decrement,
            UnaryOperatorType.Increment,
            UnaryOperatorType.PostDecrement,
            UnaryOperatorType.PostIncrement,
        };
        if (expression.Parent.Is<UnaryOperatorExpression>(unary => mutatingUnaryOperators.Contains(unary.Operator)))
        {
            return false;
        }

        // If the expression is in a while statement then most of the time it can't be safely substituted (due to e.g.
        // loop variables). Code with goto is hard to follow so we're not trying to do const substitution for those yet
        // (except for constants that are handled separately in VisitChildren()) most of the time either.
        var isHardToFollow =
            ConstantValueSubstitutionHelper.IsInWhile(expression) ||
            expression.FindFirstParentOfType<MethodDeclaration>()?.FindFirstChildOfType<GotoStatement>() != null;

        PrimitiveExpression valueExpression = null;

        // First checking if there is a substitution for the expression; if not then if it's a member reference then
        // check whether there is a global substitution for the member.
        if ((!isHardToFollow && _constantValuesTable.RetrieveAndDeleteConstantValue(expression, out valueExpression)) ||
            expression.Is<MemberReferenceExpression>(memberReferenceExpression =>
                CheckIfGlobalSubstitution(memberReferenceExpression, isHardToFollow, ref valueExpression)))
        {
            expression.ReplaceWith(valueExpression.Clone());
            return true;
        }

        return false;
    }

    private static void ReplaceIfElse(Statement branchStatement, IfElseStatement ifElseStatement, ref ReturnStatement replacingReturnStatement)
    {
        // Moving all statements from the block up.
        if (branchStatement is not BlockStatement branchBlock)
        {
            var clone = branchStatement.Clone();
            if (clone is ReturnStatement returnStatement) replacingReturnStatement = returnStatement;
            ifElseStatement.ReplaceWith(clone);
            return;
        }

        foreach (var statement in branchBlock.Statements)
        {
            var clone = statement.Clone<Statement>();
            // There should be at most a single return statement in this block.
            if (clone is ReturnStatement returnStatement) replacingReturnStatement = returnStatement;
            AstInsertionHelper.InsertStatementBefore(ifElseStatement, clone);
        }
    }

    private bool CheckIfGlobalSubstitution(
        MemberReferenceExpression memberReferenceExpression,
        bool isHardToFollow,
        ref PrimitiveExpression valueExpression)
    {
        var member = memberReferenceExpression.FindMemberDeclaration(_typeDeclarationLookupTable);

        if (member == null) return false;

        if ((!isHardToFollow || member.IsReadOnlyMember()) &&
            _constantValuesTable.RetrieveAndDeleteConstantValue(member, out valueExpression))
        {
            return true;
        }

        if (member.IsReadOnlyMember())
        {
            // If this is a nested member reference (e.g. _member.Property1.Property2) then let's find the first member
            // that has a corresponding ctor.
            var currentMemberReference = memberReferenceExpression;
            ConstructorReference constructorReference;

            while (
                !_constantValuesSubstitutingAstProcessor.ObjectHoldersToConstructorsMappings
                    .TryGetValue(currentMemberReference.Target.GetFullName(), out constructorReference) &&
                currentMemberReference.Target is MemberReferenceExpression currentMemberReferenceTarget)
            {
                currentMemberReference = currentMemberReferenceTarget;
            }

            if (constructorReference == null) return false;

            // Try to substitute this member reference's value with a value set in the corresponding constructor.

            var constructor = constructorReference.Constructor;

            // Trying to find a place where the same member is references on the same ("this") instance.
            var memberReferenceExpressionInConstructor = ConstantValueSubstitutionHelper
                .FindMemberReferenceInConstructor(constructor, member.GetFullName(), _typeDeclarationLookupTable);

            if (memberReferenceExpressionInConstructor == null) return false;

            // Using the substitution also used in the constructor. This should be safe to do even if in the ctor there
            // are multiple assignments because an retrieved constant will only remain in the ConstantValuesTable if
            // there are no more substitutions needed in the ctor. But for this we need to rebuild a ConstantValuesTable
            // just for this ctor. At this point the ctor should be fully substituted so we only need to care about
            // primitive expressions.

            var constructorConstantValuesTableBuildingVisitor =
                new ConstructorConstantValuesTableBuildingVisitor(constructor);
            constructor.AcceptVisitor(constructorConstantValuesTableBuildingVisitor);

            return constructorConstantValuesTableBuildingVisitor.ConstantValuesTable
                .RetrieveAndDeleteConstantValue(memberReferenceExpressionInConstructor, out valueExpression);
        }

        return false;
    }

    private sealed class ConstructorConstantValuesTableBuildingVisitor : DepthFirstAstVisitor
    {
        public ConstantValuesTable ConstantValuesTable { get; } = new();

        private readonly MethodDeclaration _constructor;

        public ConstructorConstantValuesTableBuildingVisitor(MethodDeclaration constructor) =>
            _constructor = constructor;

        public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            base.VisitAssignmentExpression(assignmentExpression);

            // We need to keep track of the last assignment in the root scope of the method. If after that there is
            // another assignment with a non-constant value or in an if-else or while then that makes the value holder's
            // constant value unusable.

            if (assignmentExpression.Right is not PrimitiveExpression right ||
                ConstantValueSubstitutionHelper.IsInWhileOrIfElse(assignmentExpression))
            {
                ConstantValuesTable.MarkAsNonConstant(assignmentExpression.Left, _constructor);
            }
            else if (right != null)
            {
                ConstantValuesTable.MarkAsPotentiallyConstant(assignmentExpression.Left, right, _constructor);
            }
        }
    }
}
