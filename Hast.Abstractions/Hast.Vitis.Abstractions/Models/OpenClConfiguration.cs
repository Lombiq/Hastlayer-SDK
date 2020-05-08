using Hast.Vitis.Abstractions.Interop.Enums.OpenCl;

namespace Hast.Vitis.Abstractions.Models
{
    public class OpenClConfiguration : IOpenClConfiguration
    {
        public string BinaryFilePath { get; set; } = "xclbin/hastip.hw_emu.xclbin";
        public bool DeviceIsBigEndian { get; set; } = true;
        public DeviceType DeviceType { get; set; } = DeviceType.Accelerator;
        public int HeaderCellCount { get; set; } = 4;
        public string VendorName { get; set; } = "Xilinx";
    }
}
