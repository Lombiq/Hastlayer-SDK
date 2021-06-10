using System;
using System.Collections.Generic;

namespace Hast.Samples.Kpz.Algorithms
{
    /// <summary>
    /// This struct is used to built the grid that the KPZ algorithm works on.
    /// Every node on the grid stores the derivatives of a 2D function in X and Y direction.
    /// </summary>
    public class KpzNode
    {
        public bool Dx { get; set; } // Right
        public bool Dy { get; set; } // Bottom

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
    public struct KpzNeighbours : IEquatable<KpzNeighbours>
    {
        public KpzNode Nx { get; set; } // Right neighbour value
        public KpzNode Ny { get; set; } // Left neighbour value
        public KpzCoords NxCoords { get; set; } // Right neighbour coordinates
        public KpzCoords NyCoords { get; set; } // Left neighbour coordinates

        public override bool Equals(object obj) => obj is KpzNeighbours neighbours && Equals(neighbours);

        public bool Equals(KpzNeighbours other) =>
            EqualityComparer<KpzNode>.Default.Equals(Nx, other.Nx)
            && EqualityComparer<KpzNode>.Default.Equals(Ny, other.Ny)
            && EqualityComparer<KpzCoords>.Default.Equals(NxCoords, other.NxCoords)
            && EqualityComparer<KpzCoords>.Default.Equals(NyCoords, other.NyCoords);

        public override int GetHashCode() => HashCode.Combine(Nx, Ny, NxCoords, NyCoords);

        public static bool operator ==(KpzNeighbours left, KpzNeighbours right) => left.Equals(right);
        public static bool operator !=(KpzNeighbours left, KpzNeighbours right) => !left.Equals(right);
    }

    /// <summary>
    /// This struct stores the coordinates of a <see cref="KpzNode" /> on the grid.
    /// </summary>
    public struct KpzCoords : IEquatable<KpzCoords>
    {
        public int X { get; set; }
        public int Y { get; set; }

        public override bool Equals(object obj) => obj is KpzCoords other && Equals(other);

        public bool Equals(KpzCoords other) => X == other.X && Y == other.Y;

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public static bool operator ==(KpzCoords left, KpzCoords right) => left.Equals(right);

        public static bool operator !=(KpzCoords left, KpzCoords right) => !left.Equals(right);
    }
}
