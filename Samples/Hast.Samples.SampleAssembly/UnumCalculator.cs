using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Transformer.Abstractions.SimpleMemory;
using Lombiq.Arithmetics;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// A sample on using unum floating point numbers. For some info on unums see: http://www.johngustafson.net/unums.html
    /// </summary>
    public class UnumCalculator
    {
        public const int CalculateSumOfPowersofTwo_InputUInt32Index = 0;
        public const int CalculateSumOfPowersofTwo_OutputUInt32Index = 0;

        public virtual void CalculateSumOfPowersofTwo(SimpleMemory memory)
        {
            var number = memory.ReadUInt32(CalculateSumOfPowersofTwo_InputUInt32Index);

            var environment = EnvironmentFactory();

            var a = new Unum(environment, 1);
            var b = new Unum(environment, 0);

            for (var i = 1; i <= number; i++)
            {
                b += a;
                a += a;
            }

            var resultArray = b.FractionToUintArray();
            for (var i = 0; i < resultArray.Length; i++)
            {
                memory.WriteUInt32(CalculateSumOfPowersofTwo_OutputUInt32Index + i, resultArray[i]);
            }
        }

        // Needed so UnumCalculatorSampleRunner can retrieve BitMask.SegmentCount.
        // On the Nexys 4 DDR only a total of 6b environment will fit and work (9b would fit but wouldn't execute for
        // some reason).
        public static UnumEnvironment EnvironmentFactory() => new UnumEnvironment(2, 4);
    }

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
            "System.Void Lombiq.Arithmetics.Unum::.ctor(Lombiq.Arithmetics.UnumEnvironment,System.UInt32).array"
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
            memory.WriteUInt32(UnumCalculator.CalculateSumOfPowersofTwo_InputUInt32Index, number);
            unumCalculator.CalculateSumOfPowersofTwo(memory);
            var resultArray = new uint[9];
            for (var i = 0; i < 9; i++)
            {
                resultArray[i] = memory.ReadUInt32(UnumCalculator.CalculateSumOfPowersofTwo_OutputUInt32Index + i);
            }
            return resultArray;
        }
    }
}
