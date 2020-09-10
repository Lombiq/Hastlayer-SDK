using System;
using Hast.Vitis.Abstractions.Interop.Enums.OpenCl;

namespace Hast.Vitis.Abstractions.Interop
{
    /// <summary>
    /// Xilinx-specific memory extensions. Control bank allocation of buffer object.
    /// </summary>
    /// <remarks>
    /// Source: https://github.com/Xilinx/XRT/blob/master/src/include/1_2/CL/cl_ext_xilinx.h
    /// Legacy layout is used since that's the one in the examples here:
    /// https://github.com/Xilinx/Vitis_Accel_Examples/blob/master/host/hbm_bandwidth/src/host.cpp
    /// </remarks>
    public struct XilinxMemoryExtension
    {
        public ulong Flags;
        public IntPtr Object;
        public IntPtr Parameters;

        public static XilinxMemoryExtension Create(IntPtr data, ulong bank) =>
            new XilinxMemoryExtension
            {
                Flags = bank | (ulong)MemoryFlag.ExtensionXilinxTopology,
                Object = data,
                Parameters = IntPtr.Zero
            };
    }
}
