using Hast.Transformer.Models;
using Hast.Transformer.Vhdl.SubTransformers;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System;

namespace Hast.Transformer.Vhdl.Verifiers;

/// <summary>
/// Verifies whether unsupported languages constructs not checked for otherwise are present, and if yes, throws
/// exceptions. Note: this doesn't check for everything unsupported.
/// </summary>
public class UnsupportedConstructsVerifier : IVerifyer
{
    private readonly IDisplayClassFieldTransformer _displayClassFieldTransformer;

    public UnsupportedConstructsVerifier(IDisplayClassFieldTransformer displayClassFieldTransformer) =>
        _displayClassFieldTransformer = displayClassFieldTransformer;

    public void Verify(SyntaxTree syntaxTree, ITransformationContext transformationContext) =>
        syntaxTree.AcceptVisitor(new UnsupportedConstructsFindingVisitor(_displayClassFieldTransformer));

    private sealed class UnsupportedConstructsFindingVisitor : DepthFirstAstVisitor
    {
        private readonly IDisplayClassFieldTransformer _displayClassFieldTransformer;

        public UnsupportedConstructsFindingVisitor(IDisplayClassFieldTransformer displayClassFieldTransformer) =>
            _displayClassFieldTransformer = displayClassFieldTransformer;

        public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
        {
            base.VisitFieldDeclaration(fieldDeclaration);

            if (fieldDeclaration.HasModifier(Modifiers.Static) &&
                !_displayClassFieldTransformer.IsDisplayClassField(fieldDeclaration))
            {
                throw new NotSupportedException(
                    fieldDeclaration.GetFullName() + " is a static field. " +
                    "Static fields are not supported, see: https://github.com/Lombiq/Hastlayer-SDK/issues/24."
                    .AddParentEntityName(fieldDeclaration));
            }
        }
    }
}
