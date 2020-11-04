using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hast.Common.Interfaces;
using Hast.Layer;
using Hast.Transformer.Abstractions.Extensions;

namespace Hast.Transformer.Abstractions
{
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
        Task<IHardwareDescription> Transform(IList<string> assemblyPaths, IHardwareGenerationConfiguration configuration);
    }


    public static class TransformerExtensions
    {
        public static Task<IHardwareDescription> Transform(
            this ITransformer transformer,
            IList<Assembly> assemblies,
            IHardwareGenerationConfiguration configuration)
        {
            assemblies.ThrowArgumentExceptionIfAnyInMemory();
            return transformer.Transform(assemblies.Select(assembly => assembly.Location).ToList(), configuration);
        }
    }
}
