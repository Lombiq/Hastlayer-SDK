using Hast.SDAccel.Abstractions.Interop.Enums.OpenCl;

namespace Hast.SDAccel.Abstractions.Models
{
    public interface IOpenClConfiguration
    {
        string BinaryFilePath { get; set; }
        bool DeviceIsBigEndian { get; set; }
        DeviceType DeviceType { get; set; }
        int HeaderCellCount { get; set; }
        string VendorName { get; set; }
    }
}