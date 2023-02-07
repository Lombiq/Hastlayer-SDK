using System.Collections.Generic;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class Module : IVhdlElement
{
    public ICollection<Library> Libraries { get; } = new List<Library>();
    public Entity Entity { get; set; }
    public Architecture Architecture { get; set; }

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        Libraries.ToVhdl(vhdlGenerationOptions) + vhdlGenerationOptions.NewLineIfShouldFormat() +
        Entity.ToVhdl(vhdlGenerationOptions) + vhdlGenerationOptions.NewLineIfShouldFormat() +
        Architecture.ToVhdl(vhdlGenerationOptions);
}
