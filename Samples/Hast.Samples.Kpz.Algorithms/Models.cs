using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Hast.Samples.Kpz.Algorithms;

/// <summary>
/// This struct is used to built the grid that the KPZ algorithm works on.
/// Every node on the grid stores the derivatives of a 2D function in X and Y direction.
/// </summary>
public class KpzNode
{
    public bool Dx; // Right
    public bool Dy; // Bottom

    public uint SerializeToUInt32() => (Dx ? 1U : 0) | (Dy ? 2U : 0);

    public static KpzNode DeserializeFromUInt32(uint value)
    {
        var node = new KpzNode
        {
            Dx = (value & 1) != 0,
            Dy = (value & 2) != 0,
        };
        return node;
    }
}

/// <summary>
/// This struct stores the right and bottom neighbours of a particular node on the grid.
/// It stores both their values and coordinates.
/// </summary>
[SuppressMessage(
    "Performance",
    "CA1815:Override equals and operator equals on value types",
    Justification = "The type's never checked for equality.")]
[SuppressMessage(
    "Major Code Smell",
    "S3898:Value types should implement \"IEquatable<T>\"",
    Justification = "Same.")]
public struct KpzNeighbours
{
    public KpzNode Nx; // Right neighbour value
    public KpzNode Ny; // Left neighbour value
    public KpzCoords NxCoords; // Right neighbour coordinates
    public KpzCoords NyCoords; // Left neighbour coordinates
}

/// <summary>
/// This struct stores the coordinates of a <see cref="KpzNode" /> on the grid.
/// </summary>
[SuppressMessage(
    "Performance",
    "CA1815:Override equals and operator equals on value types",
    Justification = "The type's never checked for equality.")]
[SuppressMessage(
    "Major Code Smell",
    "S3898:Value types should implement \"IEquatable<T>\"",
    Justification = "Same.")]
[StructLayout(LayoutKind.Auto)]
public struct KpzCoords
{
    public int X;
    public int Y;
}
