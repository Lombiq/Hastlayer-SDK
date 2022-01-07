namespace Hast.Algorithms.Random
{
    /// <summary>
    /// Implementation of the MWC64X pseudo random number generator algorithm.
    /// <see href="http://cas.ee.ic.ac.uk/people/dt10/research/rngs-gpu-mwc64x.html"/> For a PRNG that has lower
    /// resource usage but also lower quality see <see cref="RandomLfsr"/>.
    /// </summary>
    public class RandomMwc64X
    {
        /// <summary>
        /// Used for calculating the next state after random number generation.
        /// </summary>
        private const ulong Multiplier = 0x_FFFE_B81B;

        /// <summary>
        /// Gets or sets the current inner state of the random number generator. If you set it when instantiating the object then
        /// it'll serve as a seed.
        /// </summary>
        /// <remarks>
        /// <para>By not using a constructor the whole class can be inlined for maximal performance.</para>
        /// </remarks>
        public ulong State { get; set; } = 0x_CAFEUL; // Just some starting number.

        public uint NextUInt32()
        {
            uint c = (uint)(State >> 32);
            uint x = (uint)State;
            State = (x * Multiplier) + c;
            return x ^ c;
        }
    }
}
