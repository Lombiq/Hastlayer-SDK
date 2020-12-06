using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.SampleAssembly.Extensions
{
    public static class UnumCalculatorExtensions
    {
        // While Hastlayer can figure out if an array is statically sized most of the time we need to specify the below
        // ones manually. See UnumCalculatorSampleRunner.
        public static readonly string[] ManuallySizedArrays = new[]
        {
            "System.UInt32[] Lombiq.Arithmetics.BitMask::Segments()",
            "System.Void Lombiq.Arithmetics.BitMask::.ctor(System.UInt32,System.UInt16).array",
            "System.Void Lombiq.Arithmetics.BitMask::.ctor(System.UInt32[],System.UInt16).segments",
            "System.Void Lombiq.Arithmetics.BitMask::.ctor(System.UInt16,System.Boolean).array",
            "System.Void Lombiq.Arithmetics.BitMask::.ctor(System.UInt32[],System.UInt16).array",
            "System.Void Hast.Samples.SampleAssembly.UnumCalculator::CalculateSumOfPowersofTwo(Hast.Transformer.SimpleMemory.SimpleMemory).array",
            "Lombiq.Arithmetics.BitMask Lombiq.Arithmetics.BitMask::op_Subtraction(Lombiq.Arithmetics.BitMask, Lombiq.Arithmetics.BitMask).array",
            "Lombiq.Arithmetics.BitMask Lombiq.Arithmetics.BitMask::op_Addition(Lombiq.Arithmetics.BitMask,Lombiq.Arithmetics.BitMask).array",
            "Lombiq.Arithmetics.BitMask Lombiq.Arithmetics.BitMask::op_Subtraction(Lombiq.Arithmetics.BitMask,Lombiq.Arithmetics.BitMask).array",
            "Lombiq.Arithmetics.BitMask Lombiq.Arithmetics.BitMask::op_BitwiseOr(Lombiq.Arithmetics.BitMask,Lombiq.Arithmetics.BitMask).array",
            "Lombiq.Arithmetics.BitMask Lombiq.Arithmetics.BitMask::op_ExclusiveOr(Lombiq.Arithmetics.BitMask,Lombiq.Arithmetics.BitMask).array",
            "Lombiq.Arithmetics.BitMask Lombiq.Arithmetics.BitMask::op_BitwiseAnd(Lombiq.Arithmetics.BitMask,Lombiq.Arithmetics.BitMask).array",
            "Lombiq.Arithmetics.BitMask Lombiq.Arithmetics.BitMask::op_RightShift(Lombiq.Arithmetics.BitMask,System.Int32).array",
            "Lombiq.Arithmetics.BitMask Lombiq.Arithmetics.BitMask::op_LeftShift(Lombiq.Arithmetics.BitMask,System.Int32).array",
            "System.UInt32[] Lombiq.Arithmetics.Unum::FractionToUintArray().array",
            "System.Void Lombiq.Arithmetics.Unum::.ctor(Lombiq.Arithmetics.UnumEnvironment,System.UInt32[],System.Boolean).value",
            "System.Void Lombiq.Arithmetics.Unum::.ctor(Lombiq.Arithmetics.UnumEnvironment,System.Int32).array",
            "System.Void Lombiq.Arithmetics.Unum::.ctor(Lombiq.Arithmetics.UnumEnvironment,System.UInt32).array",
        };

        public static uint[] CalculateSumOfPowersofTwo(
            this UnumCalculator unumCalculator,
            uint number,
            IHastlayer hastlayer = null,
            IHardwareGenerationConfiguration configuration = null)
        {
            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(9)
                : hastlayer.CreateMemory(configuration, 9);
            memory.WriteUInt32(UnumCalculator.CalculateSumOfPowersofTwoInputUInt32Index, number);
            unumCalculator.CalculateSumOfPowersofTwo(memory);
            var resultArray = new uint[9];
            for (var i = 0; i < 9; i++)
            {
                resultArray[i] = memory.ReadUInt32(UnumCalculator.CalculateSumOfPowersofTwoOutputUInt32Index + i);
            }

            return resultArray;
        }
    }
}
