using System.Collections.Generic;
using System.Linq;

namespace Hast.Synthesis.Services;

public class DeviceDriverSelector : IDeviceDriverSelector
{
    private readonly IEnumerable<IDeviceDriver> _drivers;

    public DeviceDriverSelector(IEnumerable<IDeviceDriver> drivers) => _drivers = drivers;

    public IDeviceDriver GetDriver(string deviceName) =>
        _drivers.FirstOrDefault(driver => driver.DeviceManifest?.Name == deviceName);
}
