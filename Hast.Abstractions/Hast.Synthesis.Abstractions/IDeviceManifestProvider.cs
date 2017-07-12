using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Layer;
using Orchard;

namespace Hast.Synthesis.Abstractions
{
    public interface IDeviceManifestProvider : IDependency
    {
        IDeviceManifest DeviceManifest { get; }
    }
}
