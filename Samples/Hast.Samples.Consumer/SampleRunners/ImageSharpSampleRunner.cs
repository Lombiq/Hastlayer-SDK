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
        configuration.AddHardwareEntryPointType<ImageSharpSample>();

    public async Task RunAsync(
        IHastlayer hastlayer,
        IHardwareRepresentation hardwareRepresentation,
        IProxyGenerationConfiguration configuration)
    {
        // In case you wish to test the sample with a larger file, the fpga.jpg file must be replaced. You can find
        // a 100 megapixel jpeg here: https://photographingspace.com/100-megapixel-moon/

        // Not accelerated by Hastlayer.
        RunSoftwareBenchmarks();

        // Accelerated by Hastlayer.
        using var image = await Image.LoadAsync("fpga.jpg");
        var width = image.Width;
        var height = image.Height;

        var sw = Stopwatch.StartNew();
        var newImage = image.Clone(img => img.HastResize(
            width / 2, height / 2, Environment.ProcessorCount, hastlayer, hardwareRepresentation, configuration));
        sw.Stop();
        await newImage.SaveAsync("FpgaResizedWithHastlayer.jpg");
        Console.WriteLine(FormattableString.Invariant($"On FPGA it took {sw.ElapsedMilliseconds} ms"));
    }

    private static void RunSoftwareBenchmarks()
    {
        using var image = Image.Load("fpga.jpg");
        var width = image.Width;
        var height = image.Height;

        var sw = Stopwatch.StartNew();
        var newImage = image.Clone(img =>
            img.HastResize(width / 2, height / 2, Environment.ProcessorCount));
        sw.Stop();
        newImage.Save("FpgaResizedWithModifiedImageSharp.jpg");
        Console.WriteLine(
            FormattableString.Invariant($"Modified ImageSharp algorithm took {sw.ElapsedMilliseconds} ms"));

        sw.Restart();
        newImage = image.Clone(img => img.Resize(width / 2, height / 2));
        sw.Stop();
        newImage.Save("FpgaResizedWithOriginalImageSharp.jpg");
        Console.WriteLine(
            FormattableString.Invariant($"Original ImageSharp algorithm took {sw.ElapsedMilliseconds} ms"));
    }
}
