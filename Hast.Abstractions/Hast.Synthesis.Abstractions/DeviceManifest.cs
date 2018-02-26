using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Hast.Layer;

namespace Hast.Synthesis.Abstractions
{
    [DebuggerDisplay("{Name}")]
    public class DeviceManifest : IDeviceManifest
    {
        public string Name { get; set; }
        public uint ClockFrequencyHz { get; set; }
        public IEnumerable<string> SupportedCommunicationChannelNames { get; set; } = Enumerable.Empty<string>();
        public virtual string DefaultCommunicationChannelName { get { return SupportedCommunicationChannelNames.First(); } }
        public uint AvailableMemoryBytes { get; set; }
        public uint DataBusWidthBytes { get; set; }
    }
}
