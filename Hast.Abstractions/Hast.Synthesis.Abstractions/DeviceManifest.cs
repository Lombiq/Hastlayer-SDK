using Hast.Layer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hast.Synthesis.Abstractions
{
    [DebuggerDisplay("{Name}")]
    public class DeviceManifest : IDeviceManifest
    {
        public string Name { get; set; }
        public uint ClockFrequencyHz { get; set; }
        public IEnumerable<string> SupportedCommunicationChannelNames { get; set; } = Enumerable.Empty<string>();
        public virtual string DefaultCommunicationChannelName => SupportedCommunicationChannelNames.First();
        public ulong AvailableMemoryBytes { get; set; }
        public uint DataBusWidthBytes { get; set; }
        public string ToolChainName { get; set; }


        public Exception CreateUnknownToolChainException() =>
            new InvalidOperationException($"Unknown tool chain: {ToolChainName}");
    }
}
