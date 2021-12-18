using System;
using System.Net;

namespace Hast.Communication.Models
{
    /// <summary>
    /// Represents an FPGA endpoint on the network containing availability details and IP endpoint.
    /// </summary>
    public interface IFpgaEndpoint
    {
        /// <summary>
        /// Gets IP address and port of the FPGA used for the communication.
        /// </summary>
        IPEndPoint Endpoint { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the FPGA is available and waiting for commands from the PC.
        /// </summary>
        bool IsAvailable { get; set; }

        /// <summary>
        /// Gets or sets the date when the availability status and IP endpoint was checked.
        /// </summary>
        DateTime LastCheckedUtc { get; set; }
    }
}
