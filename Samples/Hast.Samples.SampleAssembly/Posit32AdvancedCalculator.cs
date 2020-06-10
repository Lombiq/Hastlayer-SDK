using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Synthesis.Abstractions;
using Hast.Transformer.Abstractions.SimpleMemory;
using Lombiq.Arithmetics;

namespace Hast.Samples.SampleAssembly
{
    public class Posit32AdvancedCalculator
    {

        public const int RepeatedDivision_InputInt32Index = 0;
        public const int RepeatedDivision_FirstInputPosit32Index = 1;
        public const int RepeatedDivision_SecondInputPosit32Index = 2;
        public const int RepeatedDivision_OutputPosit32Index = 0;

        public const int SqrtOfPositsInArray_InputPosit32CountIndex = 0;
        public const int SqrtOfPositsInArray_InputPosit32sStartIndex = 1;
        public const int SqrtOfPositsInArray_OutputPosit32StartIndex = 0;

        public virtual void RepeatedDivision(SimpleMemory memory)
        {
            var number = memory.ReadInt32(RepeatedDivision_InputInt32Index);
            var dividendPosit = memory.ReadUInt32(RepeatedDivision_FirstInputPosit32Index);
            var divisorPosit = memory.ReadUInt32(RepeatedDivision_SecondInputPosit32Index);

            var a = new Posit32(dividendPosit, true);
            var b = new Posit32(divisorPosit, true);

            for (uint i = 0; i < number; i++)
            {
                a /= b;
            }

            var result = a.PositBits;
            memory.WriteUInt32(RepeatedDivision_OutputPosit32Index, result);
        }

        public virtual void SqrtOfPositsInArray(SimpleMemory memory)
        {
            uint numberCount = memory.ReadUInt32(SqrtOfPositsInArray_InputPosit32CountIndex);

            var result = new Posit32(memory.ReadUInt32(SqrtOfPositsInArray_InputPosit32sStartIndex), true);

            for (int i = 0; i < numberCount; i++)
            {
                result = Posit32.Sqrt(new Posit32(memory.ReadUInt32(SqrtOfPositsInArray_InputPosit32sStartIndex + i), true));
                memory.WriteUInt32(SqrtOfPositsInArray_OutputPosit32StartIndex + i, result.PositBits);
            }
        }
    }

    public static class Posit32AdvancedCalculatorExtensions
    {
        public static float RepeatedDivision(this Posit32AdvancedCalculator positCalculator, int number, float dividend, float divisor, IMemoryConfiguration memoryConfiguration)
        {
            var memory = memoryConfiguration is null ?
                SimpleMemory.CreateSoftwareMemory(3) :
                SimpleMemory.Create(memoryConfiguration, 3);

            memory.WriteInt32(Posit32AdvancedCalculator.RepeatedDivision_InputInt32Index, number);
            memory.WriteUInt32(Posit32AdvancedCalculator.RepeatedDivision_FirstInputPosit32Index, new Posit32(dividend).PositBits);
            memory.WriteUInt32(Posit32AdvancedCalculator.RepeatedDivision_SecondInputPosit32Index, new Posit32(divisor).PositBits);

            positCalculator.RepeatedDivision(memory);

            return (float)new Posit32(memory.ReadUInt32(Posit32AdvancedCalculator.RepeatedDivision_OutputPosit32Index), true);
        }

        public static float[] SqrtOfPositsInArray(this Posit32AdvancedCalculator posit32Calculator, uint[] posit32Array, IMemoryConfiguration memoryConfiguration)
        {
            var memory = SimpleMemory.Create(memoryConfiguration, posit32Array.Length + 1);

            memory.WriteUInt32(Posit32AdvancedCalculator.SqrtOfPositsInArray_InputPosit32CountIndex, (uint)posit32Array.Length);

            for (var i = 0; i < posit32Array.Length; i++)
            {
                memory.WriteUInt32(Posit32AdvancedCalculator.SqrtOfPositsInArray_InputPosit32sStartIndex + i, posit32Array[i]);
            }

            posit32Calculator.SqrtOfPositsInArray(memory);
            var resultArray = new float[posit32Array.Length];

            for (var i = 0; i < resultArray.Length; i++)
            {
                resultArray[i] = (float)new Posit32(memory.ReadUInt32(Posit32AdvancedCalculator.SqrtOfPositsInArray_OutputPosit32StartIndex + i), true);
            }
            return resultArray;
        }
    }
}
