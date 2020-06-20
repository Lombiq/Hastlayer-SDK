using System.Threading.Tasks;
using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Hast.Transformer.Abstractions.Configuration;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal class PrimeCalculatorSampleRunner : ISampleRunner
    {
        public void Configure(HardwareGenerationConfiguration configuration)
        {
            // You can add complete types whose methods you'd like to invoke on the hardware from the outside like this.
            configuration.AddHardwareEntryPointType<PrimeCalculator>();
            // A not statically typed way of doing the same as above would be:
            //configuration.PublicHardwareMemberNamePrefixes.Add("Hast.Samples.SampleAssembly.PrimeCalculator");
            // Note that the bottom version can also be used to add multiple types from under a namespace.
        }

        public async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            var primeCalculator = await hastlayer.GenerateProxy(hardwareRepresentation, new PrimeCalculator(), configuration);

            var memoryConfig = (hastlayer as Hastlayer).CreateMemoryConfiguration(hardwareRepresentation);
            var isPrime = primeCalculator.IsPrimeNumber(15, memoryConfig);
            var isPrime2 = primeCalculator.IsPrimeNumber(13, memoryConfig);
            var isPrime3 = await primeCalculator.IsPrimeNumberAsync(21, memoryConfig);
            // Only 2341 is prime.
            var arePrimes = primeCalculator.ArePrimeNumbers(new uint[] { 15, 493, 2341, 99237 }, memoryConfig);
            var arePrimes2 = primeCalculator.ArePrimeNumbers(new uint[] { 13, 493 }, memoryConfig);

            // You can also launch hardware-executed method calls in parallel. If there are multiple boards
            // connected then all of them will be utilized. If the whole device pool is utilized calls will
            // wait for their turn.
            // Uncomment if you have multiple boards connected.
            //var parallelLaunchedIsPrimeTasks = new List<Task<bool>>();
            //for (uint i = 100; i < 110; i++)
            //{
            //    parallelLaunchedIsPrimeTasks
            //        .Add(Task.Factory.StartNew(indexObject => primeCalculator.IsPrimeNumber((uint)indexObject), i));
            //}
            //var parallelLaunchedArePrimes = await Task.WhenAll(parallelLaunchedIsPrimeTasks);


            // In-algorithm parallelization:
            // Note that if the amount of numbers used here can't be divided by PrimeCalculator.MaxDegreeOfParallelism
            // then for ParallelizedArePrimeNumbers the input and output will be padded to a divisible amount (see
            // comments in the method). Thus the communication round-trip will be slower for ParallelizedArePrimeNumbers.
            // Because of this since PrimeCalculator.MaxDegreeOfParallelism is 30 we use 30 numbers here.
            // All of these numbers except for 9999 are primes.
            var numbers = new uint[]
            {
                9749, 9999, 902119, 907469, 915851,
                9749, 9973, 902119, 907469, 915851,
                9749, 9999, 902119, 907469, 915851,
                9749, 9973, 902119, 907469, 915851,
                9749, 9999, 902119, 907469, 915851,
                9749, 9973, 902119, 907469, 915851
            };

            var arePrimes3 = primeCalculator.ArePrimeNumbers(numbers);
            var arePrimes4 = primeCalculator.ParallelizedArePrimeNumbers(numbers);
        }
    }
}
