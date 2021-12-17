using Hast.Layer;
using Hast.Samples.SampleAssembly;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal class ObjectOrientedShowcaseSampleRunner : ISampleRunner
    {
        public void Configure(HardwareGenerationConfiguration configuration) =>
            configuration.AddHardwareEntryPointType<ObjectOrientedShowcase>();

        public async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            var ooShowcase = await hastlayer.GenerateProxyAsync(hardwareRepresentation, new ObjectOrientedShowcase(), configuration);
            _ = ooShowcase.Run(93, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration); // 293
        }
    }
}
