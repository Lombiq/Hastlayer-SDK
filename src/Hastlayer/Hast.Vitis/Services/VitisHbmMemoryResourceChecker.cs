using Hast.Communication.Models;
using Hast.Communication.Services;
using Hast.Layer;
using Hast.Transformer.SimpleMemory;
using Hast.Vitis.Constants;
using Hast.Vitis.Models;
using Hast.Xilinx;
using System.IO;
using static Hast.Vitis.Constants.FileNameExtensions;

namespace Hast.Vitis.Services;

public class VitisHbmMemoryResourceChecker : IMemoryResourceChecker
{
    /// <summary>
    /// Checks if HBM is used and then applies further memory restrictions.
    /// </summary>
    public MemoryResourceProblem EnsureResourceAvailable(SimpleMemory memory, IHardwareRepresentation hardwareRepresentation)
    {
        var memoryByteCount = (ulong)memory.ByteCount;
        var binaryPath = hardwareRepresentation.HardwareImplementation.BinaryPath;

        if (memoryByteCount > Limits.HbmSizeBytes &&
            hardwareRepresentation.DeviceManifest is VitisDeviceManifest vitisDeviceManifest &&
            vitisDeviceManifest.SupportsHbm &&
            !File.Exists(binaryPath + NoHbmFlagExtension))
        {
            return new MemoryResourceProblem
            {
                Sender = this,
                Message = $"The device uses HMB memory. If it also has DDR memory, disabling HMB via the " +
                          $"{nameof(IOpenClConfiguration)}.{nameof(IOpenClConfiguration.UseHbm)} option might " +
                          $"help, see the readme of the Hast.Vitis library.",
                AvailableByteCount = Limits.HbmSizeBytes,
                MemoryByteCount = memoryByteCount,
            };
        }

        return null;
    }
}
