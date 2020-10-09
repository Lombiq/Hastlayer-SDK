using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Vitis.Abstractions.Extensions;
using Hast.Xilinx.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hast.Vitis.Abstractions.Services
{
    public class VitisHardwareImplementationComposerBuildProvider : IHardwareImplementationComposerBuildProvider
    {
        public IEnumerable<string> SupportedComposers { get; } = new[] { nameof(VivadoHardwareImplementationComposer) };

        public bool IsSupported(IHardwareImplementationCompositionContext context) =>
            context.DeviceManifest is XilinxDeviceManifest xilinxDeviceManifest &&
            xilinxDeviceManifest.DeviceType == XilinxDeviceType.Vitis;

        public Task<IHardwareImplementation> BuildAsync(
            IHardwareImplementationCompositionContext context,
            string buildTarget)
        {
            var deviceManifest = (XilinxDeviceManifest)context.DeviceManifest;
            if (string.IsNullOrEmpty(deviceManifest?.TechnicalName))
            {
                throw new InvalidOperationException($"The device manifest for '{deviceManifest?.Name}' is missing " +
                                                    $"its technical name which is required to build.");
            }

            var hardwareFrameworkPath = context.Configuration.HardwareFrameworkPath;

            var openClConfiguration = context.Configuration.GetOrAddOpenClConfiguration();
            openClConfiguration.BinaryFilePath = buildTarget;

            throw new NotImplementedException();
        }
    }
}
