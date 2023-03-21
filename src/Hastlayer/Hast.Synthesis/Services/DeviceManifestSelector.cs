using Hast.Layer;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Synthesis.Services;

public class DeviceManifestSelector : IDeviceManifestSelector
{
    private readonly IEnumerable<IDeviceDriver> _deviceDrivers;
    
    public DeviceManifestSelector(IEnumerable<IDeviceDriver> deviceDrivers) =>
        _deviceDrivers = deviceDrivers;

    public IEnumerable<IDeviceManifest> GetSupportedDevices() =>
        _deviceDrivers
            .Select(driver => driver.DeviceManifest)
            .GroupBy(manifest => manifest.Name)
            .Select(group => group.First());
}
