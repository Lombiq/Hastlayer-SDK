using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Hastlayer_ImageSharp_PracticeDemo.Resize;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal class ImageSharpSampleRunner : ISampleRunner
    {
        public void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<ImageSharpSample>();
        }

        public async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            // Not accelerated by hastlayer
            using var img = Image.Load("fpga.jpg");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            img.Mutate(x => x.HastResize(img.Width/2, img.Height/2, System.Environment.ProcessorCount));
            img.Save("resized_fpga.jpg");
            sw.Stop();
            System.Console.WriteLine($"On CPU it took {sw.ElapsedMilliseconds} ms");


            // In case you wish to test the sample with a larger file, the fpga.jpg file must be replaced. You can find
            // a 100 megapixel jpeg here: https://photographingspace.com/100-megapixel-moon/
            //using var image = new Bitmap("fpga.jpg");
            using var image = Image.Load("fpga.jpg");

            var resizeImage = await hastlayer
                .GenerateProxy(hardwareRepresentation, new ImageSharpSample(), configuration);
            var modifiedImage = resizeImage.HastResize(image);
            modifiedImage.Save("resized_with_hastlayer_fpga.jpg");

            sw.Restart();
            var cpuOutput = new ImageSharpSample().HastResize(image);
            sw.Stop();
            System.Console.WriteLine($"On CPU it took {sw.ElapsedMilliseconds} ms");
        }
    }
}
