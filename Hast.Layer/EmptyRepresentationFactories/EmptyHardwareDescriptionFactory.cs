using Lombiq.HelpfulLibraries.Common.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Layer.EmptyRepresentationFactories;

internal static class EmptyHardwareDescriptionFactory
{
    public static IHardwareDescription Create(IHardwareGenerationConfiguration configuration)
    {
        var mockHardwareEntryPointMappings = new Dictionary<string, int>();

        for (int i = 0; i < configuration.HardwareEntryPointMemberFullNames.Count; i++)
        {
            mockHardwareEntryPointMappings[configuration.HardwareEntryPointMemberFullNames[i]] = i;
        }

        return new HardwareDescription(mockHardwareEntryPointMappings);
    }

    private class HardwareDescription : IHardwareDescription
    {
        public string TransformationId { get; } = Sha256Helper.Empty();
        public string Language => "VHDL";

        public IReadOnlyDictionary<string, int> HardwareEntryPointNamesToMemberIdMappings { get; }

        public IEnumerable<ITransformationWarning> Warnings => Enumerable.Empty<ITransformationWarning>();

        public HardwareDescription(IReadOnlyDictionary<string, int> hardwareEntryPointNamesToMemberIdMappings) =>
            HardwareEntryPointNamesToMemberIdMappings = hardwareEntryPointNamesToMemberIdMappings;

        public Task SerializeAsync(Stream stream) => throw new NotSupportedException();
    }
}
