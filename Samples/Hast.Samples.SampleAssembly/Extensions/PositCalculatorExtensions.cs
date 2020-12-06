using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.SampleAssembly.Extensions
{
    public static class PositCalculatorExtensions
    {
        // While Hastlayer can figure out if an array is statically sized most of the time we need to specify the below
        // ones manually. See UnumCalculatorSampleRunner.
        public static readonly string[] ManuallySizedArrays = new[]
        {
            "System.UInt32[] Lombiq.Arithmetics.BitMask::Segments()",
            "System.Void Lombiq.Arithmetics.BitMask::.ctor(System.UInt32,System.UInt16).array",
            "System.Void Lombiq.Arithmetics.BitMask::.ctor(System.UInt32[],System.UInt16).segments",
            "System.Void Lombiq.Arithmetics.BitMask::.ctor(System.UInt16,System.Boolean).array",
            "Lombiq.Arithmetics.BitMask Lombiq.Arithmetics.BitMask::op_Subtraction(Lombiq.Arithmetics.BitMask, Lombiq.Arithmetics.BitMask).array",
            "Lombiq.Arithmetics.BitMask Lombiq.Arithmetics.BitMask::op_Addition(Lombiq.Arithmetics.BitMask,Lombiq.Arithmetics.BitMask).array",
            "Lombiq.Arithmetics.BitMask Lombiq.Arithmetics.BitMask::op_Subtraction(Lombiq.Arithmetics.BitMask,Lombiq.Arithmetics.BitMask).array",
            "Lombiq.Arithmetics.BitMask Lombiq.Arithmetics.BitMask::op_BitwiseOr(Lombiq.Arithmetics.BitMask,Lombiq.Arithmetics.BitMask).array",
            "Lombiq.Arithmetics.BitMask Lombiq.Arithmetics.BitMask::op_ExclusiveOr(Lombiq.Arithmetics.BitMask,Lombiq.Arithmetics.BitMask).array",
            "Lombiq.Arithmetics.BitMask Lombiq.Arithmetics.BitMask::op_BitwiseAnd(Lombiq.Arithmetics.BitMask,Lombiq.Arithmetics.BitMask).array",
            "Lombiq.Arithmetics.BitMask Lombiq.Arithmetics.BitMask::op_RightShift(Lombiq.Arithmetics.BitMask,System.Int32).array",
            "Lombiq.Arithmetics.BitMask Lombiq.Arithmetics.BitMask::op_LeftShift(Lombiq.Arithmetics.BitMask,System.Int32).array",
        };

        public static int CalculateIntegerSumUpToNumber(
            this PositCalculator positCalculator,
            int number,
            IHastlayer hastlayer = null,
            IHardwareGenerationConfiguration configuration = null)
        {
            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(1)
                : hastlayer.CreateMemory(configuration, 1);

            memory.WriteInt32(PositCalculator.CalculateLargeIntegerSumInputInt32Index, number);
            positCalculator.CalculateIntegerSumUpToNumber(memory);

            return memory.ReadInt32(PositCalculator.CalculateLargeIntegerSumOutputInt32Index);
        }
    }
}
