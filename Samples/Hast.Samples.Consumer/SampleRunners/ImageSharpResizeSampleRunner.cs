using Hast.Layer;
using Hast.Samples.SampleAssembly;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;


namespace Hast.Samples.Consumer.SampleRunners
{
    internal class ImageSharpResizeSampleRunner : ISampleRunner
    {
        public void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<ImageSharpResize>();
        }

        public async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            // ...
        }
    }
}
