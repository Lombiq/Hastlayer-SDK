using Hast.Common.Interfaces;
using Hast.Transformer.Models;
using Hast.Transformer.Vhdl.Models;
using System.Threading.Tasks;

namespace Hast.Transformer.Vhdl.Services;

/// <summary>
/// Service for building VHDL manifest from the transformation engine.
/// </summary>
public interface ITransformedVhdlManifestBuilder : IDependency
{
    /// <summary>
    /// Performs some member transformations and creates a new VHDL manifest.
    /// </summary>
    Task<TransformedVhdlManifest> BuildManifestAsync(ITransformationContext transformationContext);
}
