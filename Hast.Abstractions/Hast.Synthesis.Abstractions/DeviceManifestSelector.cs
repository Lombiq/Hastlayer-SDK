using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Layer;

namespace Hast.Synthesis.Abstractions
{
    public class DeviceManifestSelector : IDeviceManifestSelector
    {
        private readonly IEnumerable<IDeviceManifestProvider> _deviceManifestProviders;


        public DeviceManifestSelector(IEnumerable<IDeviceManifestProvider> deviceManifestProviders)
        {
            _deviceManifestProviders = deviceManifestProviders;
        }


        public IEnumerable<IDeviceManifest> GetSupporteDevices() => 
            _deviceManifestProviders.Select(provider => provider.DeviceManifest);
    }
}
