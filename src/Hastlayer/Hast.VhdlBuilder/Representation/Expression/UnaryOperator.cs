using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Hast.VhdlBuilder.Representation.Expression;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class UnaryOperator : IVhdlElement
{
    private readonly string _source;

    public static readonly UnaryOperator Identity = new("+");
    public static readonly UnaryOperator Negation = new("-");

    [SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "It's used 2 rows above.")]
    private UnaryOperator(string source) => _source = source;

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) => _source;
}
