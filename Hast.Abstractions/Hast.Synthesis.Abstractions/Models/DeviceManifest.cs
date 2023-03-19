using Hast.Layer;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hast.Synthesis.Abstractions.Models;

[DebuggerDisplay("{Name}")]
public class DeviceManifest : IDeviceManifest
{
    public string Name { get; set; }
    public uint ClockFrequencyHz { get; set; }
    public IEnumerable<string> SupportedCommunicationChannelNames { get; set; } = Enumerable.Empty<string>();
    public virtual string DefaultCommunicationChannelName => SupportedCommunicationChannelNames.First();
    public ulong AvailableMemoryBytes { get; set; }
    public uint DataBusWidthBytes { get; set; }
}
