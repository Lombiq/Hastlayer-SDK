using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Hast.Samples.SampleAssembly.ImageSharpModifications.Resize;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners;

internal class ImageSharpSampleRunner : ISampleRunner
{
    public void Configure(HardwareGenerationConfiguration configuration) =>
        configuration.AddHardwareEntryPointType<HastlayerAcceleratedImageSharp>();

    public async Task RunAsync(
        IHastlayer hastlayer,
        IHardwareRepresentation hardwareRepresentation,
        IProxyGenerationConfiguration configuration)
    {
        // In case you wish to test the sample with a larger file, the fpga.jpg file must be replaced. You can find
        // a 100 megapixel jpeg here: https://photographingspace.com/100-megapixel-moon/

        using var image = await Image.LoadAsync("fpga.jpg");

        // Not accelerated by Hastlayer.
        RunSoftwareBenchmarks(image);

        // Accelerated by Hastlayer.
        var newWidth = image.Width / 2;
        var newHeight = image.Height / 2;

        HastlayerResizeProcessor.LogPixelsWriter = Console.Out;

        Image FpgaClone(out long milliseconds)
        {
            var resizedFpga = CloneAndMeasure(
                image,
                context => context.HastResize(
                    newWidth, newHeight, Environment.ProcessorCount, hastlayer, hardwareRepresentation, configuration),
                out var timeFpga);
            milliseconds = timeFpga;
            return resizedFpga;
        }

        // Execute the FPGA a couple times before measuring to avoid counting Hastlayer's initialization overhead,
        // because in a realistic scenario you would use this more than just once per process.
        using (FpgaClone(out _)) { /* Ignore. */ }
        using (FpgaClone(out _)) { /* Ignore. */ }

        using var newImage = FpgaClone(out var timeFpga);
        await newImage.SaveAsync("FpgaResizedWithHastlayer.jpg");
        Console.WriteLine(FormattableString.Invariant($"On FPGA it took {timeFpga} ms"));
    }

    private static void RunSoftwareBenchmarks(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);

        var newWidth = image.Width / 2;
        var newHeight = image.Height / 2;

        using var resizedNew = CloneAndMeasure(
            image,
            context => context.HastResize(newWidth, newHeight, Environment.ProcessorCount),
            out var timeNew);
        resizedNew.Save("FpgaResizedWithModifiedImageSharp.jpg");
        Console.WriteLine(
            FormattableString.Invariant($"On CPU Modified ImageSharp algorithm took {timeNew} ms"));

        using var resizedOld = CloneAndMeasure(
            image,
            context => context.Resize(newWidth, newHeight),
            out var timeOld);
        resizedOld.Save("FpgaResizedWithOriginalImageSharp.jpg");
        Console.WriteLine(
            FormattableString.Invariant($"On CPU Original ImageSharp algorithm took {timeOld} ms"));
    }

    private static Image CloneAndMeasure(
        Image image,
        Action<IImageProcessingContext> operation,
        out long milliseconds)
    {
        var stopwatch = new Stopwatch();
        var cloned = image.Clone(context =>
        {
            stopwatch.Start();
            operation(context);
            stopwatch.Stop();
        });

        milliseconds = stopwatch.ElapsedMilliseconds;

        return cloned;
    }
}
