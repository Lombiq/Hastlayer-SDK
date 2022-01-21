using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using Lombiq.Arithmetics;

namespace Hast.Samples.Posit
{

    public class Posit8E4Calculator
    {
        public const int CalculateLargeIntegerSum_InputInt32Index = 0;
        public const int CalculateLargeIntegerSum_OutputInt32Index = 0;
        public const int ParallelizedCalculateLargeIntegerSum_Int32NumbersStartIndex = 0;
        public const int ParallelizedCalculateLargeIntegerSum_OutputInt32sStartIndex = 0;
        public const int AddPositsInArray_InputPosit32CountIndex = 0;
        public const int AddPositsInArray_InputPosit32sStartIndex = 1;
        public const int AddPositsInArray_OutputPosit32Index = 2;
        public const int CalculatePowerOfReal_InputInt32Index = 0;
        public const int CalculatePowerOfReal_InputPosit32Index = 1;
        public const int CalculatePowerOfReal_OutputPosit32Index = 0;
       
        public const int MaxDegreeOfParallelism = 5;


        public virtual void CalculateIntegerSumUpToNumber(SimpleMemory memory)
        {
            var number = memory.ReadUInt32(CalculateLargeIntegerSum_InputInt32Index);

            var a = new Posit8E4((byte)1);
            var b = a;

            for (uint i = 1; i < number; i++)
            {
                a += b;
            }

            var result = (int)a;
            memory.WriteInt32(CalculateLargeIntegerSum_OutputInt32Index, result);
        }

        public virtual void CalculatePowerOfReal(SimpleMemory memory)
        {
            var number = memory.ReadInt32(CalculatePowerOfReal_InputInt32Index);
            var positToMultiply = (byte)memory.ReadUInt32(CalculatePowerOfReal_InputPosit32Index);

            var a = new Posit8E4(positToMultiply, true);
            var b = a;

            for (uint i = 0; i < number; i++)
            {
                a *= b;
            }

            var result = a.PositBits;
            memory.WriteUInt32(CalculatePowerOfReal_OutputPosit32Index, result);
        }

        public virtual void ParallelizedCalculateIntegerSumUpToNumbers(SimpleMemory memory)
        {
            var numbers = new int[MaxDegreeOfParallelism];

            var tasks = new Task<int>[MaxDegreeOfParallelism];

            for (int i = 0; i < MaxDegreeOfParallelism; i++)
            {
                var upToNumber = memory.ReadInt32(ParallelizedCalculateLargeIntegerSum_Int32NumbersStartIndex + i);

                tasks[i] = Task.Factory.StartNew(
                    upToNumberObject =>
                    {
                        var a = new Posit8E4(1);
                        var b = a;

                        for (int j = 1; j < (int)upToNumberObject; j++)
                        {
                            a += b;
                        }

                        return (int)a;
                    }, upToNumber);
            }

            Task.WhenAll(tasks).Wait();

            for (int i = 0; i < MaxDegreeOfParallelism; i++)
            {
                memory.WriteInt32(ParallelizedCalculateLargeIntegerSum_OutputInt32sStartIndex + i, tasks[i].Result);
            }
        }

        public virtual void AddPositsInArray(SimpleMemory memory)
        {
            uint numberCount = memory.ReadUInt32(AddPositsInArray_InputPosit32CountIndex);

            var result = new Posit8E4((byte)memory.ReadUInt32(AddPositsInArray_InputPosit32sStartIndex), true);

            for (int i = 1; i < numberCount; i++)
            {
                result += new Posit8E4((byte)memory.ReadUInt32(AddPositsInArray_InputPosit32sStartIndex + i), true);
            }

            memory.WriteUInt32(AddPositsInArray_OutputPosit32Index, result.PositBits);
        }
    }


    public static class Posit8E4CalculatorCalculatorExtensions
    {
        public static int CalculateIntegerSumUpToNumber(
            this  Posit8E4Calculator  positCalculator,
            int number,
            IHastlayer hastlayer = null,
            IHardwareGenerationConfiguration configuration = null)
        {
             var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(1)
                : hastlayer.CreateMemory(configuration, 1);

            memory.WriteInt32( Posit8E4Calculator.CalculateLargeIntegerSum_InputInt32Index, number);
            positCalculator.CalculateIntegerSumUpToNumber(memory);

            return memory.ReadInt32( Posit8E4Calculator.CalculateLargeIntegerSum_OutputInt32Index);
        }

        public static float CalculatePowerOfReal(
            this  Posit8E4Calculator  positCalculator,
            int number,
            float real,
            IHastlayer hastlayer = null,
            IHardwareGenerationConfiguration configuration = null)
        {
            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(2)
                : hastlayer.CreateMemory(configuration, 2);

            memory.WriteInt32( Posit8E4Calculator.CalculatePowerOfReal_InputInt32Index, number);
            memory.WriteUInt32( Posit8E4Calculator.CalculatePowerOfReal_InputPosit32Index, new  Posit8E4(real).PositBits);

            positCalculator.CalculatePowerOfReal(memory);

            return (float)new Posit8E4((byte)memory.ReadUInt32( Posit8E4Calculator.CalculatePowerOfReal_OutputPosit32Index), true);
        }

        public static IEnumerable<int> ParallelizedCalculateIntegerSumUpToNumbers(
            this  Posit8E4Calculator positCalculator,
            int[] numbers,
            IHastlayer hastlayer = null,
            IHardwareGenerationConfiguration configuration = null)
        {
            if (numbers.Length !=  Posit8E4Calculator.MaxDegreeOfParallelism)
            {
                throw new ArgumentException(
                    "Provide as many numbers as the degree of parallelism of  Posit8E4Calculator is (" +
                     Posit8E4Calculator.MaxDegreeOfParallelism + ")");
            }

            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(Posit8E4Calculator.MaxDegreeOfParallelism)
                : hastlayer.CreateMemory(configuration, Posit8E4Calculator.MaxDegreeOfParallelism);

            for (int i = 0; i < numbers.Length; i++)
            {
                memory.WriteInt32( Posit8E4Calculator.ParallelizedCalculateLargeIntegerSum_Int32NumbersStartIndex + i, numbers[i]);
            }

            positCalculator.ParallelizedCalculateIntegerSumUpToNumbers(memory);

            var results = new int[ Posit8E4Calculator.MaxDegreeOfParallelism];

            for (int i = 0; i < numbers.Length; i++)
            {
                results[i] = memory.ReadInt32( Posit8E4Calculator.ParallelizedCalculateLargeIntegerSum_OutputInt32sStartIndex + i);
            }

            return results;
        }

        public static float AddPositsInArray(
            this  Posit8E4Calculator positCalculator,
            uint[] positArray,
            IHastlayer hastlayer = null,
            IHardwareGenerationConfiguration configuration = null)
        {
            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(positArray.Length + 1)
                : hastlayer.CreateMemory(configuration, positArray.Length + 1);

            memory.WriteUInt32( Posit8E4Calculator.AddPositsInArray_InputPosit32CountIndex, (uint) positArray.Length);

            for (var i = 0; i <  positArray.Length; i++)
            {
                memory.WriteUInt32( Posit8E4Calculator.AddPositsInArray_InputPosit32sStartIndex + i, positArray[i]);
            }

            positCalculator.AddPositsInArray(memory);

            return (float)new Posit32(memory.ReadUInt32( Posit8E4Calculator.AddPositsInArray_OutputPosit32Index), true);
        }
    }
}

