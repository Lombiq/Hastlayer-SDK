using Hast.Layer;
using Hast.Samples.SampleAssembly;
using System.Drawing;
using System.Drawing.Imaging;
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

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            // In case you wish to test the sample with a larger file, the fpga.jpg file must be replaced. You can find
            // a 100 megapixel jpeg here: https://photographingspace.com/100-megapixel-moon/
            using (var bitmap = new Bitmap("fpga.jpg"))
            {
                var imageContrastModifier = await hastlayer
                    .GenerateProxy(hardwareRepresentation, new ImageContrastModifier(), configuration);
                var memoryConfig = (hastlayer as Hastlayer).CreateMemoryConfiguration(hardwareRepresentation);
                var modifiedImage = imageContrastModifier.ChangeImageContrast(bitmap, -50, memoryConfig);
                modifiedImage.Save("contrast.bmp", ImageFormat.Bmp);

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var cpuOutput = new ImageContrastModifier().ChangeImageContrast(bitmap, -50, memoryConfig);
                sw.Stop();
                System.Console.WriteLine("On CPU it took " + sw.ElapsedMilliseconds + " ms.");

                // ImageFilter disabled until it's improved.
                //var imageFilter = await hastlayer.GenerateProxy(hardwareRepresentation, new ImageFilter());
                //var filteredImage = imageFilter.DetectHorizontalEdges(bitmap);
            }
        }
    }
}
