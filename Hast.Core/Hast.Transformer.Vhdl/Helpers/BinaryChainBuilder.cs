using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Expression;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Vhdl.Helpers;

internal static class BinaryChainBuilder
{
    public static IVhdlElement BuildBinaryChain(IEnumerable<IVhdlElement> expressions, BinaryOperator binaryOperator)
    {
        if (!expressions.Any()) return Empty.Instance;

        var chainExpression = expressions.First();

        // Iteratively build a binary expression chain to.
        if (expressions.Count() > 1)
        {
            var currentBinary = new Binary
            {
                Left = expressions.Skip(1).First(),
                Operator = binaryOperator,
            };

            var firstBinary = currentBinary;

            foreach (var expression in expressions.Skip(2))
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
