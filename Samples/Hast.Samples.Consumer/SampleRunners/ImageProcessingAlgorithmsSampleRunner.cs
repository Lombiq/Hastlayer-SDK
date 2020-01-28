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
                // This takes about 160 ms on an i7 CPU and net 150 ms on a Nexys A7 with a MaxDegreeOfParallelism of
                // 25 while the FPGA is about 66% utilized.
                // On Catapult with a MaxDegreeOfParallelism of 50 it uses 59% of the FPGA resources (more would fit
                // actually, needs more testing) and runs in net 65 ms (1150 ms with the communication round trip).
                var modifiedImage = imageContrastModifier.ChangeImageContrast(bitmap, -50);
                modifiedImage.Save("contrast.bmp");

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var cpuOutput = new ImageContrastModifier().ChangeImageContrast(bitmap, -50);
                sw.Stop();
                System.Console.WriteLine("On CPU it took " + sw.ElapsedMilliseconds + " ms.");

                // ImageFilter disabled until it's improved.
                //var imageFilter = await hastlayer.GenerateProxy(hardwareRepresentation, new ImageFilter());
                //var filteredImage = imageFilter.DetectHorizontalEdges(bitmap);
            }
        }
    }
}
