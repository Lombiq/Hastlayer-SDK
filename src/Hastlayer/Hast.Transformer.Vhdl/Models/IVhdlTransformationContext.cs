using Hast.Transformer.Models;

namespace Hast.Transformer.Vhdl.Models;

/// <summary>
/// The full context of a hardware transformation, including the syntax tree to transform and anything VHDL related.
/// </summary>
public interface IVhdlTransformationContext : ITransformationContext
{
    // Nothing needed here for now, leaving for future purposes.
}
