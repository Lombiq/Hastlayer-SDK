using Hast.Vitis.Abstractions.Interop.Enums.OpenCl;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Hast.Vitis.Abstractions.Interop;

/// <summary>
/// Xilinx-specific memory extensions. Control bank allocation of buffer object.
/// </summary>
/// <remarks>
/// <para>Source: <see href="https://github.com/Xilinx/XRT/blob/master/src/include/1_2/CL/cl_ext_xilinx.h"/>.</para>
/// <para>
/// Legacy layout is used since that's the one in the examples <see
/// href="https://github.com/Xilinx/Vitis_Accel_Examples/blob/master/host/hbm_bandwidth/src/host.cpp">here</see>.
/// </para>
/// </remarks>
// LayoutKind can't be Auto because this struct is exposed to unmanaged code.
[StructLayout(LayoutKind.Sequential)]
public struct XilinxMemoryExtension : IEquatable<XilinxMemoryExtension>
{
    public ulong Flags;

    [SuppressMessage(
        "Naming",
        "CA1720:Identifier contains type name",
        Justification = "Name comes from an external Xilinx definition.")]
    public IntPtr Object;

    public IntPtr Parameters;

    public static XilinxMemoryExtension Create(IntPtr data, ulong bank) =>
        new()
        {
            Flags = bank | (ulong)MemoryFlags.ExtensionXilinxTopology,
            Object = data,
            Parameters = IntPtr.Zero,
        };

    public bool Equals(XilinxMemoryExtension other) =>
        Flags == other.Flags && Object.Equals(other.Object) && Parameters.Equals(other.Parameters);

    public override bool Equals(object obj) => obj is XilinxMemoryExtension other && Equals(other);

    public override int GetHashCode() => base.GetHashCode();

    public static bool operator ==(XilinxMemoryExtension left, XilinxMemoryExtension right) => left.Equals(right);

    public static bool operator !=(XilinxMemoryExtension left, XilinxMemoryExtension right) => !left.Equals(right);
}
