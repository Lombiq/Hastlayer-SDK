using System;
using Microsoft.Extensions.Configuration;

namespace Hast.Synthesis.Abstractions
{
    public class MemoryConfiguration : IMemoryConfiguration
    {
        private int _alignment = 0;

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
            IDeviceManifestProvider deviceManifestProvider,
            IConfiguration configuration)
        {
            var memoryConfiguration = new MemoryConfiguration();
            deviceManifestProvider.ConfigureMemory(memoryConfiguration, configuration);
            return memoryConfiguration;
        }

    }
}
