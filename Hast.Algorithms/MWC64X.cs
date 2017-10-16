namespace Hast.Algorithms
{
    /// <summary>
    /// Implementation of the MWC64X random number generator algorithm.
    /// <see href="http://cas.ee.ic.ac.uk/people/dt10/research/rngs-gpu-mwc64x.html"/>
    /// </summary>
    public class MWC64X
    {
        ulong state = 7215152093156152310UL; // Random seed.

        public uint GetNextRandom()
        {
            uint c = (uint)(state >> 32);
            uint x = (uint)(state & 0xFFFFFFFFUL);
            state = x * 4294883355UL + c;
            return x ^ c;
        }
    }
}