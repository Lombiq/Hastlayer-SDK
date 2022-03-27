using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Xilinx.Abstractions.Helpers;
using System;
using static Hast.Common.Constants.DataSize;
using static Hast.Common.Constants.Frequency;

namespace Hast.Xilinx.Abstractions.ManifestProviders;

public abstract class ZynqManifestProviderBase : IDeviceManifestProvider
{
    protected string _deviceName;

    private IDeviceManifest _deviceManifest;
    public IDeviceManifest DeviceManifest =>
        _deviceManifest ??= new ZynqDeviceManifest
        {
            Name = _deviceName,
            ClockFrequencyHz = 150 * Mhz,
            SupportedCommunicationChannelNames = new[] { Constants.VitisCommunicationChannelName },
            AvailableMemoryBytes = 1 * GigaByte,
            SupportsHbm = false,
            SupportedPlatforms = new[]
            {
                _deviceName.RegexReplace(@"[^A-Za-z0-9]+", "-"),
                "hw_platform",
            },
            // The frequency is set by ZynqHardwareImplementationComposerBuildProvider after build.
            BuildWithClockFrequencyHz = false,
            AxiBusWith = 1024,
        };

    public void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration) =>
        MemoryConfigurationHelper.ConfigureMemoryForVitis(memory, hardwareGeneration);
}
