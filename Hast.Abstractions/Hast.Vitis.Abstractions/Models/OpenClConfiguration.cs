using Hast.Vitis.Abstractions.Interop.Enums.OpenCl;

namespace Hast.Vitis.Abstractions.Models
{
    public class OpenClConfiguration : IOpenClConfiguration
    {
        public bool DeviceIsBigEndian { get; set; }
        public DeviceType DeviceType { get; set; } = DeviceType.Accelerator;
        public int HeaderCellCount { get; set; } = 4;
        public string VendorName { get; set; } = "Xilinx";
        public bool UseEmulation { get; set; }
        public bool UseCache { get; set; } = true;
        public bool UseHbm { get; set; } = true;
    }
}
