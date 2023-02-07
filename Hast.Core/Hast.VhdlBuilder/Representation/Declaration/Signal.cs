using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class Signal : TypedDataObject
{
    public Signal() => DataObjectKind = DataObjectKind.Signal;
}
