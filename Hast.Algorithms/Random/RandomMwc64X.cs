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
        /// The current inner state of the random number generator. If you set it when instantiating the object then 
        /// it'll serve as a seed.
        /// </summary>
        /// <remarks>
        /// By not using a constructor the whole class can be inlined for maximal performance.
        /// </remarks>
        public ulong State = 0xCAFEUL; // Just some starting number.

        public uint NextUInt32()
        {
            uint c = (uint)(State >> 32);
            uint x = (uint)State;
            State = x * 0xFFFEB81BUL + c;
            return x ^ c;
        }
    }
}