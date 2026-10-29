using System.Collections.Generic;

namespace Hast.VhdlBuilder.Representation.Declaration;

public class VhdlManifest : IVhdlElement
{
    public IList<IVhdlElement> Modules { get; } = [];

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) => Modules.ToVhdl(vhdlGenerationOptions);
}
