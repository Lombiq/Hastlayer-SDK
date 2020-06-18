using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Lombiq.Arithmetics;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal class Posit32CalculatorSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<Posit32Calculator>();
            // Use the below config if you just want to transform only ParallelizedCalculateIntegerSumUpToNumbers to
            // hardware.
            //configuration.AddHardwareEntryPointMethod<Posit32Calculator>(p => p.ParallelizedCalculateIntegerSumUpToNumbers((SimpleMemory)null));
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            RunSoftwareBenchmarks();

            var positCalculator = await hastlayer.GenerateProxy(hardwareRepresentation, new Posit32Calculator(), configuration);

            var memoryConfig = (hastlayer as Hastlayer).CreateMemoryConfiguration(hardwareRepresentation);
            var integerSumUpToNumber = positCalculator.CalculateIntegerSumUpToNumber(100000, memoryConfig);


            positCalculator.CalculatePowerOfReal(100000, (float)1.0001, memoryConfig);


            var numbers = new int[Posit32Calculator.MaxDegreeOfParallelism];
            for (int i = 0; i < Posit32Calculator.MaxDegreeOfParallelism; i++)
            {
                numbers[i] = 100000 + (i % 2 == 0 ? -1 : 1);
            }

            var integerSumsUpToNumbers = positCalculator.ParallelizedCalculateIntegerSumUpToNumbers(numbers, memoryConfig);


            var posit32Array = new uint[100000];

            for (var i = 0; i < 100000; i++)
            {
                if (i % 2 == 0) posit32Array[i] = new Posit32((float)0.25 * 2 * i).PositBits;
                else posit32Array[i] = new Posit32((float)0.25 * -2 * i).PositBits;
            }

            var positsInArraySum = positCalculator.AddPositsInArray(posit32Array, memoryConfig);
        }

        public static void RunSoftwareBenchmarks()
        {
            var positCalculator = new Posit32Calculator();


            // Not to run the benchmark below the first time, because JIT compiling can affect it.
            positCalculator.CalculateIntegerSumUpToNumber(100000);
            var sw = Stopwatch.StartNew();
            var integerSumUpToNumber = positCalculator.CalculateIntegerSumUpToNumber(100000);
            sw.Stop();

            Console.WriteLine("Result of counting up to 100000: " + integerSumUpToNumber);
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds + "ms");

            Console.WriteLine();

            positCalculator.CalculatePowerOfReal(100000, (float)1.0001);
            sw = Stopwatch.StartNew();
            var powerOfReal = positCalculator.CalculatePowerOfReal(100000, (float)1.0001);
            sw.Stop();

            Console.WriteLine("Result of power of real number: " + powerOfReal);
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds + "ms");

            Console.WriteLine();

            var numbers = new int[Posit32Calculator.MaxDegreeOfParallelism];
            for (int i = 0; i < Posit32Calculator.MaxDegreeOfParallelism; i++)
            {
                numbers[i] = 100000 + (i % 2 == 0 ? -1 : 1);
            }

            positCalculator.ParallelizedCalculateIntegerSumUpToNumbers(numbers);
            sw = Stopwatch.StartNew();
            var integerSumsUpToNumbers = positCalculator.ParallelizedCalculateIntegerSumUpToNumbers(numbers);
            sw.Stop();

            Console.WriteLine("Result of counting up to ~100000 parallelized: " + string.Join(", ", integerSumsUpToNumbers));
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds + "ms");

            Console.WriteLine();

            var posit32Array = new uint[100000];

            for (var i = 0; i < 100000; i++)
            {
                if (i % 2 == 0) posit32Array[i] = new Posit32((float)0.25 * 2 * i).PositBits;
                else posit32Array[i] = new Posit32((float)0.25 * -2 * i).PositBits;
            }

            positCalculator.AddPositsInArray(posit32Array);
            sw = Stopwatch.StartNew();
            var positsInArraySum = positCalculator.AddPositsInArray(posit32Array);
            sw.Stop();

            Console.WriteLine("Result of addition of posits in array: " + positsInArraySum);
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds + "ms");

            Console.WriteLine();
        }
    }
}
