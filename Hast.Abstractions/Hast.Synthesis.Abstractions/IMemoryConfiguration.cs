using Hast.Common.Interfaces;

namespace Hast.Synthesis.Abstractions
{
    public interface IMemoryConfiguration
    {
        /// <summary>
        /// The alignment value. If set to greater than 0, the starting address of the content is aligned to be a
        /// multiple of that number. It must be an integer power of 2. It can only be set before any instances are
        /// created.
        /// </summary>
        int Alignment { get; }


        /// <summary>
        /// The minimum prefix size expected by the device.
        /// </summary>
        int MinimumPrefix { get; }
    }
}
