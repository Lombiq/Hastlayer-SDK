using Hast.Layer;
using Hast.Samples.Posit;
using Hast.Transformer.Vhdl.Configuration;
using Lombiq.Arithmetics;
using Lombiq.HelpfulLibraries.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/*
 * On a high level these are the steps to use Hastlayer:
 * 1. Create the Hastlayer shell.
 * 2. Configure hardware generation and generate FPGA hardware representation of the given .NET code.
 * 3. Generate proxies for hardware-transformed types and use these proxies to utilize hardware
 *    implementations. (You can see this inside the SampleRunners.)
 */

var sampleRunners = new Dictionary<
    Sample,
    (Action<HardwareGenerationConfiguration> Configure, Func<IHastlayer, IHardwareRepresentation, Task> RunAsync)>
{
    [Sample.Posit8E0Calculator] = (Posit8E0CalculatorSampleRunner.Configure, Posit8E0CalculatorSampleRunner.RunAsync),
    [Sample.Posit8E1Calculator] = (Posit8E1CalculatorSampleRunner.Configure, Posit8E1CalculatorSampleRunner.RunAsync),
    [Sample.Posit8E2Calculator] = (Posit8E2CalculatorSampleRunner.Configure, Posit8E2CalculatorSampleRunner.RunAsync),
    [Sample.Posit8E3Calculator] = (Posit8E3CalculatorSampleRunner.Configure, Posit8E3CalculatorSampleRunner.RunAsync),
    [Sample.Posit8E4Calculator] = (Posit8E4CalculatorSampleRunner.Configure, Posit8E4CalculatorSampleRunner.RunAsync),
    [Sample.Posit16E0Calculator] = (Posit16E0CalculatorSampleRunner.Configure, Posit16E0CalculatorSampleRunner.RunAsync),
    [Sample.Posit16E1Calculator] = (Posit16E1CalculatorSampleRunner.Configure, Posit16E1CalculatorSampleRunner.RunAsync),
    [Sample.Posit16E2Calculator] = (Posit16E2CalculatorSampleRunner.Configure, Posit16E2CalculatorSampleRunner.RunAsync),
    [Sample.Posit16E3Calculator] = (Posit16E3CalculatorSampleRunner.Configure, Posit16E3CalculatorSampleRunner.RunAsync),
    [Sample.Posit16E4Calculator] = (Posit16E4CalculatorSampleRunner.Configure, Posit16E4CalculatorSampleRunner.RunAsync),
    [Sample.Posit32E0Calculator] = (Posit32E0CalculatorSampleRunner.Configure, Posit32E0CalculatorSampleRunner.RunAsync),
    [Sample.Posit32E1Calculator] = (Posit32E1CalculatorSampleRunner.Configure, Posit32E1CalculatorSampleRunner.RunAsync),
    [Sample.Posit32E2Calculator] = (Posit32E2CalculatorSampleRunner.Configure, Posit32E2CalculatorSampleRunner.RunAsync),
    [Sample.Posit32E3Calculator] = (Posit32E3CalculatorSampleRunner.Configure, Posit32E3CalculatorSampleRunner.RunAsync),
    [Sample.Posit32E4Calculator] = (Posit32E4CalculatorSampleRunner.Configure, Posit32E4CalculatorSampleRunner.RunAsync),
};

if (!sampleRunners.TryGetValue(Configuration.SampleToRun, out var sampleRunner))
{
    throw new InvalidOperationException(
        StringHelper.CreateInvariant($"There is no sample runner for {Configuration.SampleToRun}."));
}

// Initializing a Hastlayer shell. Since this is non-trivial to do you can cache this shell object
// while the program runs and re-use it continuously. No need to always wrap it into a using() like
// here, just make sure to Dispose() it before the program terminates.
using var hastlayer = Hastlayer.Create();

// Hooking into an event of Hastlayer so some execution information can be made visible on the
// console.
hastlayer.ExecutedOnHardware += (_, e) =>
    Console.WriteLine(StringHelper.CreateInvariant(@$"Executing {e.Arguments.MemberFullName} on hardware took
{e.Arguments.HardwareExecutionInformation.HardwareExecutionTimeMilliseconds} milliseconds (net)
{e.Arguments.HardwareExecutionInformation.FullExecutionTimeMilliseconds} milliseconds (all together)."));

// We need to set what kind of device (FPGA/FPGA board) to generate the hardware for.
var configuration = new HardwareGenerationConfiguration(
    Configuration.DeviceName,
    Configuration.HardwareFrameworkPath);

// Letting the configuration of samples run. Check out those methods too!
sampleRunner.Configure(configuration);

// The generated VHDL code will contain debug-level information, though it will be slower to
// create.
configuration.VhdlTransformerConfiguration().VhdlGenerationConfiguration =
    VhdlGenerationConfiguration.Debug;

Console.WriteLine("Hardware generation starts.");

// Generating hardware from the sample assembly with the given configuration.
var hardwareRepresentation = await hastlayer.GenerateHardwareAsync(
    new[]
    {
        typeof(Posit8E0).Assembly,
        typeof(Posit8E0Calculator).Assembly,
    },
    configuration);

Console.WriteLine($"Hardware generation finished.{Environment.NewLine}");

// Be sure to check out transformation warnings. Most of the time the issues noticed shouldn't
// cause any problems, but sometimes they can.
if (hardwareRepresentation.HardwareDescription.Warnings.Any())
{
    Console.WriteLine(
        "There were the following transformation warnings, which may hint on issues " +
        $"that can cause the hardware implementation to produce incorrect results:{Environment.NewLine}" +
        string.Join(
            Environment.NewLine,
            hardwareRepresentation.HardwareDescription.Warnings.Select(warning => StringHelper.CreateInvariant($"* {warning}"))) +
        Environment.NewLine);
}

Console.WriteLine("Starting hardware execution.");

// Running samples.
await sampleRunner.RunAsync(hastlayer, hardwareRepresentation);

Console.WriteLine("Press any key to exit.");
Console.ReadKey();

namespace Hast.Samples.Posit
{
    internal static class Configuration
    {
        public const string DeviceName = "Nexys A7";
        public const string HardwareFrameworkPath = "HardwareFramework";
        public const Sample SampleToRun = Sample.Posit32E0Calculator;
    }
}
