using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;

namespace Hast.Transformer.Vhdl.ArchitectureComponents;

public class ConfigurableComponent : ArchitectureComponentBase
{
    public IVhdlElement BeginDeclarationsWith { get; set; }
    public IVhdlElement EndDeclarationsWith { get; set; }
    public IVhdlElement ProcessInReset { get; set; }
    public IVhdlElement ProcessNotInReset { get; set; }
    public IVhdlElement BeginBodyWith { get; set; }
    public IVhdlElement EndBodyWith { get; set; }

    public ConfigurableComponent(string name)
        : base(name)
    {
    }

    public override IVhdlElement BuildDeclarations() => BuildDeclarationsBlock(BeginDeclarationsWith, EndDeclarationsWith);

    public override IVhdlElement BuildBody() => new LogicalBlock(
            new LineComment(Name + " start"),
            BeginBodyWith ?? Empty.Instance,
            BuildProcess(ProcessNotInReset, ProcessInReset),
            EndBodyWith ?? Empty.Instance,
            new LineComment(Name + " end"));
}
