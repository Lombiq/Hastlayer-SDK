using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Transforms;
using System;

namespace Hast.TransformerTest
{
    /// <summary>
    /// A composition of <see cref="PatternStatementTransform"/> with the sole purpose of not transforming simple while
    /// statements back to more complex for statements like it would do if executed directly. Other parts of that
    /// transform, like auto-property re-creation is useful.
    /// </summary>
    internal class NoForPatternStatementTransform : ContextTrackingVisitor<AstNode>, IAstTransform
    {
        private readonly PatternStatementTransform _patternStatementTransform;
        private TransformContext _context;


        public NoForPatternStatementTransform() : base()
        {
            _patternStatementTransform = new PatternStatementTransform();
        }

        public void Run(AstNode rootNode, TransformContext context)
        {
            _context = context;
            Initialize(context);
            rootNode.AcceptVisitor(this);
        }

        public override AstNode VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
        {
            _patternStatementTransform.Run(propertyDeclaration, _context);
            return base.VisitPropertyDeclaration(propertyDeclaration);
        }
    }
}
