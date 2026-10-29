using Hast.Layer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Synthesis.Models;

public class MemoryConfiguration : IMemoryConfiguration
{
    private int _alignment;

    public int Alignment
    {
        get => _alignment;
        set
        {
            if (value < 0 || (value & (value - 1)) != 0)
            {
                throw new InvalidOperationException("The alignment value must be a power of 2.");
            }

            _alignment = value;
        }
    }

    public int MinimumPrefix { get; set; }

    private MemoryConfiguration() { }

    public static IMemoryConfiguration Create(
        IHardwareGenerationConfiguration hardwareGenerationConfiguration,
        IEnumerable<IDeviceDriver> deviceDrivers)
    {
        var memoryConfiguration = new MemoryConfiguration();
        var deviceManifestProvider = deviceDrivers.First(driver =>
            driver.DeviceManifest.Name == hardwareGenerationConfiguration.DeviceName);
        deviceManifestProvider.ConfigureMemory(memoryConfiguration, hardwareGenerationConfiguration);
        return memoryConfiguration;
    }
}
