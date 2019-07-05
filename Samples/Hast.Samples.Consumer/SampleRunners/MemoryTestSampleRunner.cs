using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Samples.SampleAssembly;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal static class MemoryTestSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<MemoryTest>();
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
        {
            var memoryTest = await hastlayer.GenerateProxy(hardwareRepresentation, new MemoryTest());

            var output1 = memoryTest.Run(3, 7);
            var output2 = memoryTest.Run(0, 50);
            var output3 = memoryTest.Run(47, 100);
        }
    }
}
