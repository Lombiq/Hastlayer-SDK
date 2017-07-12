using System;
using System.Net;

namespace Hast.Communication.Models
{
    public class FpgaEndpoint : IFpgaEndpoint
    {
        public bool IsAvailable { get; set; }

        public IPEndPoint Endpoint { get; set; }

        public DateTime LastCheckedUtc { get; set; }
    }
}
