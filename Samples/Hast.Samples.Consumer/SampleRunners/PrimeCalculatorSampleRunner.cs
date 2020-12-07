using Hast.Layer;
using Hast.Samples.SampleAssembly;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal class PrimeCalculatorSampleRunner : ISampleRunner
    {
        public void Configure(HardwareGenerationConfiguration configuration) =>
            // You can add complete types whose methods you'd like to invoke on the hardware from the outside like this.
            configuration.AddHardwareEntryPointType<PrimeCalculator>();
        // A not statically typed way of doing the same as above would be:
        //// configuration.PublicHardwareMemberNamePrefixes.Add("Hast.Samples.SampleAssembly.PrimeCalculator");
        // Note that the bottom version can also be used to add multiple types from under a namespace.

        public async Task RunAsync(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            var primeCalculator = await hastlayer.GenerateProxyAsync(hardwareRepresentation, new PrimeCalculator(), configuration);

            foreach (var number in new uint[] { 15, 13, 20 })
            {
                Console.WriteLine("primeCalculator.IsPrimeNumber: {0}", number);
                var result = primeCalculator.IsPrimeNumberSync(
                    number,
                    hastlayer,
                    hardwareRepresentation.HardwareGenerationConfiguration);
                Console.WriteLine("primeCalculator.IsPrimeNumber: {0} => {1}", number, result);
            }

            // Only 2341 is prime.
            foreach (var numbersToCheck in new[] { new uint[] { 15, 493, 2_341, 99_237 }, new uint[] { 13, 493 } })
            {
                WriteOutPrimes(
                    nameof(primeCalculator.ArePrimeNumbers),
                    primeCalculator.ArePrimeNumbers,
                    numbersToCheck,
                    hastlayer,
                    hardwareRepresentation.HardwareGenerationConfiguration);
            }

            // You can also launch hardware-executed method calls in parallel. If there are multiple boards
            // connected then all of them will be utilized. If the whole device pool is utilized calls will
            // wait for their turn.
            // Uncomment if you have multiple boards connected.
            //// var parallelLaunchedIsPrimeTasks = new List<Task<bool>>();
            //// for (uint i = 100; i < 110; i++)
            //// {
            ////    parallelLaunchedIsPrimeTasks
            ////        .Add(Task.Factory.StartNew(indexObject => primeCalculator.IsPrimeNumber((uint)indexObject), i));
            //// }
            //// var parallelLaunchedArePrimes = await Task.WhenAll(parallelLaunchedIsPrimeTasks);

            // In-algorithm parallelization:
            // Note that if the amount of numbers used here can't be divided by PrimeCalculator.MaxDegreeOfParallelism
            // then for ParallelizedArePrimeNumbers the input and output will be padded to a divisible amount (see
            // comments in the method). Thus the communication round-trip will be slower for ParallelizedArePrimeNumbers.
            // Because of this since PrimeCalculator.MaxDegreeOfParallelism is 30 we use 30 numbers here.
            // All of these numbers except for 9999 are primes.
            var numbers = new uint[]
            {
                9_749, 9_999, 902_119, 907_469, 915_851,
                9_749, 9_973, 902_119, 907_469, 915_851,
                9_749, 9_999, 902_119, 907_469, 915_851,
                9_749, 9_973, 902_119, 907_469, 915_851,
                9_749, 9_999, 902_119, 907_469, 915_851,
                9_749, 9_973, 902_119, 907_469, 915_851,
            };

            WriteOutPrimes(
                nameof(primeCalculator.ArePrimeNumbers),
                primeCalculator.ArePrimeNumbers,
                numbers,
                hastlayer,
                hardwareRepresentation.HardwareGenerationConfiguration);
            WriteOutPrimes(
                nameof(primeCalculator.ParallelizedArePrimeNumbers),
                primeCalculator.ParallelizedArePrimeNumbers,
                numbers,
                hastlayer,
                hardwareRepresentation.HardwareGenerationConfiguration);
        }

        private static void WriteOutPrimes(
            string methodName,
            Func<uint[], IHastlayer, IHardwareGenerationConfiguration, bool[]> method,
            uint[] numbers,
            IHastlayer hastlayer,
            IHardwareGenerationConfiguration hardwareGenerationConfiguration)
        {
            var concatenated = string.Join(", ", numbers);
            Console.WriteLine("primeCalculator.{0}: {1}", methodName, concatenated);
            var result = method(
                numbers,
                hastlayer,
                hardwareGenerationConfiguration);
            var resultString = string.Join(", ", numbers.Where((number, index) => result[index]));
            Console.WriteLine(
                "primeCalculator.{0}: {1} => {2}",
                methodName,
                concatenated,
                string.IsNullOrEmpty(resultString) ? "NONE" : resultString);
        }
    }
}
