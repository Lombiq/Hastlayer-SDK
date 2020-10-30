using System;
using System.Collections.Generic;

namespace Hast.Layer
{
    /// <summary>
    /// Describes the capabilities of the connected hardware device.
    /// </summary>
    public interface IDeviceManifest
    {
        /// <summary>
        /// Gets the technical name that identifies the device.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the clock frequency of the board in Hz.
        /// </summary>
        uint ClockFrequencyHz { get; }

        /// <summary>
        /// Gets the names of those communication channels usable with the connected device. The first one will be used
        /// as the default.
        /// </summary>
        IEnumerable<string> SupportedCommunicationChannelNames { get; }

        /// <summary>
        /// The default communication channel to be used if none is configured.
        /// </summary>
        /// <remarks>
        /// Should be one of the channels in <see cref="SupportedCommunicationChannelNames"/>.
        /// </remarks>
        string DefaultCommunicationChannelName { get; }

        /// <summary>
        /// Gets the amount of memory (RAM) available to hardware implementations, in bytes.
        /// </summary>
        ulong AvailableMemoryBytes { get; }

        /// <summary>
        /// Gets the FPGA vendor toolchain's name to be used with this device.
        /// </summary>
        string ToolChainName { get; }

        Exception GetUnknownToolChainException();
    }
}
