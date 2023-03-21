using Hast.Transformer.Models;

namespace Hast.Transformer.Vhdl.Models;

public class VhdlTransformationContext : TransformationContext, IVhdlTransformationContext
{
    public VhdlTransformationContext(ITransformationContext previousContext)
        : base(previousContext)
    {
    }
}
