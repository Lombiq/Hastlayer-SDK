using Hast.Vitis.Abstractions.Interop.Enums.OpenCl;

namespace Hast.Vitis.Abstractions.Models
{
    public class OpenClConfiguration : IOpenClConfiguration
    {
        public bool DeviceIsBigEndian { get; set; }
        public DeviceTypes DeviceType { get; set; } = DeviceTypes.Accelerator;
        public int HeaderCellCount { get; set; } = 4;
        public string VendorName { get; set; } = "Xilinx";
        public bool UseEmulation { get; set; }
        public int AxiBusWith { get; set; } = 512;
        public bool UseCache { get; set; } = true;
        public bool UseHbm { get; set; } = true;
    }
}
