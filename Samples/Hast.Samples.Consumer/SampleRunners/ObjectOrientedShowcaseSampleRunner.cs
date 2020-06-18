using System.Threading.Tasks;
using Hast.Layer;
using Hast.Samples.SampleAssembly;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal static class ObjectOrientedShowcaseSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<ObjectOrientedShowcase>();
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            var ooShowcase = await hastlayer
                .GenerateProxy(hardwareRepresentation, new ObjectOrientedShowcase(), configuration);
            var memoryConfig = (hastlayer as Hastlayer).CreateMemoryConfiguration(hardwareRepresentation);
            var sum = ooShowcase.Run(93, memoryConfig); // 293
        }
    }
}
