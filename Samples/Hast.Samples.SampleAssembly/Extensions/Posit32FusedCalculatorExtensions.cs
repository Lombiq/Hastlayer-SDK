using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using Lombiq.Arithmetics;

namespace Hast.Samples.SampleAssembly.Extensions
{
    public static class Posit32FusedCalculatorExtensions
    {
        public static float CalculateFusedSum(
            this Posit32FusedCalculator posit32FusedCalculator,
            uint[] posit32Array,
            IHastlayer hastlayer = null,
            IHardwareGenerationConfiguration configuration = null)
        {
            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(posit32Array.Length + 1)
                : hastlayer.CreateMemory(configuration, posit32Array.Length + 1);

            memory.WriteUInt32(Posit32FusedCalculator.CalculateFusedSumInputPosit32CountIndex, (uint)posit32Array.Length);

            for (var i = 0; i < posit32Array.Length; i++)
            {
                memory.WriteUInt32(Posit32FusedCalculator.CalculateFusedSumInputPosit32StartIndex + i, posit32Array[i]);
            }

            posit32FusedCalculator.CalculateFusedSum(memory);

            return (float)new Posit32(memory.ReadUInt32(Posit32FusedCalculator.CalculateFusedSumOutputPosit32Index), true);
        }

        public static readonly string[] ManuallySizedArrays = new[]
        {
            "System.UInt64[] Lombiq.Arithmetics.Quire::Segments()",
            "Lombiq.Arithmetics.Quire Lombiq.Arithmetics.Quire::op_Addition(Lombiq.Arithmetics.Quire,Lombiq.Arithmetics.Quire).array",
            "Lombiq.Arithmetics.Quire Lombiq.Arithmetics.Quire::op_RightShift(Lombiq.Arithmetics.Quire,System.Int32).array",
            "Lombiq.Arithmetics.Quire Lombiq.Arithmetics.Quire::op_LeftShift(Lombiq.Arithmetics.Quire,System.Int32).array",
        };
    }
}
