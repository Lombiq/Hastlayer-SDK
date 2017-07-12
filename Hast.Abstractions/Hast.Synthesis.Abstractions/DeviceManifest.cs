using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Layer;

namespace Hast.Synthesis.Abstractions
{
    public class DeviceManifest : IDeviceManifest
    {
        public string Name { get; set; }
        public uint ClockFrequencyHz { get; set; }
        public IEnumerable<string> SupportedCommunicationChannelNames { get; set; } = Enumerable.Empty<string>();
        public virtual string DefaultCommunicationChannelName { get { return SupportedCommunicationChannelNames.First(); } }
        public uint AvailableMemoryBytes { get; set; }
    }
}
