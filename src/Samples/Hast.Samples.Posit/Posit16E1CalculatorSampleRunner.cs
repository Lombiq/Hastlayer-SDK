using Hast.Layer;
using Hast.Samples.Posit.Extensions;
using Lombiq.Arithmetics;
using Lombiq.HelpfulLibraries.Common.Utilities;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Samples.Posit;

internal static class Posit16E1CalculatorSampleRunner
{
    public static void Configure(HardwareGenerationConfiguration configuration) =>
        configuration.AddHardwareEntryPointType<Posit16E1Calculator>();

    public static async Task RunAsync(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
    {
        RunSoftwareBenchmarks(hastlayer, hardwareRepresentation);

        var positCalculator = await hastlayer.GenerateProxyAsync(hardwareRepresentation, new Posit16E1Calculator());

        _ = positCalculator.CalculateIntegerSumUpToNumber(
            100000,
            hastlayer,
            hardwareRepresentation.HardwareGenerationConfiguration);

        positCalculator.CalculatePowerOfReal(
            10000,
            1.015625F,
            hastlayer,
            hardwareRepresentation.HardwareGenerationConfiguration);

        var numbers = new int[Posit16E1Calculator.MaxDegreeOfParallelism];
        for (int i = 0; i < Posit16E1Calculator.MaxDegreeOfParallelism; i++)
        {
            numbers[i] = 100000 + (i % 2 == 0 ? -1 : 1);
        }

        _ = positCalculator.ParallelizedCalculateIntegerSumUpToNumbers(
            numbers,
            hastlayer,
            hardwareRepresentation.HardwareGenerationConfiguration);

        var posit16E1Array = new uint[100000];

        for (var i = 0; i < 100000; i++)
        {
            posit16E1Array[i] = i % 2 == 0
                ? new Posit16E1(0.25F * 2 * i).PositBits
                : new Posit16E1(0.25F * -2 * i).PositBits;
        }

        _ = positCalculator.AddPositsInArray(posit16E1Array);
    }

    public static void RunSoftwareBenchmarks(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
    {
        var positCalculator = new Posit16E1Calculator();

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
            10000,
            1.015625F,
            hastlayer,
            hardwareRepresentation.HardwareGenerationConfiguration);
        sw.Stop();

        Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant("Result of power of real number: ", powerOfReal));
        Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant(elapsed, sw.ElapsedMilliseconds, "ms"));

        Console.WriteLine();

        var numbers = new int[Posit16E1Calculator.MaxDegreeOfParallelism];
        for (int i = 0; i < Posit16E1Calculator.MaxDegreeOfParallelism; i++)
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

        var posit16E1Array = new uint[100000];

        for (var i = 0; i < 100000; i++)
        {
            posit16E1Array[i] = i % 2 == 0
                ? new Posit16E1(0.25F * 2 * i).PositBits
                : new Posit16E1(0.25F * -2 * i).PositBits;
        }

        positCalculator.AddPositsInArray(posit16E1Array, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        sw = Stopwatch.StartNew();
        var positsInArraySum = positCalculator.AddPositsInArray(
            posit16E1Array,
            hastlayer,
            hardwareRepresentation.HardwareGenerationConfiguration);
        sw.Stop();

        Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant("Result of addition of posits in array: ", positsInArraySum));
        Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant(elapsed, sw.ElapsedMilliseconds, "ms"));
        Console.WriteLine();
    }
}
