using Hast.Communication.Services;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using Hast.Vitis.Abstractions.Constants;
using Hast.Xilinx.Abstractions;
using System;
using System.IO;
using static Hast.Vitis.Abstractions.Constants.Extensions;

namespace Hast.Vitis.Abstractions.Services
{
    public class VitisHbmMemoryResourceChecker : IMemoryResourceChecker
    {
        /// <summary>
        /// Checks if HBM is used and then applies further memory restrictions.
        /// </summary>
        public void EnsureResourceAvailable(SimpleMemory memory, IHardwareRepresentation hardwareRepresentation)
        {
            var memoryByteCount = (ulong)memory.ByteCount;
            var binaryPath = hardwareRepresentation.HardwareImplementation.BinaryPath;

            if (memoryByteCount > Limits.HbmSizeBytes &&
                hardwareRepresentation.DeviceManifest is XilinxDeviceManifest xilinxDeviceManifest &&
                xilinxDeviceManifest.SupportsHbm &&
                !File.Exists(binaryPath + NoHbmFlagExtension))
            {
                throw new InvalidOperationException(
                    $"The input is too large to fit into the device's HBM memory: the input is {memoryByteCount} " +
                    $"bytes, the available memory is {Limits.HbmSizeBytes} bytes. Try rebuiling the kernel with " +
                    $"UseHbm option turned off. (see the Readme.md in Hast.Vitis.Abstractions)");
            }

        }
    }
}
