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
        /// IP address and port of the FPGA used for the communication.
        /// </summary>
        IPEndPoint Endpoint { get; }

        /// <summary>
        /// It's <c>true</c> if the FPGA is available and waiting for commands from the PC.
        /// </summary>
        bool IsAvailable { get; set; }

        /// <summary>
        /// The date when the availability status and IP endpoint was checked.
        /// </summary>
        DateTime LastCheckedUtc { get; set; }
    }
}
