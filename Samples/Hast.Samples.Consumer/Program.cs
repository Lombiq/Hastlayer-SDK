using Castle.Core.Internal;
using Hast.Algorithms;
using Hast.Common.Enums;
using Hast.Communication.Exceptions;
using Hast.Layer;
using Hast.Samples.Consumer.Models;
using Hast.Samples.Consumer.SampleRunners;
using Hast.Samples.FSharpSampleAssembly;
using Hast.Samples.SampleAssembly;
using Hast.Transformer.Vhdl.Abstractions.Configuration;
using Lombiq.Arithmetics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer
{
    // In this simple console application we generate hardware from some sample algorithms. Note that the project also
    // references other projects (and the sample assembly as well), so check out those too on hints which Hastlayer
    // projects to reference from your own projects.

    // Configure the whole sample project in command line arguments. See Models/ConsumerConfiguration.cs for details.

    internal static class Program
    {
        private static async Task MainTask(string[] args)
        {
            /*
            * On a high level these are the steps to use Hastlayer:
            * 1. Create the Hastlayer shell.
            * 2. Configure hardware generation and generate FPGA hardware representation of the given .NET code.
            * 3. Generate proxies for hardware-transformed types and use these proxies to utilize hardware
            *    implementations. (You can see this inside the SampleRunners.)
            */

            // Here we create the ConsumerConfiguration used to set up this application. Check out the
            // ConsumerConfiguration class to see what can be changed to try out the various samples and available
            // devices!
            // If there are command line arguments, they are used to create the configuration. Otherwise we open an
            // interactive user interface where you can hand pick the options, load or save them. This GUI has mouse
            // support on Windows and certain Linux platforms, though not through SSH. You can fully navigate using the
            // keyboard too. Use the <c>-load name</c> and the <c>-save name</c> command line arguments to save/load
            // configurations into the ConsumerConfiguration.json file without the GUI.
            var savedConfigurations = await ConsumerConfiguration.LoadConfigurationsAsync();
            var consumerConfiguration = args.Any()
                ? ConsumerConfiguration.FromCommandLine(args, savedConfigurations)
                : new Gui(savedConfigurations).BuildConfiguration();
            if (consumerConfiguration == null) return;

            if (!consumerConfiguration.AppSecret.IsNullOrEmpty()) hastlayerConfiguration.Flavor = HastlayerFlavor.Client;

            // Initializing a Hastlayer shell. Since this is non-trivial to do you can cache this shell object while
            // the program runs and re-use it continuously. No need to always wrap it into a using() like here, just
            // make sure to Dispose() it before the program terminates.
            using var hastlayer = Hastlayer.Create(hastlayerConfiguration);
            // Hooking into an event of Hastlayer so some execution information can be made visible on the
            // console.
            hastlayer.ExecutedOnHardware += (_, e) =>
            {
                var netTime = e.HardwareExecutionInformation.HardwareExecutionTimeMilliseconds;
                var grossTime = e.HardwareExecutionInformation.FullExecutionTimeMilliseconds;

                Console.WriteLine(
                    $"Executing {e.MemberFullName} on hardware took {netTime:0.####} milliseconds (net), " +
                    $"{grossTime:0.####} milliseconds (all together).");

                if (e.SoftwareExecutionInformation == null) return;

                // This will be available in case we've set ProxyGenerationConfiguration.VerifyHardwareResults to true,
                // see the notes below, or if the hardware execution was canceled.
                var softwareTime = e.SoftwareExecutionInformation.SoftwareExecutionTimeMilliseconds;
                Console.WriteLine($"The verifying software execution took {softwareTime:0.####} milliseconds.");
            };

            // We need to set what kind of device (FPGA/FPGA board) to generate the hardware for.
            var devices = hastlayer.GetSupportedDevices()?.ToList();
            if (devices == null || !devices.Any()) throw new Exception("No devices are available!");

            // Let's just use the first one that is available unless it's specified.
            var targetDeviceName = consumerConfiguration.DeviceName.OrIfEmpty(devices.First().Name);
            var selectedDevice = devices.FirstOrDefault(device => device.Name == targetDeviceName);
            if (selectedDevice == null) throw new Exception($"Target device '{targetDeviceName}' not found!");

            var configuration = new HardwareGenerationConfiguration(selectedDevice.Name, consumerConfiguration.HardwareFrameworkPath);
            var proxyConfiguration = new ProxyGenerationConfiguration
            {
                VerifyHardwareResults = consumerConfiguration.VerifyResults,
            };

            configuration.SingleBinaryPath = GetArgument("bin") ?? GetArgument("binary");

            // If you're running Hastlayer in the Client flavor, you also need to configure some credentials:
            ConfigureClientFlavor(configuration, hastlayerConfiguration, consumerConfiguration);

            // Letting the configuration of samples run. Check out those methods too!
            ISampleRunner sampleRunner = consumerConfiguration.SampleToRun switch
            {
                Sample.Fix64Calculator => new Fix64CalculatorSampleRunner(),
                Sample.FSharpParallelAlgorithm => new FSharpParallelAlgorithmSampleRunner(),
                Sample.GenomeMatcher => new GenomeMatcherSampleRunner(),
                Sample.ParallelAlgorithm => new ParallelAlgorithmSampleRunner(),
                Sample.ImageProcessingAlgorithms => new ImageProcessingAlgorithmsSampleRunner(),
                Sample.Loopback => new LoopbackSampleRunner(),
                Sample.MemoryTest => new MemoryTestSampleRunner(),
                Sample.MonteCarloPiEstimator => new MonteCarloPiEstimatorSampleRunner(),
                Sample.ObjectOrientedShowcase => new ObjectOrientedShowcaseSampleRunner(),
                Sample.PositCalculator => new PositCalculatorSampleRunner(),
                Sample.Posit32AdvancedCalculator => new Posit32AdvancedCalculatorSampleRunner(),
                Sample.Posit32Calculator => new Posit32CalculatorSampleRunner(),
                Sample.Posit32FusedCalculator => new Posit32FusedCalculatorSampleRunner(),
                Sample.PrimeCalculator => new PrimeCalculatorSampleRunner(),
                Sample.RecursiveAlgorithms => new RecursiveAlgorithmsSampleRunner(),
                Sample.SimdCalculator => new SimdCalculatorSampleRunner(),
                Sample.UnumCalculator => new UnumCalculatorSampleRunner(),
                _ => throw new Exception($"Unknown sample '{consumerConfiguration.SampleToRun}'."),
            };
            sampleRunner.Configure(configuration);
            configuration.Label = consumerConfiguration.BuildLabel ?? consumerConfiguration.SampleToRun.ToString();

            // The generated VHDL code will contain debug-level information, though it will be slower to create.
            configuration.VhdlTransformerConfiguration().VhdlGenerationConfiguration = VhdlGenerationConfiguration.Debug;

            Console.WriteLine("Hardware generation starts.");

            // Generating hardware from the sample assembly with the given configuration. Be sure to use Debug
            // assemblies!
            var hardwareRepresentation = await hastlayer.GenerateHardware(
                new[]
                {
                    // Selecting any type from the sample assembly here just to get its Assembly object.
                    typeof(PrimeCalculator).Assembly,
                    typeof(Fix64).Assembly,
                    typeof(FSharpParallelAlgorithmContainer).Assembly,
                    // Note that the assemblies used by code to be transformed also need to be added
                    // separately. E.g. Posit is used by Hast.Samples.SampleAssembly which in turn also uses
                    // ImmutableArray.
                    typeof(Posit).Assembly,
                    typeof(ImmutableArray).Assembly,
                    // Only necessary for the F# sample.
                    typeof(Microsoft.FSharp.Core.LiteralAttribute).Assembly,
                },
                configuration);

            Console.WriteLine("Hardware generation finished.\n");

            // Be sure to check out transformation warnings. Most of the time the issues noticed shouldn't cause
            // any problems, but sometimes they can.
            if (hardwareRepresentation.HardwareDescription.Warnings.Any())
            {
                Console.WriteLine("There were transformation warnings in the logs, which may hint on issues that can " +
                                  "cause the hardware implementation to produce incorrect results.\n");
            }

            if (consumerConfiguration.GenerateHardwareOnly) Environment.Exit(0);

            Console.WriteLine("Starting hardware execution.");

            // Running the selected sample. It is executed 3 times to ensure all JIT compilation overhead has been
            // eliminated by successive reruns. The last attempt is a better representation of performance in
            // long-running applications.
            try
            {
                for (var i = 0; i < 3; i++)
                {
                    Console.WriteLine("\n\n\n\n\n\nAttempt #{0:0}:", i + 1);
                    await sampleRunner.Run(hastlayer, hardwareRepresentation, proxyConfiguration);
                }
            }
            catch (AggregateException ex) when (ex.InnerException is HardwareExecutionResultMismatchException exception)
            {
                // If you set ProxyGenerationConfiguration.VerifyHardwareResults to true (when calling
                // GenerateProxy()) then everything will be computed in software as well to check the hardware.
                // You'll get such an exception if there is any mismatch. This shouldn't normally happen, but it's
                // not impossible in corner cases.
                var mismatches = exception
                    .Mismatches?
                    .ToList() ?? new List<HardwareExecutionResultMismatchException.Mismatch>();
                var mismatchCount = mismatches.Count;
                Console.WriteLine(
                    $"There {(mismatchCount == 1 ? "was a mismatch" : $"were {mismatchCount} mismatches")} between " +
                    $"the software and hardware execution's results! The memory length is {exception.CellCount}. " +
                    $"Mismatch{(mismatchCount == 1 ? string.Empty : "es")}:");

                foreach (var mismatch in mismatches)
                {
                    Console.WriteLine("* " + mismatch);
                }
            }
        }

        private static void ConfigureClientFlavor(
            HardwareGenerationConfiguration configuration,
            HastlayerConfiguration hastlayerConfiguration,
            ConsumerConfiguration consumerConfiguration)
        {
            if (hastlayerConfiguration.Flavor != HastlayerFlavor.Client) return;
            var remoteClientConfiguration = configuration.RemoteClientConfiguration();

            if (!string.IsNullOrWhiteSpace(consumerConfiguration.Endpoint) &&
                Uri.TryCreate(consumerConfiguration.Endpoint, UriKind.Absolute, out var endpointUri))
            {
                remoteClientConfiguration.EndpointBaseUri = endpointUri;
            }

            remoteClientConfiguration.AppName = consumerConfiguration.AppName;
            remoteClientConfiguration.AppSecret = consumerConfiguration.AppSecret;

            if (string.IsNullOrEmpty(remoteClientConfiguration.AppSecret))
            {
                throw new InvalidOperationException(
                    "You haven't provided the remote credentials! Register on hastlayer.com to receive access if you don't have it yet.");
            }
        }

        private static async Task Main(string[] args)
        {
            // Wrapping the whole program into a try-catch here so it's a bit more convenient above.
            try
            {
                await MainTask(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }
    }
}
