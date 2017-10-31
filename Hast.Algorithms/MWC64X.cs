namespace Hast.Algorithms
{
    /// <summary>
    /// Implementation of the MWC64X random number generator algorithm.
    /// <see href="http://cas.ee.ic.ac.uk/people/dt10/research/rngs-gpu-mwc64x.html"/>
    /// </summary>
    public class PrngMWC64X
    {
        public ulong state; // Random seed.

        public PrngMWC64X(ulong seed) { state = seed; }
        public PrngMWC64X() { state = 0xCAFEUL; }

        public uint NextUInt32()
        {
            uint c = (uint)(state >> 32);
            uint x = (uint)state;
            uint zLow  = 0xFFFE;
            uint zHigh = 0xB81B;
            uint z = (0 << 32) | (zLow << 16) | zHigh; //workaround due to ILSpy bug
            state = (ulong)x * (ulong)z + (ulong)c;
            return x ^ c;
        }
    }
}