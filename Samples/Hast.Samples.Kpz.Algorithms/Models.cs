using System.Diagnostics.CodeAnalysis;

namespace Hast.Samples.Kpz.Algorithms
{
    /// <summary>
    /// This struct is used to built the grid that the KPZ algorithm works on.
    /// Every node on the grid stores the derivatives of a 2D function in X and Y direction.
    /// </summary>
    public class KpzNode
    {
        public bool dx; // Right
        public bool dy; // Bottom


        public uint SerializeToUInt32() => ((dx) ? 1U : 0) | ((dy) ? 2U : 0);

        public static KpzNode DeserializeFromUInt32(uint value)
        {
            var node = new KpzNode
            {
                dx = (value & 1) != 0,
                dy = (value & 2) != 0
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
    public struct KpzNeighbours
    {
        public KpzNode nx; // Right neighbour value
        public KpzNode ny; // Left neighbour value
        public KpzCoords nxCoords; // Right neighbour coordinates
        public KpzCoords nyCoords; // Left neighbour coordinates
    }

    /// <summary>
    /// This struct stores the coordinates of a <see cref="KpzNode" /> on the grid.
    /// </summary>
    [SuppressMessage(
        "Performance",
        "CA1815:Override equals and operator equals on value types",
        Justification = "The type's never checked for equality.")]
    public struct KpzCoords
    {
        public int x;
        public int y;
    }
}
