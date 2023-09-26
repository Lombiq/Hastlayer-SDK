using Hast.Layer;
using Hast.Synthesis.Models;
using Hast.Synthesis.Services;
using Hast.Xilinx.Helpers;
using System;
using static Hast.Common.Constants.DataSize;
using static Hast.Common.Constants.Frequency;

namespace Hast.Xilinx.Drivers;

public abstract class VitisDeviceDriverBase : DeviceDriverBase
{
    public abstract string PlatformName { get; }
    public abstract uint ClockFrequencyMhz { get; }

    protected VitisDeviceDriverBase(ITimingReportParser timingReportParser)
        : base(timingReportParser) =>
        _deviceManifest = new Lazy<IDeviceManifest>(() => InitializeManifest(new VitisDeviceManifest()));

    public override void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration) =>
        MemoryConfigurationHelper.ConfigureMemoryForVitis(memory, hardwareGeneration);

    protected IDeviceManifest InitializeManifest(VitisDeviceManifest manifest)
    {
        manifest.Name = DeviceName;
        manifest.ClockFrequencyHz = ClockFrequencyMhz * Mhz;
        manifest.SupportedCommunicationChannelNames = new[] { Constants.VitisCommunicationChannelName };
        // While there is much more DDR RAM on the device, the max object size in .NET is 2GB. So until we add
        // paging to SimpleMemory the limit is 2GB, see: https://github.com/Lombiq/Hastlayer-SDK/issues/27.
        manifest.AvailableMemoryBytes = 2 * GigaByte;
        manifest.SupportedPlatforms = string.IsNullOrEmpty(PlatformName) ? Array.Empty<string>() : new[] { PlatformName };

        return manifest;
    }
}
