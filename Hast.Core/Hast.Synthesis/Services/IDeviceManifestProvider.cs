using Hast.Common.Interfaces;
using Hast.Layer;
using Hast.Synthesis.Models;

namespace Hast.Synthesis.Services;

/// <summary>
/// Provides a <see cref="IDeviceManifest"/> and <c>SimpleMemory</c> creation.
/// </summary>
public interface IDeviceManifestProvider : ISingletonDependency
{
    /// <summary>
    /// Gets the manifest with the device information.
    /// </summary>
    IDeviceManifest DeviceManifest { get; }

    /// <summary>
    /// Used during <see cref="MemoryConfiguration.Create"/> to set up device specific configuration.
    /// </summary>
    void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration);
}
