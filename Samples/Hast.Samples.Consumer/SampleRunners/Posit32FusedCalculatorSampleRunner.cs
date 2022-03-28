using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Lombiq.Arithmetics;
using Lombiq.HelpfulLibraries.Common.Utilities;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners;

internal class Posit32FusedCalculatorSampleRunner : ISampleRunner
{
    public void Configure(HardwareGenerationConfiguration configuration)
    {
        configuration.AddHardwareEntryPointType<Posit32FusedCalculator>();

        configuration.TransformerConfiguration().AddLengthForMultipleArrays(
           Posit32.QuireSize >> 6,
           Posit32FusedCalculatorExtensions.ManuallySizedArrays);
    }

    public async Task RunAsync(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
    {
        RunSoftwareBenchmarks();

        var positCalculator = await hastlayer.GenerateProxyAsync(hardwareRepresentation, new Posit32FusedCalculator(), configuration);
        _ = positCalculator.CalculateFusedSum(
            CreateTestPosit32BitsArray(),
            hastlayer,
            hardwareRepresentation.HardwareGenerationConfiguration);
    }

    public static void RunSoftwareBenchmarks()
    {
        var positCalculator = new Posit32FusedCalculator();

        var posit32BitsArray = CreateTestPosit32BitsArray();

        // Not to run the benchmark below the first time, because JIT compiling can affect it.
        _ = positCalculator.CalculateFusedSum(posit32BitsArray);

        var sw = Stopwatch.StartNew();
        float result = positCalculator.CalculateFusedSum(posit32BitsArray);
        sw.Stop();

        Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant("Result of Fused addition of posits in array: ", result));
        Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant("Elapsed: ", sw.ElapsedMilliseconds, "ms"));

        Console.WriteLine();
    }

    private static uint[] CreateTestPosit32BitsArray()
    {
        var posit32Array = new uint[201];
        // All positive integers smaller than this value ("pintmax") can be exactly represented with 32-bit Posits.
        posit32Array[0] = new Posit32(8_388_608).PositBits;

        for (var i = 1; i < posit32Array.Length; i++)
        {
            posit32Array[i] = new Posit32(0.5F).PositBits;
        }

        return posit32Array;
    }
}
