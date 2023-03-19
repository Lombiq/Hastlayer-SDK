using Hast.Layer;
using Hast.TestInputs.Dynamic;
using Hast.Transformer.Vhdl.Configuration;
using Lombiq.HelpfulLibraries.Common.Utilities;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hast.DynamicTests;

[SuppressMessage(
    "Globalization",
    "CA1303:Do not pass literals as localized parameters",
    Justification = "There needn't be localization for testing.")]
internal static class TestExecutor
{
    public static Task ExecuteSelectedTestAsync<T>(Expression<Action<T>> caseSelector, Action<T> testExecutor)
        where T : DynamicTestInputBase, new() =>
        ExecuteTestAsync(configuration => configuration.AddHardwareEntryPointMethod(caseSelector), testExecutor);

    public static async Task ExecuteTestAsync<T>(Action<HardwareGenerationConfiguration> configurator, Action<T> testExecutor)
        where T : DynamicTestInputBase, new()
    {
        using var hastlayer = Hastlayer.Create();
        var configuration = new HardwareGenerationConfiguration("Nexys A7", "HardwareFramework");

        configurator(configuration);

        configuration.VhdlTransformerConfiguration().VhdlGenerationConfiguration = VhdlGenerationConfiguration.Debug;

        hastlayer.ExecutedOnHardware += (_, e) =>
            Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant(
                "Executing on hardware took ",
                e.Arguments.HardwareExecutionInformation.HardwareExecutionTimeMilliseconds,
                " milliseconds (net) ",
                e.Arguments.HardwareExecutionInformation.FullExecutionTimeMilliseconds,
                " milliseconds (all together)."));

        Console.WriteLine("Hardware generation starts.");
        var hardwareRepresentation = await hastlayer.GenerateHardwareAsync(
            new[]
            {
                typeof(T).Assembly,
            },
            configuration);

        Console.WriteLine("Hardware generated, starting hardware execution.");
        var proxyGenerationConfiguration = new ProxyGenerationConfiguration { VerifyHardwareResults = true };
        var hardwareInstance = await hastlayer.GenerateProxyAsync(
            hardwareRepresentation,
            new T(),
            proxyGenerationConfiguration);

        hardwareInstance.Hastlayer = hastlayer;
        hardwareInstance.HardwareGenerationConfiguration = configuration;
        testExecutor(hardwareInstance);

        Console.WriteLine("Hardware execution finished.");
        Console.ReadKey();
    }
}
