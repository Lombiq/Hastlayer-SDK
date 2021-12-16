using System.Linq;

namespace System
{
    internal static class ArrayExtensions
    {
        /// <summary>
        /// Padding the input array as necessary to have a multiple of MaxDegreeOfParallelism. This is needed because
        /// at the moment Hastlayer only supports a fixed degree of parallelism. This is the simplest way to overcome
        /// this.
        /// </summary>
        public static T[] PadToMultipleOf<T>(this T[] arrayToPad, int multipleOf)
        {
            _ = arrayToPad.Length;
            var remainderToMaxDegreeOfParallelism = arrayToPad.Length % multipleOf;
            if (remainderToMaxDegreeOfParallelism != 0)
            {
                return arrayToPad
                    .Concat(new T[multipleOf - remainderToMaxDegreeOfParallelism])
                    .ToArray();
            }

            return arrayToPad;
        }

        public static T[] CutToLength<T>(this T[] arrayToCut, int length)
        {
            if (arrayToCut.Length == length) return arrayToCut;
            return arrayToCut.Take(length).ToArray();
        }
    }
}
