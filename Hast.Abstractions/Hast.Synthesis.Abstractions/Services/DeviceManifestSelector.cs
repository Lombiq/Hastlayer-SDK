using Hast.Layer;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Synthesis.Abstractions.Services;

public class DeviceManifestSelector : IDeviceManifestSelector
{
    private readonly IEnumerable<IDeviceManifestProvider> _deviceManifestProviders;

    public DeviceManifestSelector(IEnumerable<IDeviceManifestProvider> deviceManifestProviders) =>
        _deviceManifestProviders = deviceManifestProviders;

    public IEnumerable<IDeviceManifest> GetSupportedDevices() =>
        _deviceManifestProviders
            .Select(provider => provider.DeviceManifest)
            .GroupBy(manifest => manifest.Name)
            .Select(group => group.First());
}
