using Hast.Layer;
using Hast.Samples.SampleAssembly;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Drawing;
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
            // In case you wish to test the sample with a larger file, the fpga.jpg file must be replaced. You can find
            // a 100 megapixel jpeg here: https://photographingspace.com/100-megapixel-moon/
            using var image = new Bitmap("fpga.jpg"); // Change to IS Image later?
            // using var image = SixLabors.ImageSharp.Image.Load("fpga.jpg");

            var resizeImage = await hastlayer
                .GenerateProxy(hardwareRepresentation, new ImageSharpSample(), configuration);
            // TODO: create method in ImageSharpSample.cs
            // var modifiedImage = resizeImage.HastResize();
            // modifiedImage.Save('resized.png', ImageFormat.Png);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            // var cpuOutput = new ImageSharpSample().HastResize();
            sw.Stop();
            System.Console.WriteLine($"On CPU it took {sw.ElapsedMilliseconds} ms");
        }
    }
}
