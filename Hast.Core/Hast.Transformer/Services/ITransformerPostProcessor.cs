using Hast.Common.Interfaces;
using Hast.Layer;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hast.Transformer.Services;

/// <summary>
/// Performs final post-processing actions, including user events and executes the <see cref="ITransformingEngine"/>.
/// </summary>
public interface ITransformerPostProcessor : IDependency
{
    /// <summary>
    /// Performs post-processing actions and returns the generated hardware description.
    /// </summary>
    Task<IHardwareDescription> PostProcessAsync(
        IEnumerable<string> assemblyPaths,
        string transformationId,
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable,
        IArraySizeHolder arraySizeHolder);
}
