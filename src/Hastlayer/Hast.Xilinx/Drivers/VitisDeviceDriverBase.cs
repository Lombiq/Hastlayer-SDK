using Hast.Layer;
using Hast.Synthesis;
using Hast.Synthesis.Models;
using Hast.Synthesis.Services;
using Hast.Xilinx.Helpers;
using System;
using static Hast.Common.Constants.DataSize;
using static Hast.Common.Constants.Frequency;

namespace Hast.Xilinx.Drivers;

public abstract class VitisDeviceDriverBase : DeviceDriverBase
{
    private readonly Lazy<IDeviceManifest> _deviceManifest;

    public abstract string DeviceName { get; }
    public abstract string PlatformName { get; }
    public abstract uint ClockFrequencyMhz { get; }

    public override IDeviceManifest DeviceManifest => _deviceManifest.Value;

    protected VitisDeviceDriverBase(ITimingReportParser timingReportParser)
        : base(timingReportParser) =>
        _deviceManifest = new Lazy<IDeviceManifest>(() => new VitisDeviceManifest
        {
            Name = DeviceName,
            ClockFrequencyHz = ClockFrequencyMhz * Mhz,
            SupportedCommunicationChannelNames = new[] { Constants.VitisCommunicationChannelName },
            // While there is 64GB DDR RAM the max object size in .NET is 2GB. So until we add paging to SimpleMemory
            // the limit is 2GB, see: https://github.com/Lombiq/Hastlayer-SDK/issues/27
            AvailableMemoryBytes = 2 * GigaByte,
            SupportedPlatforms = string.IsNullOrEmpty(PlatformName) ? Array.Empty<string>() : new[] { PlatformName },
        });

    public override void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration) =>
        MemoryConfigurationHelper.ConfigureMemoryForVitis(memory, hardwareGeneration);
}
