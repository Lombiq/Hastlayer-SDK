using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Expression;

/// <summary>
/// A slice of an array data object, i.e. array(fromIndex to toIndex).
/// </summary>
[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class ArraySlice : ArrayAccessBase
{
    public int IndexFrom { get; set; }
    public int IndexTo { get; set; }

    public bool IsDownTo { get; set; }

    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        new VectorSlice
        {
            IndexFrom = IndexFrom,
            IndexTo = IndexTo,
            IsDownTo = IsDownTo,
            Vector = ArrayReference,
        }.ToVhdl(vhdlGenerationOptions);
}
