namespace Hast.Algorithms.Random
{
    /// <summary>
    /// A linear-feedback shift register pseudo random number generator with a 16b state. For details check out <see
    /// href="https://en.wikipedia.org/wiki/Linear-feedback_shift_register">Wikipedia</see>. Also see <see
    /// href="https://en.wikipedia.org/wiki/Xorshift"/>.
    /// </summary>
    public class RandomXorshiftLfsr16
    {
        /// <summary>
        /// The current inner state of the random number generator. If you set it when instantiating the object then 
        /// it'll serve as a seed.
        /// </summary>
        /// <remarks>
        /// By not using a constructor the whole class can be inlined for maximal performance.
        /// </remarks>
        public ushort State = 49813; // Just some starting number.

        public ushort NextUInt16()
        {
            State ^= (ushort)(State >> 7);
            State ^= (ushort)(State << 9);
            State ^= (ushort)(State >> 13);

            return State;
        }
    }
}
