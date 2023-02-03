using Hast.VhdlBuilder.Representation.Declaration;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Expression;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class AttributeSpecification : IVhdlElement
{
    public Attribute Attribute { get; set; }
    public IVhdlElement Of { get; set; }
    public string ItemClass { get; set; }
    public IVhdlElement Expression { get; set; }

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        Terminated.Terminate(
            "attribute " + Attribute.ToReference().ToVhdl(vhdlGenerationOptions) + " of " + Of.ToVhdl(vhdlGenerationOptions) + ": " +
            ItemClass + " is " + Expression.ToVhdl(vhdlGenerationOptions),
            vhdlGenerationOptions);
}
