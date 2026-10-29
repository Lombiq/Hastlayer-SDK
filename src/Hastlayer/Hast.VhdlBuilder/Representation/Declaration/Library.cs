using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(Hast.VhdlBuilder.Representation.VhdlGenerationOptions.Debug)}")]
public class Library : INamedElement
{
    public string Name { get; set; }
    public IList<string> Uses { get; } = [];

    public Library() { }

    public Library(string name, IList<string> uses)
    {
        Name = name;
        Uses = uses;
    }

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions)
    {
        if (string.IsNullOrEmpty(Name)) return string.Empty;

        var builder = new StringBuilder();

        builder.Append(Terminated.Terminate("library " + Name, vhdlGenerationOptions));

        foreach (var use in Uses)
        {
            builder.Append(Terminated.Terminate("use " + Name + "." + use, vhdlGenerationOptions));
        }

        return builder.ToString();
    }
}
