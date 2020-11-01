using Hast.Transformer.Abstractions.SimpleMemory;
using Hast.Vitis.Abstractions.Interop.Enums.OpenCl;
using Hast.Vitis.Abstractions.Services;

namespace Hast.Vitis.Abstractions.Models
{
    /// <summary>
    /// A configuration for the <see cref="OpenClCommunicationService"/>. The implementing class has sensible defaults
    /// that should work out of the box, but if configuration is necessary, the appconfig.json file should be edited.
    /// </summary>
    /// <remarks>
    /// An example for the appsettings file can be seen below or in the Hast.Samples.Consumer project. Everything
    /// omitted will remain as default.
    ///
    /// <c>
    /// {
    ///   "HardwareGenerationConfiguration": {
    ///     "CustomConfiguration": {
    ///       "OpenClConfiguration": {
    ///         "BinaryFilePath": "HardwareFramework/rtl/xclbin/hastip.hw.xclbin",
    ///         "HeaderCellCount": 4
    ///       }
    ///     }
    ///   }
    /// }
    /// </c>
    /// </remarks>
    public interface IOpenClConfiguration
    {
        /// <summary>
        /// If true, any <see cref="long"/> value in the header is considered big endian, if false then little endian.
        /// </summary>
        bool DeviceIsBigEndian { get; set; }

        /// <summary>
        /// The OpenCL <see cref="DeviceType"/>.
        /// </summary>
        DeviceType DeviceType { get; set; }

        /// <summary>
        /// The number of cells (<see cref="SimpleMemory.MemoryCellSizeBytes"/> byte units) to be occupied in the
        /// <see cref="SimpleMemory"/> as <see cref="SimpleMemory.PrefixCellCount"/>.
        /// </summary>
        int HeaderCellCount { get; set; }

        /// <summary>
        /// The vendor name used in the device's hardware information detectable by OpenCL.
        /// </summary>
        string VendorName { get; set; }

        /// <summary>
        /// The device may expect input data to be aligned against N byte chunks where N is this value. If zero then no
        /// aligning is expected.
        /// </summary>
        int MemoryAlignment { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the the binary is for real hardware or emulated.
        /// </summary>
        bool UseEmulation { get; set; }

        /// <summary>
        /// Gets or sets the Advanced eXtensible Interface bus width in bits.
        /// </summary>
        int AxiBusWith { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether cache (for accelerating access to the on-board DDR or HBM memory on
        /// the device) should be used. Default is <see langword="true"/>.
        /// </summary>
        bool UseCache { get; set; }
    }
}
