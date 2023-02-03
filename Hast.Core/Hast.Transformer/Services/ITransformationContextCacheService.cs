using Hast.Common.Interfaces;
using Hast.Layer;
using Hast.Transformer.Abstractions;
using Hast.Transformer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hast.Transformer.Services;

/// <summary>
/// Handles caching of the transformed code. When enabled, <see cref="ITransformer"/> may be short-circuited to jump
/// directly to <see cref="ITransformingEngine"/> using the cached <see cref="ITransformationContext"/>.
/// </summary>
public interface ITransformationContextCacheService : IDependency
{
    /// <summary>
    /// Retrieves a stored <see cref="ITransformationContext"/> based on the transformation ID, if any exists.
    /// </summary>
    ITransformationContext GetTransformationContext(IEnumerable<string> assemblyPaths, string transformationId);

    /// <summary>
    /// Retrieves a stored <see cref="ITransformationContext"/> based on the transformation ID and passes it to the <see
    /// cref="ITransformingEngine"/>, if any exists.
    /// </summary>
    /// <returns>
    /// The output of the <see cref="ITransformingEngine.TransformAsync"/> if there is a result, <see langword="null"/>
    /// otherwise.
    /// </returns>
    Task<IHardwareDescription> ExecuteTransformationContextIfAnyAsync(
        IEnumerable<string> assemblyPaths,
        string transformationId);

    /// <summary>
    /// Stores a <see cref="ITransformationContext"/> that can later be looked up using <see
    /// cref="ITransformationContext.Id"/>.
    /// </summary>
    void SetTransformationContext(ITransformationContext transformationContext, IEnumerable<string> assemblyPaths);

    /// <summary>
    /// Returns the transformation ID.
    /// </summary>
    /// <param name="transformationIdComponents">
    /// External components that are prepended to the transformation ID source before hashing.
    /// </param>
    /// <param name="configuration">The configuration of this operation.</param>
    string BuildTransformationId(ICollection<string> transformationIdComponents, IHardwareGenerationConfiguration configuration);
}
