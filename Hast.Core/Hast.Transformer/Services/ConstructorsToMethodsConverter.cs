using Hast.Layer;
using Hast.Transformer.Helpers;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Services;

// Maybe this would be better suitable in Hast.Transformer.Vhdl since it might not be interesting for every hardware
// description language. But then we'd need to run IInstanceMethodsToStaticConverter again to make constructor methods
// static too.

/// <summary>
/// Converts constructors to normal method declarations so they're easier to process later.
/// </summary>
public class ConstructorsToMethodsConverter : IConverter
{
    public IEnumerable<string> Dependencies { get; } = new[] { nameof(ObjectVariableTypesConverter) };

    public void Convert(
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable) =>
        syntaxTree.AcceptVisitor(new ConstructorConvertingVisitor());

    private sealed class ConstructorConvertingVisitor : DepthFirstAstVisitor
    {
        public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
        {
            base.VisitConstructorDeclaration(constructorDeclaration);

            // If the ctor is empty then no need to keep it.
            if (!constructorDeclaration.Body.Statements.Any() &&
                constructorDeclaration.Initializer == ConstructorInitializer.Null)
            {
                constructorDeclaration.Remove();
                return;
            }

            var method = MethodDeclarationFactory.CreateMethod(
                name: constructorDeclaration.Name,
                annotations: constructorDeclaration.Annotations,
                attributes: constructorDeclaration.Attributes,
                parameters: constructorDeclaration.Parameters,
                body: constructorDeclaration.Body,
                returnType: new PrimitiveType("void"));

            // If the type has no base type then remove the automatically added base.ctor() call from the constructor as
            // it won't reference anything transformable.
            if (!constructorDeclaration.FindFirstParentTypeDeclaration().BaseTypes.Any())
            {
                method.Body
                    .OfType<ExpressionStatement>()
                    .SingleOrDefault(statement =>
                        statement.Expression is InvocationExpression invocation &&
                        invocation.Target.Is<MemberReferenceExpression>(reference => reference.MemberName.IsConstructorName())
                    )?.Remove();
            }

            // If there is a constructor initializer (like Ctor() : this(bla)) then handle that too by adding an
            // explicit call.
            if (constructorDeclaration.Initializer != ConstructorInitializer.Null)
            {
                if (constructorDeclaration.Initializer.ConstructorInitializerType != ConstructorInitializerType.This)
                {
                    throw new NotSupportedException(
                        "Only this() constructor initializers are supported. Unsupported constructor: " +
                        Environment.NewLine +
                        constructorDeclaration);
                }

                var invocation = new InvocationExpression(
                    new MemberReferenceExpression(
                        new ThisReferenceExpression().WithAnnotation(new ThisResolveResult(constructorDeclaration.GetActualType())),
                        constructorDeclaration.Name),
                    constructorDeclaration.Initializer.Arguments.Select(argument => argument.Clone()));

                invocation.AddAnnotation(constructorDeclaration.Initializer.GetResolveResult<InvocationResolveResult>());

                var invocationStatement = new ExpressionStatement(invocation);
                if (method.Body.Any())
                {
                    AstInsertionHelper.InsertStatementBefore(method.Body.First(), invocationStatement);
                }
                else
                {
                    method.Body.Add(invocationStatement);
                }
            }

            constructorDeclaration.ReplaceWith(method);
        }
    }
}
