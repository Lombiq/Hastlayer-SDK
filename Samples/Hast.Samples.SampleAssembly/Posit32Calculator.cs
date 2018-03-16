using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Transformer.Abstractions.SimpleMemory;
using Lombiq.Arithmetics;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// A sample showing how floating point numbers of type posit (<see href="https://posithub.org" />) can be used 
    /// with Hastlayer. Using the statically-typed <see cref="Posit32"/> variant here.
    /// </summary>
    public class Posit32Calculator
    {
        public const int CalculateLargeIntegerSum_InputInt32Index = 0;
        public const int CalculateLargeIntegerSum_OutputInt32Index = 0;
        public const int AddPositsInArray_InputPosit32CountIndex = 0;
        public const int AddPositsInArray_InputPosit32sStartIndex = 1;
        public const int AddPositsInArray_OutputPosit32Index = 2;


        public virtual void CalculateIntegerSumUpToNumber(SimpleMemory memory)
        {
            var number = memory.ReadUInt32(CalculateLargeIntegerSum_InputInt32Index);

            var a = new Posit32(1);
            var b = a;

            for (uint i = 1; i < number; i++)
            {
                a += b;
            }

            var result = (int)a;
            memory.WriteInt32(CalculateLargeIntegerSum_OutputInt32Index, result);
        }

        public virtual void AddPositsInArray(SimpleMemory memory)
        {
            uint numberCount = memory.ReadUInt32(AddPositsInArray_InputPosit32CountIndex);

            var result = new Posit32(memory.ReadUInt32(AddPositsInArray_InputPosit32sStartIndex), true);

            for (int i = 1; i < numberCount; i++)
            {
                result += new Posit32(memory.ReadUInt32(AddPositsInArray_InputPosit32sStartIndex + i), true);
            }

            memory.WriteUInt32(AddPositsInArray_OutputPosit32Index, result.PositBits);
        }
    }


    public static class Posit32CalculatorExtensions
    {
        public static int CalculateIntegerSumUpToNumber(this Posit32Calculator positCalculator, int number)
        {
            var memory = new SimpleMemory(1);

            memory.WriteInt32(Posit32Calculator.CalculateLargeIntegerSum_InputInt32Index, number);
            positCalculator.CalculateIntegerSumUpToNumber(memory);

            return memory.ReadInt32(Posit32Calculator.CalculateLargeIntegerSum_OutputInt32Index);
        }

        public static float AddPositsInArray(this Posit32Calculator posit32Calculator, uint[] posit32Array)
        {
            var memory = new SimpleMemory(posit32Array.Length + 1);

            memory.WriteUInt32(Posit32Calculator.AddPositsInArray_InputPosit32CountIndex, (uint)posit32Array.Length);

            for (var i = 0; i < posit32Array.Length; i++)
            {
                memory.WriteUInt32(Posit32Calculator.AddPositsInArray_InputPosit32sStartIndex + i, posit32Array[i]);
            }

            posit32Calculator.AddPositsInArray(memory);

            return (float)new Posit32(memory.ReadUInt32(Posit32Calculator.AddPositsInArray_OutputPosit32Index), true);
        }
    }
}
