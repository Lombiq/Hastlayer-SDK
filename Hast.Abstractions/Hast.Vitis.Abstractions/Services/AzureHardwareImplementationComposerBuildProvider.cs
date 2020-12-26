using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Xilinx.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hast.Vitis.Abstractions.Services
{
    public class AzureHardwareImplementationComposerBuildProvider : IHardwareImplementationComposerBuildProvider
    {
        public ISet<string> Requirements { get; } = new HashSet<string> { nameof(VitisCommunicationService) };

        public bool CanCompose(IHardwareImplementationCompositionContext context) =>
            context.DeviceManifest is XilinxDeviceManifest xilinxDeviceManifest &&
            xilinxDeviceManifest.Name.StartsWith("Azure", StringComparison.InvariantCulture);

        public async Task BuildAsync(
            IHardwareImplementationCompositionContext context,
            IHardwareImplementation implementation)
        {

        }
    }
}
