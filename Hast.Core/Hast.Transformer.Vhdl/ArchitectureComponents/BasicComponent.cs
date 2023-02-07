using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;

namespace Hast.Transformer.Vhdl.ArchitectureComponents;

public class BasicComponent : ArchitectureComponentBase
{
    public IVhdlElement Declarations { get; set; }
    public IVhdlElement Body { get; set; }

    public BasicComponent(string name)
        : base(name)
    {
    }

    public override IVhdlElement BuildDeclarations() => BuildDeclarationsBlock(Declarations);

    public override IVhdlElement BuildBody()
    {
        if (Body == null) return Empty.Instance;

        return new LogicalBlock(
            new LineComment(Name + " start"),
            Body,
            new LineComment(Name + " end"));
    }
}
