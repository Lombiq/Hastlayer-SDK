using System.Runtime.CompilerServices;

namespace Hast.Algorithms
{
    /// <summary>
    /// Implementation of the MWC64X random number generator algorithm.
    /// <see href="http://cas.ee.ic.ac.uk/people/dt10/research/rngs-gpu-mwc64x.html"/>
    /// </summary>
    public class RandomMwc64X
    {
        public ulong State; // Random seed.


        public RandomMwc64X(ulong seed) { State = seed; }
        public RandomMwc64X() { State = 0xCAFEUL; }


        public uint NextUInt32()
        {
            uint c = (uint)(State >> 32);
            uint x = (uint)State;
            // Creating the value 0xFFFEB81BUL. 
            // This literal can't be directly used due to an ILSpy bug, see:
            // https://github.com/icsharpcode/ILSpy/issues/807
            uint zLow  = 0xFFFE;
            uint zHigh = 0xB81B;
            uint z = 0 | (zLow << 16) | zHigh;
            State = (ulong)x * z + c;
            return x ^ c;
        }
    }
}