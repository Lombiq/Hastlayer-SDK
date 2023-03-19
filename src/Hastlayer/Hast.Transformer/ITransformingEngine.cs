using Hast.Common.Interfaces;
using Hast.Layer;
using Hast.Transformer.Models;
using System.Threading.Tasks;

namespace Hast.Transformer;

/// <summary>
/// Describes the concrete engine that does the .NET to hardware description transformation. Implementation could
/// include ones generating e.g. VHDL or Verilog code.
/// </summary>
public interface ITransformingEngine : IDependency
{
    /// <summary>
    /// Transforms the given syntax tree to hardware description.
    /// </summary>
    /// <param name="transformationContext">
    /// The full context of the transformation, including the syntax tree to transform.
    /// </param>
    /// <returns>The hardware description created from the syntax tree.</returns>
    Task<IHardwareDescription> TransformAsync(ITransformationContext transformationContext);
}
