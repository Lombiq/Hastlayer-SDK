using Hast.Algorithms;
using Hast.Communication.Exceptions;
using Hast.Layer;
using Hast.Samples.Consumer.SampleRunners;
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

    // Configure the whole sample project here or in command line arguments:
    internal static class Configuration
    {
        /// <summary>
        /// Which supported hardware device to use? If you leave this empty the first one will be used. If you're
        /// testing Hastlayer locally then you'll need to use the "Nexys A7" or "Nexys4 DDR" devices; for
        /// high-performance local or cloud FPGAs see the docs.
        /// You can also provide this in the -device command line argument.
        /// </summary>
        public static string DeviceName = "Nexys A7";

        /// <summary>
        /// If you're running Hastlayer in the Client flavor, you need to configure your credentials here. Here the
        /// name of your app.
        /// You can also provide this in the -appname command line argument.
        /// </summary>
        public static string AppName = "appname";

        /// <summary>
        /// If you're running Hastlayer in the Client flavor, you need to configure your credentials here. Here the
        /// app secret corresponding to of your app.
        /// You can also provide this in the -appsecret command line argument.
        /// </summary>
        public static string AppSecret = "appsecret";

        /// <summary>
        /// Which sample algorithm to transform and run? Choose one. Currently the GenomeMatcher sample is not up-to-date
        /// enough and shouldn't be really taken as good examples (check out the other ones).
        /// You can also provide this in the -sample command line argument.
        /// </summary>
        public static Sample SampleToRun = Sample.Loopback;

        /// <summary>
        /// Specify a path here where the hardware framework is located. The file describing the hardware to be
        /// generated will be saved there as well as anything else necessary. If the path is relative (like the
        /// default) then the file will be saved along this project's executable in the bin output directory.
        /// </summary>
        public static string HardwareFrameworkPath = "HardwareFramework";
    }

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

            // Configuring the Hastlayer shell. Which flavor should we use? If you're unsure then you'll need
            // the Client flavor: This will let you connect to a remote Hastlayer service to run the software
            // to hardware transformation. In most cases the flavor defaults to the one you need.
            // var hastlayerConfiguration = new HastlayerConfiguration { Flavor = HastlayerFlavor.Client };
            var hastlayerConfiguration = new HastlayerConfiguration();

            // Initializing a Hastlayer shell. Since this is non-trivial to do you can cache this shell object
            // while the program runs and re-use it continuously. No need to always wrap it into a using() like
            // here, just make sure to Dispose() it before the program terminates.
            using var hastlayer = Hastlayer.Create(hastlayerConfiguration);
            // Hooking into an event of Hastlayer so some execution information can be made visible on the
            // console.
            hastlayer.ExecutedOnHardware += (sender, e) =>
            {
                Console.WriteLine(
                    "Executing " +
                    e.MemberFullName +
                    " on hardware took " +
                    e.HardwareExecutionInformation.HardwareExecutionTimeMilliseconds.ToString("0.####") +
                    " milliseconds (net) " +
                    e.HardwareExecutionInformation.FullExecutionTimeMilliseconds.ToString("0.####") +
                    " milliseconds (all together).");

                if (e.SoftwareExecutionInformation != null)
                {
                    // This will be available in case we've set ProxyGenerationConfiguration.VerifyHardwareResults
                    // to true, see the notes below, or if the hardware execution was canceled.
                    Console.WriteLine($"The software execution took {e.SoftwareExecutionInformation.SoftwareExecutionTimeMilliseconds} milliseconds.");
                }
            };


            // A little helper for later.
            var argsList = (IList<string>)args;
            string GetArgument(string name)
            {
                name = "-" + name;
                return args.Contains(name) ? args[argsList.IndexOf(name) + 1] : null;
            }

            // We need to set what kind of device (FPGA/FPGA board) to generate the hardware for.
            var devices = hastlayer.GetSupportedDevices()?.ToList();
            if (devices == null || !devices.Any()) throw new Exception("No devices are available!");

            // Let's just use the first one that is available unless it's specified.
            if (string.IsNullOrEmpty(Configuration.DeviceName)) Configuration.DeviceName = devices.First().Name;
            var targetDeviceName = GetArgument("device") ?? Configuration.DeviceName;
            var selectedDevice = devices.FirstOrDefault(device => device.Name == targetDeviceName);
            if (selectedDevice == null) throw new Exception($"Target device '{targetDeviceName}' not found!");

            var configuration = new HardwareGenerationConfiguration(selectedDevice.Name, Configuration.HardwareFrameworkPath);
            var proxyConfiguration = new ProxyGenerationConfiguration();
            if (argsList.Contains("-verify")) proxyConfiguration.VerifyHardwareResults = true;

            // If you're running Hastlayer in the Client flavor, you also need to configure some credentials:
            var remoteClientConfiguration = configuration.RemoteClientConfiguration();
            remoteClientConfiguration.AppName = GetArgument("appname") ?? Configuration.AppName;
            remoteClientConfiguration.AppSecret = GetArgument("appsecret") ?? Configuration.AppSecret;
            if (hastlayerConfiguration.Flavor == HastlayerFlavor.Client &&
                remoteClientConfiguration.AppSecret == "appsecret")
            {
                throw new InvalidOperationException(
                    "You haven't changed the default remote credentials! Register on hastlayer.com to receive access if you don't have it yet.");
            }

            // If the sample was selected in the command line use that, or otherwise the default.
            Configuration.SampleToRun = (Sample)Enum.Parse(typeof(Sample), GetArgument("sample") ?? Configuration.SampleToRun.ToString(), true);

            // Letting the configuration of samples run. Check out those methods too!
            switch (Configuration.SampleToRun)
            {
                case Sample.Fix64Calculator:
                    Fix64CalculatorSampleRunner.Configure(configuration);
                    break;
                case Sample.FSharpParallelAlgorithm:
                    FSharpParallelAlgorithmSampleRunner.Configure(configuration);
                    break;
                case Sample.GenomeMatcher:
                    GenomeMatcherSampleRunner.Configure(configuration);
                    break;
                case Sample.ParallelAlgorithm:
                    ParallelAlgorithmSampleRunner.Configure(configuration);
                    break;
                case Sample.ImageProcessingAlgorithms:
                    ImageProcessingAlgorithmsSampleRunner.Configure(configuration);
                    break;
                case Sample.Loopback:
                    LoopbackSampleRunner.Configure(configuration);
                    break;
                case Sample.MemoryTest:
                    MemoryTestSampleRunner.Configure(configuration);
                    break;
                case Sample.MonteCarloPiEstimator:
                    MonteCarloPiEstimatorSampleRunner.Configure(configuration);
                    break;
                case Sample.ObjectOrientedShowcase:
                    ObjectOrientedShowcaseSampleRunner.Configure(configuration);
                    break;
                case Sample.PositCalculator:
                    PositCalculatorSampleRunner.Configure(configuration);
                    break;
                case Sample.Posit32AdvancedCalculator:
                    Posit32AdvancedCalculatorSampleRunner.Configure(configuration);
                    break;
                case Sample.Posit32Calculator:
                    Posit32CalculatorSampleRunner.Configure(configuration);
                    break;
                case Sample.Posit32FusedCalculator:
                    Posit32FusedCalculatorSampleRunner.Configure(configuration);
                    break;
                case Sample.PrimeCalculator:
                    PrimeCalculatorSampleRunner.Configure(configuration);
                    break;
                case Sample.RecursiveAlgorithms:
                    RecursiveAlgorithmsSampleRunner.Configure(configuration);
                    break;
                case Sample.SimdCalculator:
                    SimdCalculatorSampleRunner.Configure(configuration);
                    break;
                case Sample.UnumCalculator:
                    UnumCalculatorSampleRunner.Configure(configuration);
                    break;
                default:
                    throw new Exception($"Unknown sample '{Configuration.SampleToRun}'.");
            }

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
                        typeof(FSharpSampleAssembly.FSharpParallelAlgorithmContainer).Assembly,
                        // Note that the assemblies used by code to be transformed also need to be added
                        // separately. E.g. Posit is used by Hast.Samples.SampleAssembly which in turn also uses
                        // ImmutableArray.
                        typeof(Posit).Assembly,
                        typeof(ImmutableArray).Assembly
                },
                configuration);

            Console.WriteLine("Hardware generation finished.");
            Console.WriteLine();

            // Be sure to check out transformation warnings. Most of the time the issues noticed shouldn't cause
            // any problems, but sometimes they can.
            if (hardwareRepresentation.HardwareDescription.Warnings.Any())
            {
                Console.WriteLine("There were transformation warnings in the logs, which may hint on issues that can " +
                                  "cause the hardware implementation to produce incorrect results.\n");
            }

            Console.WriteLine("Starting hardware execution.");

            // Running samples.
            try
            {
                switch (Configuration.SampleToRun)
                {
                    case Sample.Fix64Calculator:
                        await Fix64CalculatorSampleRunner.Run(hastlayer, hardwareRepresentation, proxyConfiguration);
                        break;
                    case Sample.FSharpParallelAlgorithm:
                        await FSharpParallelAlgorithmSampleRunner.Run(hastlayer, hardwareRepresentation, proxyConfiguration);
                        break;
                    case Sample.GenomeMatcher:
                        await GenomeMatcherSampleRunner.Run(hastlayer, hardwareRepresentation, proxyConfiguration);
                        break;
                    case Sample.ParallelAlgorithm:
                        await ParallelAlgorithmSampleRunner.Run(hastlayer, hardwareRepresentation, proxyConfiguration);
                        break;
                    case Sample.ImageProcessingAlgorithms:
                        await ImageProcessingAlgorithmsSampleRunner.Run(hastlayer, hardwareRepresentation, proxyConfiguration);
                        break;
                    case Sample.Loopback:
                        await LoopbackSampleRunner.Run(hastlayer, hardwareRepresentation, proxyConfiguration);
                        break;
                    case Sample.MemoryTest:
                        await MemoryTestSampleRunner.Run(hastlayer, hardwareRepresentation, proxyConfiguration);
                        break;
                    case Sample.MonteCarloPiEstimator:
                        await MonteCarloPiEstimatorSampleRunner.Run(hastlayer, hardwareRepresentation, proxyConfiguration);
                        break;
                    case Sample.ObjectOrientedShowcase:
                        await ObjectOrientedShowcaseSampleRunner.Run(hastlayer, hardwareRepresentation, proxyConfiguration);
                        break;
                    case Sample.PositCalculator:
                        await PositCalculatorSampleRunner.Run(hastlayer, hardwareRepresentation, proxyConfiguration);
                        break;
                    case Sample.Posit32AdvancedCalculator:
                        await Posit32AdvancedCalculatorSampleRunner.Run(hastlayer, hardwareRepresentation, proxyConfiguration);
                        break;
                    case Sample.Posit32Calculator:
                        await Posit32CalculatorSampleRunner.Run(hastlayer, hardwareRepresentation, proxyConfiguration);
                        break;
                    case Sample.Posit32FusedCalculator:
                        await Posit32FusedCalculatorSampleRunner.Run(hastlayer, hardwareRepresentation, proxyConfiguration);
                        break;
                    case Sample.PrimeCalculator:
                        await PrimeCalculatorSampleRunner.Run(hastlayer, hardwareRepresentation, proxyConfiguration);
                        break;
                    case Sample.RecursiveAlgorithms:
                        await RecursiveAlgorithmsSampleRunner.Run(hastlayer, hardwareRepresentation, proxyConfiguration);
                        break;
                    case Sample.SimdCalculator:
                        await SimdCalculatorSampleRunner.Run(hastlayer, hardwareRepresentation, proxyConfiguration);
                        break;
                    case Sample.UnumCalculator:
                        await UnumCalculatorSampleRunner.Run(hastlayer, hardwareRepresentation, proxyConfiguration);
                        break;
                    default:
                        throw new Exception($"Unknown sample '{Configuration.SampleToRun}'.");
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
                Console.WriteLine($"There {(mismatchCount == 1 ? "was a mismatch" : $"were {mismatchCount} mismatches")} between the software and hardware execution's results! Mismatch{(mismatchCount == 1 ? string.Empty : "es")}:");

                foreach (var mismatch in mismatches)
                {
                    Console.WriteLine("* " + mismatch);
                }
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
