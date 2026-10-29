using System.Collections.Generic;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

/// <summary>
/// An element that has a body but is inline, i.e. not surrounded by anything.
/// </summary>
[DebuggerDisplay("{ToVhdl(Hast.VhdlBuilder.Representation.VhdlGenerationOptions.Debug)}")]
public class InlineBlock : IBlockElement
{
    public IList<IVhdlElement> Body { get; }

    public InlineBlock(params IVhdlElement[] vhdlElements) => Body = [.. vhdlElements];

    public InlineBlock(IEnumerable<IVhdlElement> vhdlElements) => Body = [.. vhdlElements];

    public virtual string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) => Body.ToVhdl(vhdlGenerationOptions);
}
