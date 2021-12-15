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
    /// A sample showing how custom-sized floating point numbers of type Posit (<see href="https://posithub.org" />)
    /// can be used with Hastlayer.
    /// </summary>
    /// <remarks>
    /// This sample is added here for future use. At the time statically-sized Posits like Posit32 are better usable,
    /// <see cref="Posit32Calculator"/>;
    /// </remarks>
    public class PositCalculator
    {
        public const int CalculateLargeIntegerSum_InputInt32Index = 0;
        public const int CalculateLargeIntegerSum_OutputInt32Index = 0;

        public virtual void CalculateIntegerSumUpToNumber(SimpleMemory memory)
        {
            var number = memory.ReadUInt32(CalculateLargeIntegerSum_InputInt32Index);

            var environment = EnvironmentFactory();

            var a = new Posit(environment, 1);
            var b = a;

            for (var i = 1; i < number; i++)
            {
                a += b;
            }

            var result = (int)a;
            memory.WriteInt32(CalculateLargeIntegerSum_OutputInt32Index, result);
        }

        public static PositEnvironment EnvironmentFactory() => new PositEnvironment(32, 3);
    }

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

            memory.WriteInt32(PositCalculator.CalculateLargeIntegerSum_InputInt32Index, number);
            positCalculator.CalculateIntegerSumUpToNumber(memory);

            return memory.ReadInt32(PositCalculator.CalculateLargeIntegerSum_OutputInt32Index);
        }
    }
}
