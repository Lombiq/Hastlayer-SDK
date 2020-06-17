namespace Hast.Synthesis.Abstractions
{
    public interface IMemoryConfiguration
    {
        /// <summary>
        /// The alignment value. If set to greater than 0, the starting address of the content is aligned to be a
        /// multiple of that number. It must be an integer and power of 2. It can only be set before any instances
        /// are created.
        /// </summary>
        int Alignment { get; }


        /// <summary>
        /// The minimum cell count to be reserved in front of the message payload. It's required for device-specific
        /// headers in the message.
        /// </summary>
        int MinimumPrefix { get; }
    }
}
