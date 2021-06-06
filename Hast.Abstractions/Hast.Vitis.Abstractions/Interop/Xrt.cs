using System;
using Hast.Vitis.Abstractions.Interop.Enums.OpenCl;
using System.Diagnostics.CodeAnalysis;

namespace Hast.Vitis.Abstractions.Interop
{
    /// <summary>
    /// Xilinx-specific memory extensions. Control bank allocation of buffer object.
    /// </summary>
    /// <remarks>
    /// <para>Source: https://github.com/Xilinx/XRT/blob/master/src/include/1_2/CL/cl_ext_xilinx.h
    /// Legacy layout is used since that's the one in the examples
    /// <see href="https://github.com/Xilinx/Vitis_Accel_Examples/blob/master/host/hbm_bandwidth/src/host.cpp">here</see>.</para>
    /// </remarks>
    public struct XilinxMemoryExtension : IEquatable<XilinxMemoryExtension>
    {
        public ulong Flags { get; set; }

        [SuppressMessage(
            "Naming",
            "CA1720:Identifier contains type name",
            Justification = "Intentional. It refers to the \"obj\" field in the external structure.")]
        public IntPtr Object { get; set; }
        public IntPtr Parameters { get; set; }

        public static XilinxMemoryExtension Create(IntPtr data, ulong bank) =>
            new()
            {
                Flags = bank | (ulong)MemoryFlags.ExtensionXilinxTopology,
                Object = data,
                Parameters = IntPtr.Zero,
            };

        public override bool Equals(object obj) => obj is XilinxMemoryExtension extension && Equals(extension);

        public bool Equals(XilinxMemoryExtension other) =>
            Flags == other.Flags &&
            Object == other.Object &&
            Parameters == other.Parameters;

        public override int GetHashCode() => HashCode.Combine(Flags, Object, Parameters);

        public static bool operator ==(XilinxMemoryExtension left, XilinxMemoryExtension right) => left.Equals(right);
        public static bool operator !=(XilinxMemoryExtension left, XilinxMemoryExtension right) => !left.Equals(right);
    }
}
