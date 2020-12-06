using System;
using System.Collections.Generic;
using System.Linq;
using Hast.Layer;

namespace Hast.Synthesis.Abstractions
{
    public class MemoryConfiguration : IMemoryConfiguration
    {
        private int _alignment;

        public int Alignment
        {
            get => _alignment;
            set
            {
                if (value < 0 || (value & value - 1) != 0)
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
            IEnumerable<IDeviceManifestProvider> deviceManifestProviders)
        {
            var memoryConfiguration = new MemoryConfiguration();
            var deviceManifestProvider = deviceManifestProviders.First(manifestProvider =>
                manifestProvider.DeviceManifest.Name == hardwareGenerationConfiguration.DeviceName);
            deviceManifestProvider.ConfigureMemory(memoryConfiguration, hardwareGenerationConfiguration);
            return memoryConfiguration;
        }

    }
}
