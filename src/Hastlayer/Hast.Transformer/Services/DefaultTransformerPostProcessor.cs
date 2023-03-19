using Hast.Layer;
using Hast.Synthesis.Services;
using Hast.Transformer.Configuration;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hast.Transformer.Services;

public class DefaultTransformerPostProcessor : ITransformerPostProcessor
{
    private readonly IDeviceDriverSelector _deviceDriverSelector;
    private readonly IEnumerable<EventHandler<ITransformationContext>> _eventHandlers;
    private readonly IInvocationInstanceCountAdjuster _invocationInstanceCountAdjuster;
    private readonly ISimpleMemoryUsageVerifier _simpleMemoryUsageVerifier;
    private readonly ITransformationContextCacheService _transformationContextCacheService;
    private readonly ITransformingEngine _engine;
    private readonly ITypeDeclarationLookupTableFactory _typeDeclarationLookupTableFactory;

    public DefaultTransformerPostProcessor(
        IDeviceDriverSelector deviceDriverSelector,
        IEnumerable<EventHandler<ITransformationContext>> eventHandlers,
        IInvocationInstanceCountAdjuster invocationInstanceCountAdjuster,
        ISimpleMemoryUsageVerifier simpleMemoryUsageVerifier,
        ITransformationContextCacheService transformationContextCacheService,
        ITransformingEngine engine,
        ITypeDeclarationLookupTableFactory typeDeclarationLookupTableFactory)
    {
        _deviceDriverSelector = deviceDriverSelector;
        _eventHandlers = eventHandlers;
        _invocationInstanceCountAdjuster = invocationInstanceCountAdjuster;
        _simpleMemoryUsageVerifier = simpleMemoryUsageVerifier;
        _transformationContextCacheService = transformationContextCacheService;
        _engine = engine;
        _typeDeclarationLookupTableFactory = typeDeclarationLookupTableFactory;
    }

    public Task<IHardwareDescription> PostProcessAsync(
        IEnumerable<string> assemblyPaths,
        string transformationId,
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable,
        IArraySizeHolder arraySizeHolder)
    {
        _invocationInstanceCountAdjuster.AdjustInvocationInstanceCounts(syntaxTree, configuration);

        var transformerConfiguration = configuration.TransformerConfiguration();
        if (transformerConfiguration.UseSimpleMemory)
        {
            _simpleMemoryUsageVerifier.VerifySimpleMemoryUsage(syntaxTree);
        }

        var context = new TransformationContext
        {
            Id = transformationId,
            HardwareGenerationConfiguration = configuration,
            SyntaxTree = syntaxTree,
            TypeDeclarationLookupTable = _typeDeclarationLookupTableFactory.Create(syntaxTree),
            KnownTypeLookupTable = knownTypeLookupTable,
            ArraySizeHolder = arraySizeHolder,
            DeviceDriver = _deviceDriverSelector.GetDriver(configuration.DeviceName),
        };

        if (context.DeviceDriver == null)
        {
            throw new InvalidOperationException(
                $"No device driver with the name \"{context.HardwareGenerationConfiguration.DeviceName}\" was found.");
        }

        foreach (var eventHandler in _eventHandlers) eventHandler?.Invoke(this, context);

        if (context.HardwareGenerationConfiguration.EnableCaching)
        {
            _transformationContextCacheService.SetTransformationContext(context, assemblyPaths);
        }

        return _engine.TransformAsync(context);
    }
}
