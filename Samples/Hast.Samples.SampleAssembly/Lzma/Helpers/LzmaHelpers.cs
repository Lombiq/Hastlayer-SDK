namespace Hast.Samples.SampleAssembly.Lzma.Helpers
{
    public static class LzmaHelpers
    {
        /// <summary>
        /// Replacement of the <see cref="System.Math.Min(byte, byte)"/> method that can be 
        /// transformed without decompiling the entire System assembly.
        /// </summary>
        /// <param name="firstValue">First value to be compared.</param>
        /// <param name="secondValue">Second value to be compared.</param>
        /// <returns>The lower one of the given values.</returns>
        public static uint GetMinValue(uint firstValue, uint secondValue) =>
            firstValue <= secondValue ? firstValue : secondValue;
    }
}
