using Hast.Layer;
using Hast.Samples.Posit.Extensions;
using Lombiq.Arithmetics;
using Lombiq.HelpfulLibraries.Common.Utilities;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Samples.Posit;

internal static class Posit8E3CalculatorSampleRunner
{
    public static void Configure(HardwareGenerationConfiguration configuration) =>
        configuration.AddHardwareEntryPointType<Posit8E3Calculator>();

    public static async Task RunAsync(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
    {
        RunSoftwareBenchmarks(hastlayer, hardwareRepresentation);

        var positCalculator = await hastlayer.GenerateProxyAsync(hardwareRepresentation, new Posit8E3Calculator());

        _ = positCalculator.CalculateIntegerSumUpToNumber(
            100000,
            hastlayer,
            hardwareRepresentation.HardwareGenerationConfiguration);

        positCalculator.CalculatePowerOfReal(5, 0.5F);

        var numbers = new int[Posit8E3Calculator.MaxDegreeOfParallelism];
        for (int i = 0; i < Posit8E3Calculator.MaxDegreeOfParallelism; i++)
        {
            numbers[i] = 100000 + (i % 2 == 0 ? -1 : 1);
        }

        _ = positCalculator.ParallelizedCalculateIntegerSumUpToNumbers(
            numbers,
            hastlayer,
            hardwareRepresentation.HardwareGenerationConfiguration);

        var posit8E3Array = new uint[100000];

        for (var i = 0; i < 100000; i++)
        {
            posit8E3Array[i] = i % 2 == 0
                ? new Posit8E3(0.25F * 2 * i).PositBits
                : new Posit8E3(0.25F * -2 * i).PositBits;
        }

        _ = positCalculator.AddPositsInArray(posit8E3Array);
    }

    public static void RunSoftwareBenchmarks(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
    {
        var positCalculator = new Posit8E3Calculator();

        // Not to run the benchmark below the first time, because JIT compiling can affect it.
        positCalculator.CalculateIntegerSumUpToNumber(100000);
        var sw = Stopwatch.StartNew();
        var integerSumUpToNumber = positCalculator.CalculateIntegerSumUpToNumber(
            100000,
            hastlayer,
            hardwareRepresentation.HardwareGenerationConfiguration);
        sw.Stop();

        const string elapsed = "Elapsed: ";

        Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant("Result of counting up to 100000: ", integerSumUpToNumber));
        Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant(elapsed, sw.ElapsedMilliseconds, "ms"));

        Console.WriteLine();

        positCalculator.CalculatePowerOfReal(100000, 1.0001F, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        sw = Stopwatch.StartNew();
        var powerOfReal = positCalculator.CalculatePowerOfReal(
            5,
            0.5F,
            hastlayer,
            hardwareRepresentation.HardwareGenerationConfiguration);
        sw.Stop();

        Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant("Result of power of real number: ", powerOfReal));
        Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant(elapsed, sw.ElapsedMilliseconds, "ms"));

        Console.WriteLine();

        var numbers = new int[Posit8E3Calculator.MaxDegreeOfParallelism];
        for (int i = 0; i < Posit8E3Calculator.MaxDegreeOfParallelism; i++)
        {
            numbers[i] = 100000 + (i % 2 == 0 ? -1 : 1);
        }

        positCalculator.ParallelizedCalculateIntegerSumUpToNumbers(numbers, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        sw = Stopwatch.StartNew();
        var integerSumsUpToNumbers = positCalculator.ParallelizedCalculateIntegerSumUpToNumbers(
            numbers,
            hastlayer,
            hardwareRepresentation.HardwareGenerationConfiguration);
        sw.Stop();

        Console.WriteLine(
            "Result of counting up to ~100000 parallelized: " +
            string.Join(", ", integerSumsUpToNumbers.Select(number => number.ToTechnicalString())));
        Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant(elapsed, sw.ElapsedMilliseconds, "ms"));

        Console.WriteLine();

        var posit8E3Array = new uint[100000];

        for (var i = 0; i < 100000; i++)
        {
            posit8E3Array[i] = i % 2 == 0
                ? new Posit8E3(0.25F * 2 * i).PositBits
                : new Posit8E3(0.25F * -2 * i).PositBits;
        }

        positCalculator.AddPositsInArray(posit8E3Array, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        sw = Stopwatch.StartNew();
        var positsInArraySum = positCalculator.AddPositsInArray(
            posit8E3Array,
            hastlayer,
            hardwareRepresentation.HardwareGenerationConfiguration);
        sw.Stop();

        Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant("Result of addition of posits in array: ", positsInArraySum));
        Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant(elapsed, sw.ElapsedMilliseconds, "ms"));
        Console.WriteLine();
    }
}
