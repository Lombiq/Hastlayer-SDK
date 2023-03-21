using Hast.Common.Services;
using Hast.Layer;
using Hast.Transformer.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Hast.Transformer.Services;

public class TransformationContextCacheService : ITransformationContextCacheService
{
    private readonly IMemoryCache _cache;
    private readonly IJsonConverter _jsonConverter;
    private readonly ILogger<TransformationContextCacheService> _logger;
    private readonly ITransformingEngine _engine;
    private readonly IHashProvider _hashProvider;

    public TransformationContextCacheService(
        IMemoryCache cache,
        IJsonConverter jsonConverter,
        ILogger<TransformationContextCacheService> logger,
        ITransformingEngine engine,
        IHashProvider hashProvider)
    {
        _cache = cache;
        _jsonConverter = jsonConverter;
        _logger = logger;
        _engine = engine;
        _hashProvider = hashProvider;
    }

    public ITransformationContext GetTransformationContext(IEnumerable<string> assemblyPaths, string transformationId) =>
        _cache.Get(GetCacheKey(assemblyPaths, transformationId)) as ITransformationContext;

    public Task<IHardwareDescription> ExecuteTransformationContextIfAnyAsync(
        IEnumerable<string> assemblyPaths,
        string transformationId) =>
        GetTransformationContext(assemblyPaths, transformationId) is { } cachedTransformationContext
            ? _engine.TransformAsync(cachedTransformationContext)
            : Task.FromResult<IHardwareDescription>(null);

    public void SetTransformationContext(ITransformationContext transformationContext, IEnumerable<string> assemblyPaths) => _cache.Set(
            GetCacheKey(assemblyPaths, transformationContext.Id),
            transformationContext,
            new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(5),
            });

    public string BuildTransformationId(
        ICollection<string> transformationIdComponents,
        IHardwareGenerationConfiguration configuration)
    {
        transformationIdComponents.AddRange(configuration.HardwareEntryPointMemberFullNames);
        transformationIdComponents.AddRange(configuration.HardwareEntryPointMemberNamePrefixes);
        transformationIdComponents.Add(_jsonConverter.Serialize(configuration.CustomConfiguration));

        // Adding the assembly name so the Hastlayer version is included too, to prevent stale caches after a Hastlayer
        // update.
        transformationIdComponents.Add(GetType().Assembly.FullName);

        // Adding the device name to ensure different a cached program for a different hardware doesn't get used.
        transformationIdComponents.Add(configuration.DeviceName);

        if (!string.IsNullOrWhiteSpace(configuration.Label)) transformationIdComponents.Add(configuration.Label);

        foreach (var transformationIdComponent in transformationIdComponents)
        {
            _logger.LogTrace(
                "Transformation ID component: {Component}",
                transformationIdComponent.StartsWithOrdinal("source code: ")
                    ? "[whole source code]"
                    : transformationIdComponent);
        }

        return _hashProvider.ComputeHash(string.Empty, transformationIdComponents.ToArray());
    }

    private static string GetCacheKey(IEnumerable<string> assemblyPaths, string transformationId)
    {
        var fileHashes = new StringBuilder();

        foreach (var path in assemblyPaths)
        {
            using var stream = File.OpenRead(path);
            using var sha256 = SHA256.Create();
            fileHashes.Append(BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty));
        }

        var hashCode = transformationId.GetHashCode(StringComparison.InvariantCulture);
        return FormattableString.Invariant($"Hast.Transformer.TransformationContextCache.{fileHashes} - {hashCode}");
    }
}
