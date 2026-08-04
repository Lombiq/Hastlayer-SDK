using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{DebugDisplay,nq}")]
public class Signal : TypedDataObject
{
    public Signal() => DataObjectKind = DataObjectKind.Signal;
}
