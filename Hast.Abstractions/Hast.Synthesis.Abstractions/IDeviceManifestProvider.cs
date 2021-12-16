using Hast.Common.Interfaces;
using Hast.Layer;

namespace Hast.Synthesis.Abstractions
{
    public interface IDeviceManifestProvider : ISingletonDependency
    {
        IDeviceManifest DeviceManifest { get; }

        void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration);
    }
}
