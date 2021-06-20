using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using Lombiq.Arithmetics;

namespace Hast.Samples.SampleAssembly.Extensions
{
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
            memory.WriteUInt32(
                Posit32AdvancedCalculator.RepeatedDivisionFirstInputPosit32Index,
                new Posit32(dividend).PositBits);
            memory.WriteUInt32(
                Posit32AdvancedCalculator.RepeatedDivisionSecondInputPosit32Index,
                new Posit32(divisor).PositBits);

            positCalculator.RepeatedDivision(memory);

            return (float)new Posit32(
                memory.ReadUInt32(Posit32AdvancedCalculator.RepeatedDivisionOutputPosit32Index),
                true);
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

            memory.WriteUInt32(
                Posit32AdvancedCalculator.SqrtOfPositsInArrayInputPosit32CountIndex,
                (uint)posit32Array.Length);

            for (var i = 0; i < posit32Array.Length; i++)
            {
                memory.WriteUInt32(
                    Posit32AdvancedCalculator.SqrtOfPositsInArrayInputPosit32sStartIndex + i,
                    posit32Array[i]);
            }

            posit32Calculator.SqrtOfPositsInArray(memory);
            var resultArray = new float[posit32Array.Length];

            for (var i = 0; i < resultArray.Length; i++)
            {
                resultArray[i] =
                    (float)new Posit32(
                        memory.ReadUInt32(Posit32AdvancedCalculator.SqrtOfPositsInArrayOutputPosit32StartIndex + i),
                        true);
            }

            return resultArray;
        }
    }
}
