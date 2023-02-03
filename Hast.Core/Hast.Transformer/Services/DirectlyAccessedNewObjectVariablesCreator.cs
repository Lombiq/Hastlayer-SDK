using Hast.Layer;
using Hast.Transformer.Helpers;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;
using System;
using System.Collections.Generic;

namespace Hast.Transformer.Services;

/// <summary>
/// If a newly created object's members are accessed directly instead of assigning the object to a variable for example,
/// then this service will add an intermediary variable for easier later processing.
/// </summary>
/// <example>
/// <code>
/// var size = new BitMask(Size).Size;
///
/// ...will be converted into the following form:
/// var bitMask = new BitMask(Size);
/// var size = bitMask.Size;
/// </code>
/// </example>
public class DirectlyAccessedNewObjectVariablesCreator : IConverter
{
    public IEnumerable<string> Dependencies { get; } = new[] { nameof(ConditionalExpressionsToIfElsesConverter) };

    public void Convert(
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable) =>
        syntaxTree.AcceptVisitor(new DirectlyAccessedNewObjectVariableCreatingVisitor());

    private sealed class DirectlyAccessedNewObjectVariableCreatingVisitor : DepthFirstAstVisitor
    {
        public override void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
        {
            base.VisitObjectCreateExpression(objectCreateExpression);

            HandleExpression(objectCreateExpression, objectCreateExpression.Type);
        }

        public override void VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression)
        {
            base.VisitDefaultValueExpression(defaultValueExpression);

            //// Handling cases like the one below, where Fix64 is a struct without a parameterless ctor:
            //// public static Fix64 Zero() => new Fix64();

            HandleExpression(defaultValueExpression, defaultValueExpression.Type);
        }

        private static void HandleExpression(Expression expression, AstType astType)
        {
            var resolveResult = expression.GetResolveResult();
            var typeName = astType.GetFullName();
            if (expression.Parent is AssignmentExpression ||
                typeName.IsDisplayOrClosureClassName() ||
                // Omitting Funcs for now, as those are used in parallel code with Tasks and handled separately.
                resolveResult.Type.IsFunc())
            {
                return;
            }

            var variableIdentifier = VariableHelper.DeclareAndReferenceVariable("object", expression, astType);
            var assignment = new AssignmentExpression(variableIdentifier, expression.Clone())
                .WithAnnotation(new OperatorResolveResult(
                    resolveResult.Type,
                    System.Linq.Expressions.ExpressionType.Assign,
                    resolveResult,
                    resolveResult));

            AstInsertionHelper.InsertStatementBefore(
                expression.FindFirstParentStatement(),
                new ExpressionStatement(assignment));

            expression.ReplaceWith(variableIdentifier.Clone());
        }
    }
}
