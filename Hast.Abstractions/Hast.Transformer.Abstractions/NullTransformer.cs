using Hast.Common.Interfaces;
using Hast.Layer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Transformer.Abstractions
{
    public class NullTransformer : ITransformer
    {
        private readonly ILogger _logger;

        public NullTransformer(ILogger logger)
        {
            _logger = logger;
        }


        public Task<IHardwareDescription> Transform(IEnumerable<string> assemblyPaths, IHardwareGenerationConfiguration configuration)
        {
            _logger.LogWarning("No Transformer is available. This most possibly means an issue.");

            var mockHardwareEntryPointMappings = new Dictionary<string, int>();

            for (int i = 0; i < configuration.HardwareEntryPointMemberFullNames.Count; i++)
            {
                mockHardwareEntryPointMappings[configuration.HardwareEntryPointMemberFullNames[i]] = i;
            }

            return Task.FromResult<IHardwareDescription>(new HardwareDescription(mockHardwareEntryPointMappings));
        }


        private class HardwareDescription : IHardwareDescription
        {
            public string Language => "VHDL";

            public IReadOnlyDictionary<string, int> HardwareEntryPointNamesToMemberIdMappings { get; }

            public IEnumerable<ITransformationWarning> Warnings => Enumerable.Empty<ITransformationWarning>();


            public HardwareDescription(IReadOnlyDictionary<string, int> hardwareEntryPointNamesToMemberIdMappings)
            {
                HardwareEntryPointNamesToMemberIdMappings = hardwareEntryPointNamesToMemberIdMappings;
            }


            public Task Serialize(Stream stream)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
