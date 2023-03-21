using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class UnOmittableLineComment : LineComment
{
    public UnOmittableLineComment(string text)
        : base(text) => CantBeOmitted = true;
}
