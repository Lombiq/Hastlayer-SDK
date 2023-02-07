using Hast.Layer;
using Hast.Transformer.Vhdl.ArchitectureComponents;
using Hast.VhdlBuilder.Representation;
using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.Models;

internal class ArchitectureComponentResult : IArchitectureComponentResult
{
    public IArchitectureComponent ArchitectureComponent { get; set; }
    public IVhdlElement Declarations { get; set; }
    public IVhdlElement Body { get; set; }
    public IEnumerable<ITransformationWarning> Warnings { get; set; } = new List<ITransformationWarning>();

    public ArchitectureComponentResult(IArchitectureComponent component)
    {
        ArchitectureComponent = component;
        Declarations = component?.BuildDeclarations();
        Body = component?.BuildBody();
    }
}
