using Hast.Layer;
using Lombiq.Arithmetics;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Hast.Samples.Posit
{
    internal class Posit32_2_CalculatorSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<Posit32_2_Calculator>();
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
        {
            RunSoftwareBenchmarks();

            var positCalculator = await hastlayer.GenerateProxy(hardwareRepresentation, new Posit32_2_Calculator());


            var integerSumUpToNumber = positCalculator.CalculateIntegerSumUpToNumber(100000);


            positCalculator.CalculatePowerOfReal(1000, (float)1.015625);


            var numbers = new int[Posit32_2_Calculator.MaxDegreeOfParallelism];
            for (int i = 0; i < Posit32_2_Calculator.MaxDegreeOfParallelism; i++)
            {
                numbers[i] = 100000 + (i % 2 == 0 ? -1 : 1);
            }

            var integerSumsUpToNumbers = positCalculator.ParallelizedCalculateIntegerSumUpToNumbers(numbers);


            var Posit32_2Array = new uint[100000];

            for (var i = 0; i < 100000; i++)
            {
                if (i % 2 == 0) Posit32_2Array[i] = new Posit32_2((float)0.25 * 2 * i).PositBits;
                else Posit32_2Array[i] = new Posit32_2((float)0.25 * -2 * i).PositBits;
            }

            var positsInArraySum = positCalculator.AddPositsInArray(Posit32_2Array);
        }

        public static void RunSoftwareBenchmarks()
        {
            var positCalculator = new Posit32_2_Calculator();


            // Not to run the benchmark below the first time, because JIT compiling can affect it.
            positCalculator.CalculateIntegerSumUpToNumber(100000);
            var sw = Stopwatch.StartNew();
            var integerSumUpToNumber = positCalculator.CalculateIntegerSumUpToNumber(100000);
            sw.Stop();

            Console.WriteLine("Result of counting up to 100000: " + integerSumUpToNumber);
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds + "ms");

            Console.WriteLine();

            sw = Stopwatch.StartNew();

            var powerOfReal = positCalculator.CalculatePowerOfReal(1000, (float)1.015625);

            sw.Stop();

            Console.WriteLine("Result of power of real number: " + powerOfReal);
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds + "ms");

            Console.WriteLine();

            var numbers = new int[Posit32_2_Calculator.MaxDegreeOfParallelism];
            for (int i = 0; i < Posit32_2_Calculator.MaxDegreeOfParallelism; i++)
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

            var Posit32_2Array = new uint[100000];

            for (var i = 0; i < 100000; i++)
            {
                if (i % 2 == 0) Posit32_2Array[i] = new Posit32_2((float)0.25 * 2 * i).PositBits;
                else Posit32_2Array[i] = new Posit32_2((float)0.25 * -2 * i).PositBits;
            }

            positCalculator.AddPositsInArray(Posit32_2Array);
            sw = Stopwatch.StartNew();
            var positsInArraySum = positCalculator.AddPositsInArray(Posit32_2Array);
            sw.Stop();

            Console.WriteLine("Result of addition of posits in array: " + positsInArraySum);
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds + "ms");

            Console.WriteLine();
        }
    }
}

