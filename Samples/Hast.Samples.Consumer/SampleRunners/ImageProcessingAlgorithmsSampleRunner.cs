using Hast.Layer;
using Hast.Samples.SampleAssembly;
using System.Drawing;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal static class ImageProcessingAlgorithmsSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<ImageContrastModifier>();

            // ImageFilter is not parallelized, so not including it not to take away FPGA resources from 
            // ImageContrastModifier.
            //configuration.AddHardwareEntryPointType<ImageFilter>();
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
        {
            using (var bitmap = new Bitmap("fpga.jpg"))
            {
                var imageContrastModifier = await hastlayer
                    .GenerateProxy(hardwareRepresentation, new ImageContrastModifier());
                // This takes about 160ms on an i7 CPU and net 150ms on a Nexys A7.
                var modifiedImage = imageContrastModifier.ChangeImageContrast(bitmap, -50);
                modifiedImage.Save("contrast.bmp");

                // ImageFilter disabled until it's improved.
                //var imageFilter = await hastlayer.GenerateProxy(hardwareRepresentation, new ImageFilter());
                //var filteredImage = imageFilter.DetectHorizontalEdges(bitmap);
            }
        }
    }
}
