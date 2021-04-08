using Hast.Layer;
using Hast.Samples.SampleAssembly;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal class ImageProcessingAlgorithmsSampleRunner : ISampleRunner
    {
        public void Configure(HardwareGenerationConfiguration configuration) =>
            configuration.AddHardwareEntryPointType<ImageContrastModifier>()
            // ImageFilter is not parallelized, so not including it not to take away FPGA resources from
            // ImageContrastModifier.
            // .AddHardwareEntryPointType<ImageFilter>()
            ;


        public async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            // In case you wish to test the sample with a larger file, the fpga.jpg file must be replaced. You can find
            // a 100 megapixel jpeg here: https://photographingspace.com/100-megapixel-moon/
            using var bitmap = await Image.LoadAsync<Rgba32>("fpga.jpg");

            var imageContrastModifier = await hastlayer
                .GenerateProxy(hardwareRepresentation, new ImageContrastModifier(), configuration);
            var modifiedImage = imageContrastModifier.ChangeImageContrast(bitmap, -50, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            await modifiedImage.SaveAsync("contrast.bmp", new BmpEncoder());

            var sw = System.Diagnostics.Stopwatch.StartNew();
            new ImageContrastModifier().ChangeImageContrast(bitmap, -50, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            sw.Stop();
            System.Console.WriteLine("On CPU it took " + sw.ElapsedMilliseconds + " ms.");

            // ImageFilter disabled until it's improved.
            //var imageFilter = await hastlayer.GenerateProxy(hardwareRepresentation, new ImageFilter());
            //var filteredImage = imageFilter.DetectHorizontalEdges(bitmap);
        }
    }
}
