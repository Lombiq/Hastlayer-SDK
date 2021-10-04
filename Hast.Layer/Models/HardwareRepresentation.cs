using System.Collections.Generic;

namespace Hast.Layer.Models
{
    internal class HardwareRepresentation : IHardwareRepresentation
    {
        public IEnumerable<string> SoftAssemblyPaths { get; set; }
        public IHardwareDescription HardwareDescription { get; set; }
        public IHardwareImplementation HardwareImplementation { get; set; }
        public IDeviceManifest DeviceManifest { get; set; }
        public IHardwareGenerationConfiguration HardwareGenerationConfiguration { get; set; }
    }
}
