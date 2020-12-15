using Hast.Synthesis.Abstractions;

namespace Hast.Layer
{
    public static class DeviceManifestExtensions
    {
        public static bool UsesVivadoInToolChain(this IDeviceManifest manifest) =>
            manifest.ToolChainName == CommonToolChainNames.Vivado ||
            manifest.ToolChainName == CommonToolChainNames.Vitis;
    }
}
