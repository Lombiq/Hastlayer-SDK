using Hast.Layer;
using Hast.Transformer.Vhdl.Abstractions.Configuration;
using Lombiq.Arithmetics;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Samples.Posit
{
    internal static class Configuration
    {
        public static string DeviceName = "Nexys A7";
        public static Sample SampleToRun = Sample.Posit32_0_Calculator;
        public static string HardwareFrameworkPath = "HardwareFramework";
    }

    internal class Program
    {
        private static void Main()
        {
            Task.Run(async () =>
            {
                /*
                 * On a high level these are the steps to use Hastlayer:
                 * 1. Create the Hastlayer shell.
                 * 2. Configure hardware generation and generate FPGA hardware representation of the given .NET code.
                 * 3. Generate proxies for hardware-transformed types and use these proxies to utilize hardware
                 *    implementations. (You can see this inside the SampleRunners.)
                 */

                // Configuring the Hastlayer shell. Which flavor should we use? If you're unsure then you'll need
                // the Client flavor: This will let you connect to a remote Hastlayer service to run the software
                // to hardware transformation.
                var hastlayerConfiguration = new HastlayerConfiguration { Flavor = HastlayerFlavor.Developer };

                // Initializing a Hastlayer shell. Since this is non-trivial to do you can cache this shell object 
                // while the program runs and re-use it continuously. No need to always wrap it into a using() like 
                // here, just make sure to Dispose() it before the program terminates.
                using var hastlayer = Hastlayer.Create(hastlayerConfiguration);

                // Hooking into an event of Hastlayer so some execution information can be made visible on the
                // console.
                hastlayer.ExecutedOnHardware += (sender, e) =>
                    Console.WriteLine(@$"Executing {e.Arguments.MemberFullName} on hardware took
{e.Arguments.HardwareExecutionInformation.HardwareExecutionTimeMilliseconds} milliseconds (net)
{e.Arguments.HardwareExecutionInformation.FullExecutionTimeMilliseconds} milliseconds (all together).");


                // We need to set what kind of device (FPGA/FPGA board) to generate the hardware for.
                var devices = hastlayer.GetSupportedDevices();
                // Let's just use the first one that is available. However you might want to use a specific
                // device, not just any first one.
                var configuration = new HardwareGenerationConfiguration(
                    Configuration.DeviceName,
                    Configuration.HardwareFrameworkPath);

                // If you're running Hastlayer in the Client flavor, you also need to configure some credentials
                // here:
                var remoteClientConfiguration = configuration.RemoteClientConfiguration();
                remoteClientConfiguration.AppName = "TestApp";
                remoteClientConfiguration.AppSecret = "appsecret";
                if (hastlayerConfiguration.Flavor == HastlayerFlavor.Client &&
                    remoteClientConfiguration.AppSecret == "appsecret")
                {
                    throw new InvalidOperationException("You haven't changed the default remote credentials!" +
                        " Write to crew@hastlayer.com to receive access if you don't have yet.");
                }


                // Letting the configuration of samples run. Check out those methods too!
                switch (Configuration.SampleToRun)
                {
                    case Sample.Posit8_0_Calculator:
                        Posit8_0_CalculatorSampleRunner.Configure(configuration);
                        break;
                    case Sample.Posit8_1_Calculator:
                        Posit8_1_CalculatorSampleRunner.Configure(configuration);
                        break;
                    case Sample.Posit8_2_Calculator:
                        Posit8_2_CalculatorSampleRunner.Configure(configuration);
                        break;
                    case Sample.Posit8_3_Calculator:
                        Posit8_3_CalculatorSampleRunner.Configure(configuration);
                        break;
                    case Sample.Posit16_0_Calculator:
                        Posit16_0_CalculatorSampleRunner.Configure(configuration);
                        break;
                    case Sample.Posit16_1_Calculator:
                        Posit16_1_CalculatorSampleRunner.Configure(configuration);
                        break;
                    case Sample.Posit16_2_Calculator:
                        Posit16_2_CalculatorSampleRunner.Configure(configuration);
                        break;
                    case Sample.Posit16_3_Calculator:
                        Posit16_3_CalculatorSampleRunner.Configure(configuration);
                        break;
                    case Sample.Posit32_0_Calculator:
                        Posit32_0_CalculatorSampleRunner.Configure(configuration);
                        break;
                    case Sample.Posit32_1_Calculator:
                        Posit32_1_CalculatorSampleRunner.Configure(configuration);
                        break;
                    case Sample.Posit32_2_Calculator:
                        Posit32_2_CalculatorSampleRunner.Configure(configuration);
                        break;
                    case Sample.Posit32_3_Calculator:
                        Posit32_3_CalculatorSampleRunner.Configure(configuration);
                        break;
                    default:
                        break;
                }

                // The generated VHDL code will contain debug-level information, though it will be slower to
                // create.
                configuration.VhdlTransformerConfiguration().VhdlGenerationConfiguration =
                    VhdlGenerationConfiguration.Debug;

                Console.WriteLine("Hardware generation starts.");

                // Generating hardware from the sample assembly with the given configuration.
                var hardwareRepresentation = await hastlayer.GenerateHardwareAsync(
                    new[]
                    {
                        typeof(Posit8_0).Assembly,
                        typeof(Posit8_0_Calculator).Assembly,
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
                            hardwareRepresentation.HardwareDescription.Warnings.Select(warning => $"* {warning}")) +
                        Environment.NewLine);
                }

                Console.WriteLine("Starting hardware execution.");

                // Running samples.
                switch (Configuration.SampleToRun)
                {
                    case Sample.Posit8_0_Calculator:
                        await Posit8_0_CalculatorSampleRunner.Run(hastlayer, hardwareRepresentation);
                        break;
                    case Sample.Posit8_1_Calculator:
                        await Posit8_1_CalculatorSampleRunner.Run(hastlayer, hardwareRepresentation);
                        break;
                    case Sample.Posit8_2_Calculator:
                        await Posit8_2_CalculatorSampleRunner.Run(hastlayer, hardwareRepresentation);
                        break;
                    case Sample.Posit8_3_Calculator:
                        await Posit8_3_CalculatorSampleRunner.Run(hastlayer, hardwareRepresentation);
                        break;
                    case Sample.Posit16_0_Calculator:
                        await Posit16_0_CalculatorSampleRunner.Run(hastlayer, hardwareRepresentation);
                        break;
                    case Sample.Posit16_1_Calculator:
                        await Posit16_1_CalculatorSampleRunner.Run(hastlayer, hardwareRepresentation);
                        break;
                    case Sample.Posit16_2_Calculator:
                        await Posit16_2_CalculatorSampleRunner.Run(hastlayer, hardwareRepresentation);
                        break;
                    case Sample.Posit16_3_Calculator:
                        await Posit16_3_CalculatorSampleRunner.Run(hastlayer, hardwareRepresentation);
                        break;
                    case Sample.Posit32_0_Calculator:
                        await Posit32_0_CalculatorSampleRunner.Run(hastlayer, hardwareRepresentation);
                        break;
                    case Sample.Posit32_1_Calculator:
                        await Posit32_1_CalculatorSampleRunner.Run(hastlayer, hardwareRepresentation);
                        break;
                    case Sample.Posit32_2_Calculator:
                        await Posit32_2_CalculatorSampleRunner.Run(hastlayer, hardwareRepresentation);
                        break;
                    case Sample.Posit32_3_Calculator:
                        await Posit32_3_CalculatorSampleRunner.Run(hastlayer, hardwareRepresentation);
                        break;
                    default:
                        break;
                }
            }).Wait();

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
