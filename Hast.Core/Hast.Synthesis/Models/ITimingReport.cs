using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Synthesis.Models;

/// <summary>
/// Timing report containing hardware timing information on binary operations.
/// </summary>
public interface ITimingReport
{
    /// <summary>
    /// Retrieves the timing value for the given binary operation.
    /// </summary>
    /// <param name="binaryOperator">The operator of the binary operation.</param>
    /// <param name="operandSizeBits">The size of the operation's operands, in bits.</param>
    /// <param name="isSigned">Indicates whether the operands are signed.</param>
    /// <param name="constantOperand">
    /// If one of the operand of the operation is constant then supply the value here (due to compiler optimizations the
    /// latency can be different - smaller - in such a case).
    /// </param>
    /// <returns>The latency, in ns, what the operation will roughly take. -1 if no timing data was found.</returns>
    decimal GetLatencyNs(BinaryOperatorType binaryOperator, int operandSizeBits, bool isSigned, string constantOperand = null);

    /// <summary>
    /// Retrieves the timing value for the given unary operation.
    /// </summary>
    /// <param name="unaryOperator">The operator of the unary operation.</param>
    /// <param name="operandSizeBits">The size of the operation's operands, in bits.</param>
    /// <param name="isSigned">Indicates whether the operands are signed.</param>
    /// <returns>The latency, in ns, what the operation will roughly take. -1 if no timing data was found.</returns>
    // No need for a constantOperand parameter, since a constant unary operator expression is just a constant that will
    // be substituted in compile-time.
    decimal GetLatencyNs(UnaryOperatorType unaryOperator, int operandSizeBits, bool isSigned);
}
