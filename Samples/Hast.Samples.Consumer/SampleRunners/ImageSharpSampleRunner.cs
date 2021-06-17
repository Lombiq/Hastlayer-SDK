using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Hast.Samples.SampleAssembly.ImageSharpModifications.Resize;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal class ImageSharpSampleRunner : ISampleRunner
    {
        public void Configure(HardwareGenerationConfiguration configuration) =>
            configuration.AddHardwareEntryPointType<ImageSharpSample>();

        public async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            // In case you wish to test the sample with a larger file, the fpga.jpg file must be replaced. You can find
            // a 100 megapixel jpeg here: https://photographingspace.com/100-megapixel-moon/
            
            // Not accelerated by Hastlayer.
            RunSoftwareBenchmarks();

            // Accelerated by Hastlayer.
            using var image = Image.Load("fpga.jpg");

            var resizeImage = await hastlayer
                .GenerateProxy(hardwareRepresentation, new ImageSharpSample(), configuration);
            var modifiedImage = resizeImage
                .Resize(image, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            modifiedImage.Save("fpga_resized_with_hastlayer_fpga.jpg");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var cpuOutput = new ImageSharpSample()
                .Resize(image, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            sw.Stop();
            cpuOutput.Save("fpga_resized_with_hastlayer_cpu.jpg");
            System.Console.WriteLine($"On CPU it took {sw.ElapsedMilliseconds} ms");
        }

        public static void RunSoftwareBenchmarks()
        {
            using var image = Image.Load("fpga.jpg");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var newImage = image.Clone(x => x.HastResize(image.Width / 2, image.Height / 2, System.Environment.ProcessorCount));
            sw.Stop();
            newImage.Save("fpga_resized_wtih_modified_imagesharp.jpg");
            System.Console.WriteLine($"Modified ImageSharp algorithm took {sw.ElapsedMilliseconds} ms");

            sw.Restart();
            newImage = image.Clone(x => x.Resize(image.Width / 2, image.Height / 2));
            sw.Stop();
            newImage.Save("fpga_resized_wtih_original_imagesharp.jpg");
            System.Console.WriteLine($"Original ImageSharp algorithm took {sw.ElapsedMilliseconds} ms");
        }
    }
}
