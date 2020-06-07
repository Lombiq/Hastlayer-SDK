using Hast.Vitis.Abstractions.Interop.Enums.OpenCl;
using Microsoft.Extensions.Configuration;

namespace Hast.Vitis.Abstractions.Models
{
    public class OpenClConfiguration : IOpenClConfiguration
    {
        public string BinaryFilePath { get; set; } = "HardwareFramework/rtl/xclbin/hastip.hw.xclbin";
        public bool DeviceIsBigEndian { get; set; } = true;
        public DeviceType DeviceType { get; set; } = DeviceType.Accelerator;
        public int HeaderCellCount { get; set; } = 4;
        public string VendorName { get; set; } = "Xilinx";
        public int MemoryAlignment { get; set; } = 4096;

        public static IOpenClConfiguration FromConfiguration(IConfiguration configuration)
        {
            var section = configuration.GetSection(nameof(OpenClConfiguration));
            return section?.Get<OpenClConfiguration>() ?? new OpenClConfiguration();
        }
    }
}
