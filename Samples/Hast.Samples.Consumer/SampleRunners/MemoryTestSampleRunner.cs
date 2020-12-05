using Hast.Layer;
using Hast.Samples.SampleAssembly;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal class MemoryTestSampleRunner : ISampleRunner
    {
        public void Configure(HardwareGenerationConfiguration configuration) => configuration.AddHardwareEntryPointType<MemoryTest>();

        public async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation,
            IProxyGenerationConfiguration configuration)
        {
            var memoryTest = await hastlayer.GenerateProxy(hardwareRepresentation, new MemoryTest(), configuration);

            var output1 = memoryTest.Run(0, 1, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            var output2 = memoryTest.Run(0, 3, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            var output3 = memoryTest.Run(0, 7, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            var output4 = memoryTest.Run(0, 50, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            var output5 = memoryTest.Run(1, 1, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            var output6 = memoryTest.Run(3, 7, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            var output7 = memoryTest.Run(47, 100, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        }
    }
}
