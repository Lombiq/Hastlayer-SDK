using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Expression;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Vhdl.Helpers;

internal static class BinaryChainBuilder
{
    public static IVhdlElement BuildBinaryChain(IEnumerable<IVhdlElement> expressions, BinaryOperator binaryOperator)
    {
        var expressionsList = expressions.ToList();

        if (!expressionsList.Any()) return Empty.Instance;

        var chainExpression = expressionsList[0];

        // Iteratively build a binary expression chain to.
        if (expressionsList.Count > 1)
        {
            var currentBinary = new Binary
            {
                Left = expressionsList[1],
                Operator = binaryOperator,
            };

            var firstBinary = currentBinary;

            foreach (var expression in expressionsList.Skip(2))
            {
                var newBinary = new Binary
                {
                    Left = expression,
                    Operator = binaryOperator,
                };

                currentBinary.Right = newBinary;
                currentBinary = newBinary;
            }

            currentBinary.Right = chainExpression;
            chainExpression = firstBinary;
        }

        return chainExpression;
    }
}
