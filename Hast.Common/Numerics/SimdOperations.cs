using System;
using System.Diagnostics.CodeAnalysis;

namespace Hast.Common.Numerics
{
    /// <summary>
    /// Special Hastlayer-supported SIMD (Single Instruction Multiple Data) operations that can be utilized to execute
    /// an operation on multiple elements at once.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These operations are transformed into SIMD-like operations but currently they are plainly sequential in .NET.
    /// </para>
    /// </remarks>
    [SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "TODO: parallelize these to SIMD in .NET too.")]
    public static class SimdOperations
    {
        public static int[] AddVectors(int[] vector1, int[] vector2, int maxDegreeOfParallelism) =>
            RunInt32VectorOperation(vector1, vector2, (element1, element2) => element1 + element2);

        public static uint[] AddVectors(uint[] vector1, uint[] vector2, int maxDegreeOfParallelism) =>
            RunUInt32VectorOperation(vector1, vector2, (element1, element2) => element1 + element2);

        public static long[] AddVectors(long[] vector1, long[] vector2, int maxDegreeOfParallelism) =>
            RunInt64VectorOperation(vector1, vector2, (element1, element2) => element1 + element2);

        public static ulong[] AddVectors(ulong[] vector1, ulong[] vector2, int maxDegreeOfParallelism) =>
            RunUInt64VectorOperation(vector1, vector2, (element1, element2) => element1 + element2);

        public static int[] SubtractVectors(int[] vector1, int[] vector2, int maxDegreeOfParallelism) =>
            RunInt32VectorOperation(vector1, vector2, (element1, element2) => element1 - element2);

        public static uint[] SubtractVectors(uint[] vector1, uint[] vector2, int maxDegreeOfParallelism) =>
            RunUInt32VectorOperation(vector1, vector2, (element1, element2) => element1 - element2);

        public static long[] SubtractVectors(long[] vector1, long[] vector2, int maxDegreeOfParallelism) =>
            RunInt64VectorOperation(vector1, vector2, (element1, element2) => element1 - element2);

        public static ulong[] SubtractVectors(ulong[] vector1, ulong[] vector2, int maxDegreeOfParallelism) =>
            RunUInt64VectorOperation(vector1, vector2, (element1, element2) => element1 - element2);

        public static int[] MultiplyVectors(int[] vector1, int[] vector2, int maxDegreeOfParallelism) =>
            RunInt32VectorOperation(vector1, vector2, (element1, element2) => element1 * element2);

        public static uint[] MultiplyVectors(uint[] vector1, uint[] vector2, int maxDegreeOfParallelism) =>
            RunUInt32VectorOperation(vector1, vector2, (element1, element2) => element1 * element2);

        public static long[] MultiplyVectors(long[] vector1, long[] vector2, int maxDegreeOfParallelism) =>
            RunInt64VectorOperation(vector1, vector2, (element1, element2) => element1 * element2);

        public static ulong[] MultiplyVectors(ulong[] vector1, ulong[] vector2, int maxDegreeOfParallelism) =>
            RunUInt64VectorOperation(vector1, vector2, (element1, element2) => element1 * element2);

        public static int[] DivideVectors(int[] vector1, int[] vector2, int maxDegreeOfParallelism) =>
            RunInt32VectorOperation(vector1, vector2, (element1, element2) => element1 / element2);

        public static uint[] DivideVectors(uint[] vector1, uint[] vector2, int maxDegreeOfParallelism) =>
            RunUInt32VectorOperation(vector1, vector2, (element1, element2) => element1 / element2);

        public static long[] DivideVectors(long[] vector1, long[] vector2, int maxDegreeOfParallelism) =>
            RunInt64VectorOperation(vector1, vector2, (element1, element2) => element1 / element2);

        public static ulong[] DivideVectors(ulong[] vector1, ulong[] vector2, int maxDegreeOfParallelism) =>
            RunUInt64VectorOperation(vector1, vector2, (element1, element2) => element1 / element2);

        public static void ThrowIfVectorsNotEquallyLong<T>(T[] vector1, T[] vector2)
        {
            if (vector1.Length != vector2.Length)
            {
                throw new InvalidOperationException("The two vectors must have the same number of elements.");
            }
        }

        private static int[] RunInt32VectorOperation(int[] vector1, int[] vector2, Func<int, int, int> operation)
        {
            ThrowIfVectorsNotEquallyLong(vector1, vector2);

            var result = new int[vector1.Length];

            for (int i = 0; i < vector1.Length; i++)
            {
                result[i] = operation(vector1[i], vector2[i]);
            }

            return result;
        }

        private static uint[] RunUInt32VectorOperation(uint[] vector1, uint[] vector2, Func<uint, uint, uint> operation)
        {
            ThrowIfVectorsNotEquallyLong(vector1, vector2);

            var result = new uint[vector1.Length];

            for (int i = 0; i < vector1.Length; i++)
            {
                result[i] = operation(vector1[i], vector2[i]);
            }

            return result;
        }

        private static long[] RunInt64VectorOperation(long[] vector1, long[] vector2, Func<long, long, long> operation)
        {
            ThrowIfVectorsNotEquallyLong(vector1, vector2);

            var result = new long[vector1.Length];

            for (int i = 0; i < vector1.Length; i++)
            {
                result[i] = operation(vector1[i], vector2[i]);
            }

            return result;
        }

        private static ulong[] RunUInt64VectorOperation(ulong[] vector1, ulong[] vector2, Func<ulong, ulong, ulong> operation)
        {
            ThrowIfVectorsNotEquallyLong(vector1, vector2);

            var result = new ulong[vector1.Length];

            for (int i = 0; i < vector1.Length; i++)
            {
                result[i] = operation(vector1[i], vector2[i]);
            }

            return result;
        }
    }
}
