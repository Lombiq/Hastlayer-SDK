using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System;
using System.Linq;

namespace Hast.Transformer.Services.ConstantValuesSubstitution;

/// <summary>
/// The value of parameters of an object creation or method invocation, a member (in this case: field or property) or
/// what's returned from a method can only be substituted if they have a globally unique value, since these are used not
/// just from a single method (in contrast to variables). Thus these need special care, handling them here.
/// </summary>
internal class GlobalValueHoldersHandlingVisitor : DepthFirstAstVisitor
{
    private readonly ConstantValuesTable _constantValuesTable;
    private readonly ITypeDeclarationLookupTable _typeDeclarationLookupTable;
    private readonly AstNode _rootNode;

    public GlobalValueHoldersHandlingVisitor(
        ConstantValuesSubstitutingAstProcessor constantValuesSubstitutingAstProcessor,
        AstNode rootNode)
    {
        _constantValuesTable = constantValuesSubstitutingAstProcessor.ConstantValuesTable;
        _typeDeclarationLookupTable = constantValuesSubstitutingAstProcessor.TypeDeclarationLookupTable;
        _rootNode = rootNode;
    }

    public override void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
    {
        base.VisitObjectCreateExpression(objectCreateExpression);

        // Marking all parameters where a non-constant parameter is passed as non-constant.
        foreach (var argument in objectCreateExpression.Arguments)
        {
            var parameter = ConstantValueSubstitutionHelper
                .FindConstructorParameterForPassedExpression(objectCreateExpression, argument, _typeDeclarationLookupTable);

            if (argument is PrimitiveExpression primitiveExpression)
            {
                _constantValuesTable.MarkAsPotentiallyConstant(parameter, primitiveExpression, _rootNode, disallowDifferentValues: true);
            }
            else
            {
                _constantValuesTable.MarkAsNonConstant(parameter, _rootNode);
            }
        }
    }

    public override void VisitInvocationExpression(InvocationExpression invocationExpression)
    {
        base.VisitInvocationExpression(invocationExpression);

        // Marking all parameters where a non-constant parameter is passed as non-constant.
        foreach (var argument in invocationExpression.Arguments)
        {
            var parameter = ConstantValueSubstitutionHelper
                .FindMethodParameterForPassedExpression(invocationExpression, argument, _typeDeclarationLookupTable);

            if (argument is PrimitiveExpression expression)
            {
                _constantValuesTable.MarkAsPotentiallyConstant(parameter, expression, _rootNode, disallowDifferentValues: true);
            }
            else
            {
                _constantValuesTable.MarkAsNonConstant(parameter, _rootNode);
            }
        }
    }

    public override void VisitParameterDeclaration(ParameterDeclaration parameterDeclaration)
    {
        base.VisitParameterDeclaration(parameterDeclaration);

        // Handling parameters with default values.
        if (parameterDeclaration.DefaultExpression is not PrimitiveExpression primitiveExpression) return;
        _constantValuesTable.MarkAsPotentiallyConstant(parameterDeclaration, primitiveExpression, _rootNode, disallowDifferentValues: true);
    }

    public override void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
    {
        base.VisitMemberReferenceExpression(memberReferenceExpression);

        // We only care about cases where this member is assigned to.
        if (!memberReferenceExpression.Parent.Is<AssignmentExpression>(
            assignment => assignment.Left == memberReferenceExpression, out var parentAssignment))
        {
            return;
        }

        var memberEntity = memberReferenceExpression.FindMemberDeclaration(_typeDeclarationLookupTable);

        // If a primitive value is assigned then re-mark it so if there are multiple different such assignments then the
        // member will be unmarked.
        if (parentAssignment.Right is PrimitiveExpression expression)
        {
            _constantValuesTable
                .MarkAsPotentiallyConstant(memberEntity, expression, _rootNode, disallowDifferentValues: true);
        }
        else
        {
            _constantValuesTable.MarkAsNonConstant(memberEntity, _rootNode);
        }
    }

    public override void VisitReturnStatement(ReturnStatement returnStatement)
    {
        base.VisitReturnStatement(returnStatement);

        // Method substitution is only valid if there is only one return statement in the method (or multiple ones but
        // returning the same constant value).
        if (returnStatement.Expression is PrimitiveExpression primitiveExpression)
        {
            _constantValuesTable.MarkAsPotentiallyConstant(
                returnStatement.FindFirstParentEntityDeclaration(),
                primitiveExpression,
                _rootNode,
                disallowDifferentValues: true);
        }
        else if (!returnStatement.Expression.GetFullName().IsBackingFieldName())
        {
            _constantValuesTable.MarkAsNonConstant(returnStatement.FindFirstParentEntityDeclaration(), _rootNode);
        }
    }

    // Since fields and properties can be read even without initializing them they can be only substituted if they are
    // read-only (or just have their data type defaults assigned to them). This is possible with readonly fields and
    // auto-properties having only getters. So to prevent anything else from being substituted marking those as
    // non-constant (this could be improved to still substitute the .NET default value if nothing else is assigned, but
    // this should be extremely rare). With the scope-using substitution this restriction seems to be safe to remove,
    // however since with read- only members potentially constant values are also enforced in .NET it's safer to leave
    // it like this.

    public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
    {
        base.VisitPropertyDeclaration(propertyDeclaration);

        if (!propertyDeclaration.IsReadOnlyMember())
        {
            _constantValuesTable.MarkAsNonConstant(propertyDeclaration, _rootNode);
        }
        else if (propertyDeclaration.Initializer is PrimitiveExpression primitiveExpression)
        {
            _constantValuesTable.MarkAsPotentiallyConstant(propertyDeclaration, primitiveExpression, _rootNode, disallowDifferentValues: true);
        }
    }

    public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
    {
        base.VisitFieldDeclaration(fieldDeclaration);

        if (!fieldDeclaration.IsReadOnlyMember())
        {
            _constantValuesTable.MarkAsNonConstant(fieldDeclaration, _rootNode);
        }
        else
        {
            // The AST should be processed in a way at this stage that there aren't multiple variables.
            var variable = fieldDeclaration.Variables.SingleOrDefault();
            if (variable?.Initializer is PrimitiveExpression primitiveExpression)
            {
                _constantValuesTable.MarkAsPotentiallyConstant(fieldDeclaration, primitiveExpression, _rootNode, disallowDifferentValues: true);
            }
        }
    }
}
