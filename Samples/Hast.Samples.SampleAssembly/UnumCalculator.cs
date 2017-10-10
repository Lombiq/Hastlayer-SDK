using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Transformer.Abstractions.SimpleMemory;
using Lombiq.Unum;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// A sample on using Unum floating point numbers. For some info on Unums see: http://www.johngustafson.net/unums.html
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
            "System.UInt32[] Lombiq.Unum.BitMask::Segments()",
            "System.Void Lombiq.Unum.BitMask::.ctor(System.UInt32,System.UInt16).array",
            "System.Void Lombiq.Unum.BitMask::.ctor(System.UInt32[],System.UInt16).segments",
            "System.Void Lombiq.Unum.BitMask::.ctor(System.UInt16,System.Boolean).array",
            "System.Void Hast.Samples.SampleAssembly.UnumCalculator::CalculateSumOfPowersofTwo(Hast.Transformer.SimpleMemory.SimpleMemory).array",
            "Lombiq.Unum.BitMask Lombiq.Unum.BitMask::op_Subtraction(Lombiq.Unum.BitMask, Lombiq.Unum.BitMask).array",
            "Lombiq.Unum.BitMask Lombiq.Unum.BitMask::op_Addition(Lombiq.Unum.BitMask,Lombiq.Unum.BitMask).array",
            "Lombiq.Unum.BitMask Lombiq.Unum.BitMask::op_Subtraction(Lombiq.Unum.BitMask,Lombiq.Unum.BitMask).array",
            "Lombiq.Unum.BitMask Lombiq.Unum.BitMask::op_BitwiseOr(Lombiq.Unum.BitMask,Lombiq.Unum.BitMask).array",
            "Lombiq.Unum.BitMask Lombiq.Unum.BitMask::op_ExclusiveOr(Lombiq.Unum.BitMask,Lombiq.Unum.BitMask).array",
            "Lombiq.Unum.BitMask Lombiq.Unum.BitMask::op_BitwiseAnd(Lombiq.Unum.BitMask,Lombiq.Unum.BitMask).array",
            "Lombiq.Unum.BitMask Lombiq.Unum.BitMask::op_RightShift(Lombiq.Unum.BitMask,System.Int32).array",
            "Lombiq.Unum.BitMask Lombiq.Unum.BitMask::op_LeftShift(Lombiq.Unum.BitMask,System.Int32).array",
            "System.UInt32[] Lombiq.Unum.Unum::FractionToUintArray().array",
            "System.Void Lombiq.Unum.Unum::.ctor(Lombiq.Unum.UnumEnvironment,System.UInt32[],System.Boolean).value",
            "System.Void Lombiq.Unum.Unum::.ctor(Lombiq.Unum.UnumEnvironment,System.Int32).array",
            "System.Void Lombiq.Unum.Unum::.ctor(Lombiq.Unum.UnumEnvironment,System.UInt32).array"
        };


        public static uint[] CalculateSumOfPowersofTwo(this UnumCalculator unumCalculator, uint number)
        {
            var memory = new SimpleMemory(9);
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
