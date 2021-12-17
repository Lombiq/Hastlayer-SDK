using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using Lombiq.Arithmetics;

namespace Hast.Samples.SampleAssembly
{
    public class Posit32AdvancedCalculator
    {

        public const int RepeatedDivisionInputInt32Index = 0;
        public const int RepeatedDivisionFirstInputPosit32Index = 1;
        public const int RepeatedDivisionSecondInputPosit32Index = 2;
        public const int RepeatedDivisionOutputPosit32Index = 0;

        public const int SqrtOfPositsInArrayInputPosit32CountIndex = 0;
        public const int SqrtOfPositsInArrayInputPosit32sStartIndex = 1;
        public const int SqrtOfPositsInArrayOutputPosit32StartIndex = 0;

        public virtual void RepeatedDivision(SimpleMemory memory)
        {
            var number = memory.ReadInt32(RepeatedDivisionInputInt32Index);
            var dividendPosit = memory.ReadUInt32(RepeatedDivisionFirstInputPosit32Index);
            var divisorPosit = memory.ReadUInt32(RepeatedDivisionSecondInputPosit32Index);

            var a = new Posit32(dividendPosit, true);
            var b = new Posit32(divisorPosit, true);

            for (uint i = 0; i < number; i++)
            {
                a /= b;
            }

            var result = a.PositBits;
            memory.WriteUInt32(RepeatedDivisionOutputPosit32Index, result);
        }

        public virtual void SqrtOfPositsInArray(SimpleMemory memory)
        {
            uint numberCount = memory.ReadUInt32(SqrtOfPositsInArrayInputPosit32CountIndex);

            var result = new Posit32(memory.ReadUInt32(SqrtOfPositsInArrayInputPosit32sStartIndex), true);

            for (int i = 0; i < numberCount; i++)
            {
                result = Posit32.Sqrt(new Posit32(memory.ReadUInt32(SqrtOfPositsInArrayInputPosit32sStartIndex + i), true));
                memory.WriteUInt32(SqrtOfPositsInArrayOutputPosit32StartIndex + i, result.PositBits);
            }
        }
    }

    public static class Posit32AdvancedCalculatorExtensions
    {
        public static float RepeatedDivision(
            this Posit32AdvancedCalculator positCalculator,
            int number,
            float dividend,
            float divisor,
            IHastlayer hastlayer = null,
            IHardwareGenerationConfiguration configuration = null)
        {
            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(3)
                : hastlayer.CreateMemory(configuration, 3);

            memory.WriteInt32(Posit32AdvancedCalculator.RepeatedDivisionInputInt32Index, number);
            memory.WriteUInt32(Posit32AdvancedCalculator.RepeatedDivisionFirstInputPosit32Index, new Posit32(dividend).PositBits);
            memory.WriteUInt32(Posit32AdvancedCalculator.RepeatedDivisionSecondInputPosit32Index, new Posit32(divisor).PositBits);

            positCalculator.RepeatedDivision(memory);

            return (float)new Posit32(memory.ReadUInt32(Posit32AdvancedCalculator.RepeatedDivisionOutputPosit32Index), true);
        }

        public static float[] SqrtOfPositsInArray(
            this Posit32AdvancedCalculator posit32Calculator,
            uint[] posit32Array,
            IHastlayer hastlayer = null,
            IHardwareGenerationConfiguration configuration = null)
        {
            var cellCount = posit32Array.Length + 1;
            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(cellCount)
                : hastlayer.CreateMemory(configuration, cellCount);

            memory.WriteUInt32(Posit32AdvancedCalculator.SqrtOfPositsInArrayInputPosit32CountIndex, (uint)posit32Array.Length);

            for (var i = 0; i < posit32Array.Length; i++)
            {
                memory.WriteUInt32(Posit32AdvancedCalculator.SqrtOfPositsInArrayInputPosit32sStartIndex + i, posit32Array[i]);
            }

            posit32Calculator.SqrtOfPositsInArray(memory);
            var resultArray = new float[posit32Array.Length];

            for (var i = 0; i < resultArray.Length; i++)
            {
                resultArray[i] = (float)new Posit32(
                    memory.ReadUInt32(Posit32AdvancedCalculator.SqrtOfPositsInArrayOutputPosit32StartIndex + i),
                    true);
            }

            return resultArray;
        }
    }
}
