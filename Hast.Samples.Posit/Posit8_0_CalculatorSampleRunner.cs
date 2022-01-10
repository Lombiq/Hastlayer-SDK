﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Lombiq.Arithmetics;

namespace Hast.Samples.Posit
{
    internal class Posit8_0_CalculatorSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<Posit8_0_Calculator>();
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
        {
            RunSoftwareBenchmarks(hastlayer, hardwareRepresentation);

            var positCalculator = await hastlayer.GenerateProxyAsync(hardwareRepresentation, new Posit8_0_Calculator());


            var integerSumUpToNumber = positCalculator.CalculateIntegerSumUpToNumber(100000, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);

                        positCalculator.CalculatePowerOfReal( 5, (float)0.5);
         
            
            var numbers = new int[Posit8_0_Calculator.MaxDegreeOfParallelism];
            for (int i = 0; i < Posit8_0_Calculator.MaxDegreeOfParallelism; i++)
            {
                numbers[i] = 100000 + (i % 2 == 0 ? -1 : 1);
            }

            var integerSumsUpToNumbers = positCalculator.ParallelizedCalculateIntegerSumUpToNumbers(numbers,  hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);


            var Posit8_0Array = new uint[100000];

            for (var i = 0; i < 100000; i++)
            {
                if (i % 2 == 0) Posit8_0Array[i] = new Posit8_0((float)0.25 * 2 * i).PositBits;
                else Posit8_0Array[i] = new Posit8_0((float)0.25 * -2 * i).PositBits;
            }

            var positsInArraySum = positCalculator.AddPositsInArray(Posit8_0Array);
        }

        public static void RunSoftwareBenchmarks(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
        {
            var positCalculator = new Posit8_0_Calculator();


            // Not to run the benchmark below the first time, because JIT compiling can affect it.
            positCalculator.CalculateIntegerSumUpToNumber(100000);
            var sw = Stopwatch.StartNew();
            var integerSumUpToNumber = positCalculator.CalculateIntegerSumUpToNumber(
                100000,
                hastlayer,
                hardwareRepresentation.HardwareGenerationConfiguration);
            sw.Stop();

            Console.WriteLine("Result of counting up to 100000: " + integerSumUpToNumber);
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds + "ms");

            Console.WriteLine();

            positCalculator.CalculatePowerOfReal(100000, (float)1.0001, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            sw = Stopwatch.StartNew();
            
            
            var powerOfReal = positCalculator.CalculatePowerOfReal( 5, (float)0.5, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration );
            
            sw.Stop();

            Console.WriteLine("Result of power of real number: " + powerOfReal);
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds + "ms");

            Console.WriteLine();

            var numbers = new int[Posit8_0_Calculator.MaxDegreeOfParallelism];
            for (int i = 0; i < Posit8_0_Calculator.MaxDegreeOfParallelism; i++)
            {
                numbers[i] = 100000 + (i % 2 == 0 ? -1 : 1);
            }

            positCalculator.ParallelizedCalculateIntegerSumUpToNumbers(numbers,hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            sw = Stopwatch.StartNew();
            var integerSumsUpToNumbers = positCalculator.ParallelizedCalculateIntegerSumUpToNumbers(
                numbers,
                hastlayer,
                hardwareRepresentation.HardwareGenerationConfiguration);
            sw.Stop();

            Console.WriteLine("Result of counting up to ~100000 parallelized: " + string.Join(", ", integerSumsUpToNumbers));
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds + "ms");

            Console.WriteLine();

            var Posit8_0Array = new uint[100000];

            for (var i = 0; i < 100000; i++)
            {
                if (i % 2 == 0)  Posit8_0Array[i] = new  Posit8_0((float)0.25 * 2 * i).PositBits;
                else  Posit8_0Array[i] = new  Posit8_0((float)0.25 * -2 * i).PositBits;
            }

            positCalculator.AddPositsInArray( Posit8_0Array,hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            sw = Stopwatch.StartNew();
            var positsInArraySum = positCalculator.AddPositsInArray( Posit8_0Array, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            sw.Stop();

            Console.WriteLine("Result of addition of posits in array: " + positsInArraySum);
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds + "ms");
            Console.WriteLine();
        }}
}

