using Hast.Layer;
using Hast.Transformer.Helpers;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;

namespace Hast.Transformer.Services;

/// <summary>
/// Converts operator overloads into standard methods.
/// </summary>
public class OperatorsToMethodsConverter : IConverter
{
    public IEnumerable<string> Dependencies { get; } = new[] { nameof(ConstructorsToMethodsConverter) };

    public void Convert(
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable) =>
        syntaxTree.AcceptVisitor(new OperatorConvertingVisitor());

    private sealed class OperatorConvertingVisitor : DepthFirstAstVisitor
    {
        public override void VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
        {
            base.VisitOperatorDeclaration(operatorDeclaration);

            var method = MethodDeclarationFactory.CreateMethod(
                name: operatorDeclaration.Name,
                annotations: operatorDeclaration.Annotations,
                attributes: operatorDeclaration.Attributes,
                parameters: operatorDeclaration.Parameters,
                body: operatorDeclaration.Body,
                returnType: operatorDeclaration.ReturnType);

            method.Modifiers = operatorDeclaration.Modifiers;

            operatorDeclaration.ReplaceWith(method);
        }
    }
}
