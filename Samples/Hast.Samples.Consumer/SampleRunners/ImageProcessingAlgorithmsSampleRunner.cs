using System.Drawing;
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Samples.SampleAssembly;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal static class ImageProcessingAlgorithmsSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<ImageContrastModifier>();
            configuration.AddHardwareEntryPointType<ImageFilter>();
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
        {
            using (var bitmap = new Bitmap("fpga.jpg"))
            {
                var imageContrastModifier = await hastlayer
                    .GenerateProxy(hardwareRepresentation, new ImageContrastModifier());
                var modifiedImage = imageContrastModifier.ChangeImageContrast(bitmap, -50);

                var imageFilter = await hastlayer.GenerateProxy(hardwareRepresentation, new ImageFilter());
                var filteredImage = imageFilter.DetectHorizontalEdges(bitmap);
            }
        }
    }
}
