using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(Hast.VhdlBuilder.Representation.VhdlGenerationOptions.Debug)}")]
public class UnOmittableBlockComment : BlockComment
{
    public UnOmittableBlockComment(params string[] lines)
        : base(lines) => CantBeOmitted = true;
}
