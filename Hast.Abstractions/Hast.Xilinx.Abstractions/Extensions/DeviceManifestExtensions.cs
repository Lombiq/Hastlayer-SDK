using Hast.Xilinx;

namespace Hast.Layer;

public static class DeviceManifestExtensions
{
    public static bool UsesVivadoInToolChain(this IDeviceManifest manifest) => manifest is XilinxDeviceManifest;
}
