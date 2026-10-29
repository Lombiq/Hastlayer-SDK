using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Lombiq.Arithmetics;
using Lombiq.HelpfulLibraries.Common.Utilities;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners;

[SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "This application is not localized.")]
internal sealed class Posit32AdvancedCalculatorSampleRunner : ISampleRunner
{
    public void Configure(HardwareGenerationConfiguration configuration) =>
        configuration.AddHardwareEntryPointType<Posit32AdvancedCalculator>();

    public async Task RunAsync(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
    {
        RunSoftwareBenchmarks();
        var positCalculator = await hastlayer.GenerateProxyAsync(hardwareRepresentation, new Posit32AdvancedCalculator(), configuration);

        positCalculator.RepeatedDivision(10, 153157.898526F, 3.3F, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);

        var sqrtInputArray = new uint[10];
        for (int i = 0; i < 10; i++)
        {
            sqrtInputArray[i] = new Posit32((float)(i + 1) * (i + 1)).PositBits;
        }

        positCalculator.SqrtOfPositsInArray(sqrtInputArray, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
    }

    public static void RunSoftwareBenchmarks()
    {
        var positCalculator = new Posit32AdvancedCalculator();

        positCalculator.RepeatedDivision(10, 153157.898526F, 3.3F);
        var sw = Stopwatch.StartNew();
        var resultOfDivision = positCalculator.RepeatedDivision(10, 153157.898526F, 3.3F);
        sw.Stop();

        Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant("Result of repeated division: ", resultOfDivision));
        Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant("Elapsed: ", sw.ElapsedMilliseconds, "ms"));

        var sqrtInputArray = new uint[10];
        for (int i = 0; i < 10; i++)
        {
            sqrtInputArray[i] = new Posit32((float)(i + 1) * (i + 1)).PositBits;
        }

        sw = Stopwatch.StartNew();
        var resultOfSqrt = positCalculator.SqrtOfPositsInArray(sqrtInputArray);
        sw.Stop();

        Console.WriteLine("Result of sqrt: ");
        for (int i = 0; i < resultOfSqrt.Length; i++)
        {
            Console.Write(resultOfSqrt[i].ToString(CultureInfo.InvariantCulture) + ", ");
        }

        Console.WriteLine();
        Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant("Elapsed: ", sw.ElapsedMilliseconds, "ms"));
    }
}
