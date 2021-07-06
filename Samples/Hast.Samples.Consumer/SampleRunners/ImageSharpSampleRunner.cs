using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Hast.Samples.SampleAssembly.ImageSharpModifications.Resize;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal class ImageSharpSampleRunner : ISampleRunner
    {
        public void Configure(HardwareGenerationConfiguration configuration) =>
            configuration.AddHardwareEntryPointType<ImageSharpSample>();

        public Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            // In case you wish to test the sample with a larger file, the fpga.jpg file must be replaced. You can find
            // a 100 megapixel jpeg here: https://photographingspace.com/100-megapixel-moon/

            // Not accelerated by Hastlayer.
            RunSoftwareBenchmarks();

            // Accelerated by Hastlayer.
            using var image = Image.Load("fpga.jpg");

            var sw = Stopwatch.StartNew();
            var newImage = image.Clone(img => img.HastResize(
                image.Width / 2, image.Height / 2, System.Environment.ProcessorCount, hastlayer, hardwareRepresentation, configuration));
            sw.Stop();
            newImage.Save("FpgaResizedWithHastlayer.jpg");
            System.Console.WriteLine($"On CPU it took {sw.ElapsedMilliseconds} ms");

            return Task.CompletedTask;
        }

        public static void RunSoftwareBenchmarks()
        {
            using var image = Image.Load("fpga.jpg");
            var sw = Stopwatch.StartNew();
            var newImage = image.Clone(img =>
                img.HastResize(image.Width / 2, image.Height / 2, System.Environment.ProcessorCount));
            sw.Stop();
            newImage.Save("FpgaResizedWithModifiedImageSharp.jpg");
            System.Console.WriteLine($"Modified ImageSharp algorithm took {sw.ElapsedMilliseconds} ms");

            sw.Restart();
            newImage = image.Clone(img => img.Resize(image.Width / 2, image.Height / 2));
            sw.Stop();
            newImage.Save("FpgaResizedWithOriginalImageSharp.jpg");
            System.Console.WriteLine($"Original ImageSharp algorithm took {sw.ElapsedMilliseconds} ms");
        }
    }
}
