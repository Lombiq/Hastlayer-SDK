using Hast.Synthesis.Abstractions;

namespace Hast.Layer
{
    public static class DeviceManifestExtensions
    {
        public static string GetBaseToolChainName(this IDeviceManifest manifest) =>
            manifest.ToolChainName.Split('-')[0].TrimEnd();

        public static bool UsesVivadoInToolChain(this IDeviceManifest manifest)
        {
            var baseToolChainName = GetBaseToolChainName(manifest);
            return baseToolChainName is CommonToolChainNames.Vivado or CommonToolChainNames.Vitis;
        }
    }
}
