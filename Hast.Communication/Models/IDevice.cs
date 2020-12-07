using System;

namespace Hast.Communication.Models
{
    /// <summary>
    /// Represents a compatible device for hardware execution.
    /// </summary>
    public interface IDevice : IDisposable
    {
        /// <summary>
        /// Event fires if the device (not <see cref="IReservedDevice"/>) is disposing.
        /// </summary>
        event EventHandler Disposing;

        /// <summary>
        /// Gets a string that uniquely identifies the given device.
        /// </summary>
        string Identifier { get; }

        /// <summary>
        /// Gets metadata associated with the device.
        /// </summary>
        dynamic Metadata { get; }
    }
}
