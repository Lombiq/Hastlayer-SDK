using Lombiq.HelpfulLibraries.Common.Utilities;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

/// <summary>
/// Instantiation of an unconstrained VHDL array.
/// </summary>
[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class UnconstrainedArrayInstantiation : ArrayTypeBase
{
    public int RangeFrom { get; set; }
    public int RangeTo { get; set; }

    public override DataType ToReference() => this;

    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
         StringHelper.CreateInvariant($"{vhdlGenerationOptions.NameShortener(Name)}({RangeFrom} to {RangeTo})");
}
