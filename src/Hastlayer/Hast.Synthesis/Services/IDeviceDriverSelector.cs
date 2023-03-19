using Hast.Common.Interfaces;

namespace Hast.Synthesis.Services;

/// <summary>
/// A collection of <see cref="IDeviceDriver"/> instances you can select by device name.
/// </summary>
public interface IDeviceDriverSelector : IDependency
{
    /// <summary>
    /// Returns a driver by device name.
    /// </summary>
    IDeviceDriver GetDriver(string deviceName);
}
