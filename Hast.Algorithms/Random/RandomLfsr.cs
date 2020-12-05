namespace Hast.Algorithms.Random
{
    /// <summary>
    /// A very simple pseudo random number generator implemented with a
    /// <see href="https://en.wikipedia.org/wiki/Linear_feedback_shift_register">linear-feedback shift register</see>.
    /// Use this if you want simple but fast PRNG with the least resources. For a PRNG that has better quality but
    /// higher resource usage see <see cref="RandomMwc64X"/>.
    /// </summary>
    public class RandomLfsr
    {
        /// <summary>
        /// The current inner state of the random number generator. If you set it when instantiating the object then
        /// it'll serve as a seed.
        /// </summary>
        /// <remarks>
        /// <para>By not using a constructor the whole class can be inlined for maximal performance.</para>
        /// </remarks>
        public uint State { get; set; } = 498113; // Just some starting number.

        public uint NextUInt32()
        {
            // Using the taps from https://www.xilinx.com/support/documentation/application_notes/xapp052.pdf
            uint tapBits = State >> 0 ^ State >> 10 ^ State >> 30 ^ State >> 31;
            // Could also be
            // uint tapBits = State >> 0 ^ State >> 2 ^ State >> 6 ^ State >> 7;
            // according to the taps here: https://web.archive.org/web/20161007061934/http://courses.cse.tamu.edu/csce680/walker/lfsr_table.pdf

            State = State >> 1 | tapBits << 31;
            return State;
        }
    }
}
