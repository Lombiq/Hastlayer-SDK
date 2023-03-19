using Hast.Common.Services;
using Hast.Transformer.Helpers;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Transformer.Vhdl.SubTransformers;

public class RemainderOperatorExpressionsExpander : IRemainderOperatorExpressionsExpander
{
    private readonly IHashProvider _hashProvider;

    public RemainderOperatorExpressionsExpander(IHashProvider hashProvider) => _hashProvider = hashProvider;

    public void ExpandRemainderOperatorExpressions(SyntaxTree syntaxTree) =>
        syntaxTree.AcceptVisitor(new RemainderOperatorExpressionsExpanderVisitor(_hashProvider));

    private sealed class RemainderOperatorExpressionsExpanderVisitor : DepthFirstAstVisitor
    {
        private readonly IHashProvider _hashProvider;

        public RemainderOperatorExpressionsExpanderVisitor(IHashProvider hashProvider) => _hashProvider = hashProvider;

        public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
        {
            base.VisitBinaryOperatorExpression(binaryOperatorExpression);

            if (binaryOperatorExpression.Operator != BinaryOperatorType.Modulus) return;

            // Changing a % b to a – a / b * b. At this point the operands should have the same type, so it's safe just
            // clone around.

            if (binaryOperatorExpression.GetActualType() == null)
            {
                binaryOperatorExpression
                    .AddAnnotation(binaryOperatorExpression.Left.CreateResolveResultFromActualType());
            }

            // First assigning the operands to new variables so if method calls, casts or anything are in there those
            // are not duplicated.

            void CreateVariableForOperand(Expression operand)
            {
                // Don't create a variable if it's not necessary. Primitive values should be left out because operations
                // with primitive operands can be faster on hardware.
                if (operand is PrimitiveExpression or IdentifierExpression)
                {
                    return;
                }

                // Need to add ILRange because there can be multiple remainder operations for the same variable so
                // somehow we need to distinguish between them.
                var ilRangeName = operand.GetILRangeName();
                if (string.IsNullOrEmpty(ilRangeName))
                {
                    ilRangeName = operand
                        .FindFirstChildOfType<AstNode>(child => !string.IsNullOrEmpty(child.GetILRangeName()))
                        ?.GetILRangeName();
                }

                var variableIdentifier = VariableHelper.DeclareAndReferenceVariable(
                    _hashProvider.ComputeHash("remainderOperand", operand.GetFullName(), ilRangeName),
                    operand.GetActualType(),
                    TypeHelper.CreateAstType(operand.GetActualType()),
                    operand.FindFirstParentStatement());

                var assignment = new AssignmentExpression(variableIdentifier, operand.Clone())
                    .WithAnnotation(operand.CreateResolveResultFromActualType());

                AstInsertionHelper.InsertStatementBefore(
                    binaryOperatorExpression.FindFirstParentStatement(),
                    new ExpressionStatement(assignment));

                operand.ReplaceWith(variableIdentifier.Clone());
            }

            CreateVariableForOperand(binaryOperatorExpression.Left);
            CreateVariableForOperand(binaryOperatorExpression.Right);

            // Building the chained operation from the inside out.

            // a / b
            var dividingExpression = binaryOperatorExpression.Clone<BinaryOperatorExpression>();
            dividingExpression.Operator = BinaryOperatorType.Divide;

            // a / b * b
            var multiplyingExpression = binaryOperatorExpression.Clone<BinaryOperatorExpression>();
            multiplyingExpression.Operator = BinaryOperatorType.Multiply;
            multiplyingExpression.Left = dividingExpression;

            // a – a / b * b
            binaryOperatorExpression.Operator = BinaryOperatorType.Subtract;
            binaryOperatorExpression.Right = multiplyingExpression;
        }
    }
}
