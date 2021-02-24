using Hast.Layer;
using Hast.Samples.SampleAssembly;
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
            using var image = new Bitmap("fpga.jpg"); // Change to IS Image later?

            var resizeImage = await hastlayer
                .GenerateProxy(hardwareRepresentation, new ImageSharpSample(), configuration);
            // var modifiedImage = resizeImage.HastResize()
            // modifiedImage.Save('resized.png', ImageFormat.Png);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            // var cpuOutput = new ImageSharpResize().Do Stuff
            sw.Stop();
            System.Console.WriteLine($"On CPU it took {sw.ElapsedMilliseconds} ms");
        }
    }
}
