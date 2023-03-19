using Hast.Common.Models;
using Hast.Layer;
using Hast.Transformer.Models;
using Hast.Transformer.Vhdl.Configuration;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hast.Transformer.Vhdl.Services;

public class VhdlTransformingEngine : ITransformingEngine
{
    private readonly IVhdlHardwareDescriptionCachingService _vhdlHardwareDescriptionCachingService;
    private readonly ITransformedVhdlManifestBuilder _transformedVhdlManifestBuilder;
    private readonly IEnumerable<EventHandler<TransformedVhdlManifest>> _vhdlTransformationEventHandler;

    public VhdlTransformingEngine(
        IVhdlHardwareDescriptionCachingService vhdlHardwareDescriptionCachingService,
        ITransformedVhdlManifestBuilder transformedVhdlManifestBuilder,
        IEnumerable<EventHandler<TransformedVhdlManifest>> vhdlTransformationEventHandler)
    {
        _vhdlHardwareDescriptionCachingService = vhdlHardwareDescriptionCachingService;
        _transformedVhdlManifestBuilder = transformedVhdlManifestBuilder;
        _vhdlTransformationEventHandler = vhdlTransformationEventHandler;
    }

    public async Task<IHardwareDescription> TransformAsync(ITransformationContext transformationContext)
    {
        if (transformationContext.HardwareGenerationConfiguration.EnableCaching)
        {
            var cachedHardwareDescription = await _vhdlHardwareDescriptionCachingService
                .GetHardwareDescriptionAsync(transformationContext.Id);
            if (cachedHardwareDescription != null) return cachedHardwareDescription;
        }

        var transformedVhdlManifest = await _transformedVhdlManifestBuilder.BuildManifestAsync(transformationContext);
        foreach (var handler in _vhdlTransformationEventHandler)
        {
            handler?.Invoke(this, transformedVhdlManifest);
        }

        var vhdlGenerationConfiguration = transformationContext
            .HardwareGenerationConfiguration
            .VhdlTransformerConfiguration()
            .VhdlGenerationConfiguration;

        var vhdlGenerationOptions = new VhdlGenerationOptions
        {
            OmitComments = !vhdlGenerationConfiguration.AddComments,
        };

        if (vhdlGenerationConfiguration.ShortenNames)
        {
            vhdlGenerationOptions.NameShortener = VhdlGenerationOptions.SimpleNameShortener;
        }

        var xdcSource = transformedVhdlManifest.XdcFile?.ToVhdl(vhdlGenerationOptions);
        var hardwareDescription = new VhdlHardwareDescription
        {
            TransformationId = transformationContext.Id,
            HardwareEntryPointNamesToMemberIdMappings = transformedVhdlManifest.MemberIdTable.Mappings,
            VhdlSource = transformedVhdlManifest.Manifest.ToVhdl(vhdlGenerationOptions),
            Warnings = transformedVhdlManifest.Warnings,
            XdcSource = xdcSource,
        };

        if (transformationContext.HardwareGenerationConfiguration.EnableCaching)
        {
            await _vhdlHardwareDescriptionCachingService.SetHardwareDescriptionAsync(
                transformationContext.Id,
                hardwareDescription);
        }

        return hardwareDescription;
    }
}
