using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Hast.Samples.SampleAssembly.ImageSharpModifications.Extensions;
using Hast.Samples.SampleAssembly.ImageSharpModifications.Resize;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
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

            // Not accelerated by hastlayer
            RunSoftwareBenchmarks();

            // Accelerated by hastlayer
            using var image = Image.Load("fpga.jpg");

            var resizeImage = await hastlayer
                .GenerateProxy(hardwareRepresentation, new ImageSharpSample(), configuration);
            var modifiedImage = resizeImage
                .Resize(image, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            modifiedImage.Save("0resized_with_hastlayer_fpga.jpg");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var cpuOutput = new ImageSharpSample()
                .Resize(image, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            sw.Stop();
            cpuOutput.Save("0resized_with_cpu.jpg");
            System.Console.WriteLine($"On CPU it took {sw.ElapsedMilliseconds} ms");
        }

        public static void RunSoftwareBenchmarks()
        {
            using var image = Image.Load("fpga.jpg");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            image.Mutate(x => x.HastResize(image.Width / 2, image.Height / 2, System.Environment.ProcessorCount));
            sw.Stop();
            image.Save("0resized_fpga.jpg");
            System.Console.WriteLine($"Non-hastlyer algorithm took {sw.ElapsedMilliseconds} ms");
        }
    }
}
