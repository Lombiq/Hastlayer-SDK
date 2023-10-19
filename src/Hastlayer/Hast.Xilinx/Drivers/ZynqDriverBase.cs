using Hast.Layer;
using Hast.Synthesis.Models;
using Hast.Synthesis.Services;
using Hast.Xilinx.Constants;
using Hast.Xilinx.Helpers;
using System;
using static Hast.Common.Constants.DataSize;
using static Hast.Common.Constants.Frequency;

namespace Hast.Xilinx.Drivers;

public abstract class ZynqDriverBase : DeviceDriverBase
{
    protected override string TimingReportFileName => nameof(ZynqDriverBase);

    protected ZynqDriverBase(ITimingReportParser timingReportParser)
        : base(timingReportParser) =>
        _deviceManifest = new Lazy<IDeviceManifest>(() => new ZynqDeviceManifest
        {
            Name = DeviceName,
            ClockFrequencyHz = 150 * Mhz,
            SupportedCommunicationChannelNames = new[] { Vitis.CommunicationChannelName },
            AvailableMemoryBytes = 1 * GigaByte,
            SupportsHbm = false,
            SupportedPlatforms = new[]
            {
                DeviceName.RegexReplace(@"[^A-Za-z0-9]+", "-"),
                "hw_platform",
            },
            // The frequency is set by ZynqHardwareImplementationComposerBuildProvider after build.
            BuildWithClockFrequencyHz = false,
            AxiBusWith = 1024,
        });

    public override void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration) =>
        MemoryConfigurationHelper.ConfigureMemoryForVitis(memory, hardwareGeneration);
}
