using Hast.Layer;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;

namespace Hast.Transformer.Services;

/// <summary>
/// Removes ref locals and substitutes them with the original reference. This makes the AST simpler.
/// </summary>
/// <example>
/// <para>The variable "reference" can be substituted by an indexer expression to the array element it refers to.</para>
/// <code>
/// uint[] array;
/// array = new uint[1];
/// array [0] = num;
/// ref uint reference = ref array[0];
/// reference = reference &gt;&gt; 1;
/// </code>
/// </example>
public class RefLocalVariablesRemover : IConverter
{
    public IEnumerable<string> Dependencies { get; } = new[] { nameof(MethodInliner) };

    public void Convert(
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable) =>
        syntaxTree.AcceptVisitor(new InitializersChangingVisitor());

    private sealed class InitializersChangingVisitor : DepthFirstAstVisitor
    {
        private readonly Dictionary<string, Expression> _substitutes = new();

        public override void VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
        {
            base.VisitVariableDeclarationStatement(variableDeclarationStatement);

            if (!(variableDeclarationStatement.Type is ComposedType composedType && composedType.HasRefSpecifier))
            {
                return;
            }

            foreach (var variableInitializer in variableDeclarationStatement.Variables)
            {
                _substitutes[variableInitializer.GetFullName()] =
                    ((DirectionExpression)variableInitializer.Initializer).Expression;
            }

            variableDeclarationStatement.Remove();
        }

        public override void VisitIdentifierExpression(IdentifierExpression identifierExpression)
        {
            base.VisitIdentifierExpression(identifierExpression);

            if (!_substitutes.TryGetValue(identifierExpression.GetFullName(), out var substitute))
            {
                return;
            }

            identifierExpression.ReplaceWith(substitute.Clone());
        }
    }
}
