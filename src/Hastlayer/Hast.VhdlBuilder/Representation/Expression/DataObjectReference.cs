using Hast.VhdlBuilder.Representation.Declaration;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Expression;

/// <summary>
/// Represents a reference to a VHLD data object (e.g. signal, variable, constant), so e.g. in variable assignments such
/// references should be used.
/// </summary>
[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class DataObjectReference : DataObjectBase
{
    public override IDataObject ToReference() => this;

    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) => vhdlGenerationOptions.ShortenName(Name);
}
