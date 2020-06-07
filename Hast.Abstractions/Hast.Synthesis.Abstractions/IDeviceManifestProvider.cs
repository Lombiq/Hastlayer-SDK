using Hast.Common.Interfaces;
using Hast.Layer;
using Microsoft.Extensions.Configuration;

namespace Hast.Synthesis.Abstractions
{
    public interface IDeviceManifestProvider : ISingletonDependency
    {
        IDeviceManifest DeviceManifest { get; }

        void ConfigureMemory(MemoryConfiguration memoryConfiguration, IConfiguration configuration);
    }
}
