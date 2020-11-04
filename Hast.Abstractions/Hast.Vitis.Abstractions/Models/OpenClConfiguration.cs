using Hast.Vitis.Abstractions.Interop.Enums.OpenCl;

namespace Hast.Vitis.Abstractions.Models
{
    public class OpenClConfiguration : IOpenClConfiguration
    {
        public bool DeviceIsBigEndian { get; set; }
        public DeviceType DeviceType { get; set; } = DeviceType.Accelerator;
        public int HeaderCellCount { get; set; } = 4;
        public string VendorName { get; set; } = "Xilinx";
        public int MemoryAlignment { get; set; } = 4096;
        public bool UseEmulation { get; set; }
        public int AxiBusWith { get; set; } = 512;
        public bool UseCache { get; set; } = true;
    }
}
