using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Expression;

/// <summary>
/// An array element access expression, i.e. array(index).
/// </summary>
[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class ArrayElementAccess : ArrayAccessBase
{
    public IVhdlElement IndexExpression { get; set; }

    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        ArrayReference.ToReference().ToVhdl(vhdlGenerationOptions) + "(" + IndexExpression.ToVhdl(vhdlGenerationOptions) + ")";
}
