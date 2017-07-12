using System;
using Hast.Common.Numerics;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.SampleAssembly
{
    public enum SimdOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide
    }

    /// <summary>
    /// Sample to showcase SIMD (Simple Instruction Multiple Data) processing usage, i.e. operations executed in parallel
    /// on multiple elements of vectors.
    /// </summary>
    /// <remarks>
    /// System.Numerics.Vectors (including the NuGet package version of it: http://www.nuget.org/packages/System.Numerics.Vectors)
    /// could be used for SIMD processing on x64 systems. However Vector<T> can only contain that many elements that can
    /// fit into the processor's SIMD register and thus is quite inconvenient to use. So using a custom implementation.
    /// </remarks>
    public class SimdCalculator
    {
        public const int VectorsElementCountInt32Index = 0;
        public const int VectorElementsStartInt32Index = 1;
        public const int ResultVectorElementsStartInt32Index = 1;

        // This needs to be this low to fit all operations on the Nexys 4 DDR board's FPGA. On the same board just
        // transforming AddVectors or SubtractVectors could fit with a degree of parallelism of more than 500.
        public const int MaxDegreeOfParallelism = 30;


        public virtual void AddVectors(SimpleMemory memory)
        {
            RunSimdOperation(memory, SimdOperation.Add);
        }

        public virtual void SubtractVectors(SimpleMemory memory)
        {
            RunSimdOperation(memory, SimdOperation.Subtract);
        }

        public virtual void MultiplyVectors(SimpleMemory memory)
        {
            RunSimdOperation(memory, SimdOperation.Multiply);
        }

        public virtual void DivideVectors(SimpleMemory memory)
        {
            RunSimdOperation(memory, SimdOperation.Divide);
        }


        private void RunSimdOperation(SimpleMemory memory, SimdOperation operation)
        {
            var elementCount = memory.ReadInt32(VectorsElementCountInt32Index);

            int i = 0;

            while (i < elementCount)
            {
                var vector1 = new int[MaxDegreeOfParallelism];
                var vector2 = new int[MaxDegreeOfParallelism];
                var resultVector = new int[MaxDegreeOfParallelism];

                for (int m = 0; m < MaxDegreeOfParallelism; m++)
                {
                    vector1[m] = memory.ReadInt32(VectorElementsStartInt32Index + i + m);
                }

                for (int m = 0; m < MaxDegreeOfParallelism; m++)
                {
                    vector2[m] = memory.ReadInt32(VectorElementsStartInt32Index + i + m + elementCount);
                }

                switch (operation)
                {
                    case SimdOperation.Add:
                        resultVector = SimdOperations.AddVectors(vector1, vector2, MaxDegreeOfParallelism);
                        break;
                    case SimdOperation.Subtract:
                        resultVector = SimdOperations.SubtractVectors(vector1, vector2, MaxDegreeOfParallelism);
                        break;
                    case SimdOperation.Multiply:
                        resultVector = SimdOperations.MultiplyVectors(vector1, vector2, MaxDegreeOfParallelism);
                        break;
                    case SimdOperation.Divide:
                        resultVector = SimdOperations.DivideVectors(vector1, vector2, MaxDegreeOfParallelism);
                        break;
                    default:
                        break;
                }

                for (int m = 0; m < MaxDegreeOfParallelism; m++)
                {
                    memory.WriteInt32(ResultVectorElementsStartInt32Index + i + m, resultVector[m]);
                }

                i += MaxDegreeOfParallelism;
            }
        }
    }


    public static class SimdCalculatorExtensions
    {
        public static int[] AddVectors(this SimdCalculator simdCalculator, int[] vector1, int[] vector2)
        {
            return RunSimdOperation(vector1, vector2, memory => simdCalculator.AddVectors(memory));
        }

        public static int[] SubtractVectors(this SimdCalculator simdCalculator, int[] vector1, int[] vector2)
        {
            return RunSimdOperation(vector1, vector2, memory => simdCalculator.SubtractVectors(memory));
        }

        public static int[] MultiplyVectors(this SimdCalculator simdCalculator, int[] vector1, int[] vector2)
        {
            return RunSimdOperation(vector1, vector2, memory => simdCalculator.MultiplyVectors(memory));
        }

        public static int[] DivideVectors(this SimdCalculator simdCalculator, int[] vector1, int[] vector2)
        {
            return RunSimdOperation(vector1, vector2, memory => simdCalculator.DivideVectors(memory));
        }


        private static int[] RunSimdOperation(int[] vector1, int[] vector2, Action<SimpleMemory> operation)
        {
            SimdOperations.ThrowIfVectorsNotEquallyLong(vector1, vector2);

            var originalElementCount = vector1.Length;

            vector1 = vector1.PadToMultipleOf(SimdCalculator.MaxDegreeOfParallelism);
            vector2 = vector2.PadToMultipleOf(SimdCalculator.MaxDegreeOfParallelism);

            var elementCount = vector1.Length;
            var memory = new SimpleMemory(1 + elementCount * 2);

            memory.WriteInt32(SimdCalculator.VectorsElementCountInt32Index, elementCount);

            for (int i = 0; i < elementCount; i++)
            {
                memory.WriteInt32(SimdCalculator.VectorElementsStartInt32Index + i, vector1[i]);
                memory.WriteInt32(SimdCalculator.VectorElementsStartInt32Index + elementCount + i, vector2[i]);
            }

            operation(memory);

            var result = new int[elementCount];

            for (int i = 0; i < elementCount; i++)
            {
                result[i] = memory.ReadInt32(SimdCalculator.ResultVectorElementsStartInt32Index + i);
            }

            return result.CutToLength(originalElementCount);
        }
    }
}
