using Hast.Common.Interfaces;
using Hast.Layer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hast.Transformer;

/// <summary>
/// Service for transforming a .NET assembly into hardware description.
/// </summary>
public interface ITransformer : IDependency
{
    /// <summary>
    /// Transforms the given assembly to hardware description.
    /// </summary>
    /// <param name="assemblyPaths">The file path to the assemblies to transform.</param>
    /// <param name="configuration">Configuration for how the hardware generation should happen.</param>
    /// <returns>The hardware description created from the assemblies.</returns>
    Task<IHardwareDescription> TransformAsync(IList<string> assemblyPaths, IHardwareGenerationConfiguration configuration);
}
