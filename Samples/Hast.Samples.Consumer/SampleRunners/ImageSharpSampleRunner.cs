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

        var stopwatch = Stopwatch.StartNew();
        var newImage = image.Clone(context => context.HastResize(
            newWidth, newHeight, Environment.ProcessorCount, hastlayer, hardwareRepresentation, configuration));
        stopwatch.Stop();
        await newImage.SaveAsync("FpgaResizedWithHastlayer.jpg");
        Console.WriteLine(FormattableString.Invariant($"On FPGA it took {stopwatch.ElapsedMilliseconds} ms"));
    }

    private static void RunSoftwareBenchmarks(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);

        var newWidth = image.Width / 2;
        var newHeight = image.Height / 2;

        var stopwatch = Stopwatch.StartNew();
        var newImage = image.Clone(context =>
            context.HastResize(newWidth, newHeight, Environment.ProcessorCount));
        stopwatch.Stop();
        newImage.Save("FpgaResizedWithModifiedImageSharp.jpg");
        Console.WriteLine(
            FormattableString.Invariant($"On CPU Modified ImageSharp algorithm took {stopwatch.ElapsedMilliseconds} ms"));

        stopwatch.Restart();
        newImage = image.Clone(context => context.Resize(newWidth, newHeight));
        stopwatch.Stop();
        newImage.Save("FpgaResizedWithOriginalImageSharp.jpg");
        Console.WriteLine(
            FormattableString.Invariant($"On CPU Original ImageSharp algorithm took {stopwatch.ElapsedMilliseconds} ms"));
    }
}
