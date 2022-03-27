namespace Hast.Algorithms.Random;

/// <summary>
/// The same linear-feedback shift register pseudo random number generator as in <see cref="RandomLfsr"/> but with
/// a 16b state. For details check out the documentation on the other implementation.
/// </summary>
public class RandomLfsr16
{
    /// <summary>
    /// Gets or sets the current inner state of the random number generator. If you set it when instantiating the object then
    /// it'll serve as a seed.
    /// </summary>
    /// <remarks>
    /// <para>By not using a constructor the whole class can be inlined for maximal performance.</para>
    /// </remarks>
    public ushort State { get; set; } = 49_813; // Just some starting number.

    public ushort NextUInt16()
    {
        // Using the taps from https://en.wikipedia.org/wiki/Linear-feedback_shift_register
        ushort tapBits = (ushort)((State >> 0) ^ (State >> 1) ^ (State >> 3) ^ (State >> 15));
        State = (ushort)((State >> 1) | (tapBits << 15));
        return State;
    }
}
