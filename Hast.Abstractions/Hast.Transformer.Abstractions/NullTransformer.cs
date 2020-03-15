using Hast.Layer;
using Orchard.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Transformer.Abstractions
{
    public class NullTransformer : ITransformer
    {
        public ILogger Logger { get; set; }


        public NullTransformer()
        {
            Logger = NullLogger.Instance;
        }


        public Task<IHardwareDescription> Transform(IEnumerable<string> assemblyPaths, IHardwareGenerationConfiguration configuration)
        {
            Logger.Warning("No Transformer is available. This most possibly means an issue.");

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
