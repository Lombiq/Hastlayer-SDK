using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Transformer.Abstractions.SimpleMemory;
using Lombiq.Arithmetics;

namespace Hast.Samples.Posit
{

    public class Posit16_0_Calculator
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

            var a = new Posit16_0((ushort)1);
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
            var positToMultiply = (ushort)memory.ReadUInt32(CalculatePowerOfReal_InputPosit32Index);

            var a = new Posit16_0(positToMultiply, true);
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
                        var a = new Posit16_0(1);
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

            var result = new Posit16_0((ushort)memory.ReadUInt32(AddPositsInArray_InputPosit32sStartIndex), true);

            for (int i = 1; i < numberCount; i++)
            {
                result += new Posit16_0((ushort)memory.ReadUInt32(AddPositsInArray_InputPosit32sStartIndex + i), true);
            }

            memory.WriteUInt32(AddPositsInArray_OutputPosit32Index, result.PositBits);
        }
    }


    public static class Posit16_0_CalculatorCalculatorExtensions
    {
        public static int CalculateIntegerSumUpToNumber(this  Posit16_0_Calculator  positCalculator, int number)
        {
            var memory = new SimpleMemory(1);

            memory.WriteInt32( Posit16_0_Calculator.CalculateLargeIntegerSum_InputInt32Index, number);
            positCalculator.CalculateIntegerSumUpToNumber(memory);

            return memory.ReadInt32( Posit16_0_Calculator.CalculateLargeIntegerSum_OutputInt32Index);
        }

        public static float CalculatePowerOfReal(this  Posit16_0_Calculator  positCalculator, int number, float real)
        {
            var memory = new SimpleMemory(2);

            memory.WriteInt32( Posit16_0_Calculator.CalculatePowerOfReal_InputInt32Index, number);
            memory.WriteUInt32( Posit16_0_Calculator.CalculatePowerOfReal_InputPosit32Index, new  Posit16_0(real).PositBits);

            positCalculator.CalculatePowerOfReal(memory);

            return (float)new Posit16_0((ushort)memory.ReadUInt32( Posit16_0_Calculator.CalculatePowerOfReal_OutputPosit32Index), true);
        }

        public static IEnumerable<int> ParallelizedCalculateIntegerSumUpToNumbers(this  Posit16_0_Calculator positCalculator, int[] numbers)
        {
            if (numbers.Length !=  Posit16_0_Calculator.MaxDegreeOfParallelism)
            {
                throw new ArgumentException(
                    "Provide as many numbers as the degree of parallelism of  Posit16_0_Calculator is (" +
                     Posit16_0_Calculator.MaxDegreeOfParallelism + ")");
            }

            var memory = new SimpleMemory( Posit16_0_Calculator.MaxDegreeOfParallelism);

            for (int i = 0; i < numbers.Length; i++)
            {
                memory.WriteInt32( Posit16_0_Calculator.ParallelizedCalculateLargeIntegerSum_Int32NumbersStartIndex + i, numbers[i]);
            }

            positCalculator.ParallelizedCalculateIntegerSumUpToNumbers(memory);

            var results = new int[ Posit16_0_Calculator.MaxDegreeOfParallelism];

            for (int i = 0; i < numbers.Length; i++)
            {
                results[i] = memory.ReadInt32( Posit16_0_Calculator.ParallelizedCalculateLargeIntegerSum_OutputInt32sStartIndex + i);
            }

            return results;
        }

        public static float AddPositsInArray(this  Posit16_0_Calculator positCalculator, uint[] positArray)
        {
            var memory = new SimpleMemory( positArray.Length + 1);

            memory.WriteUInt32( Posit16_0_Calculator.AddPositsInArray_InputPosit32CountIndex, (uint) positArray.Length);

            for (var i = 0; i <  positArray.Length; i++)
            {
                memory.WriteUInt32( Posit16_0_Calculator.AddPositsInArray_InputPosit32sStartIndex + i, positArray[i]);
            }

            positCalculator.AddPositsInArray(memory);

            return (float)new Posit32(memory.ReadUInt32( Posit16_0_Calculator.AddPositsInArray_OutputPosit32Index), true);
        }
    }
}

