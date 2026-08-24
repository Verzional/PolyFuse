using System;
using UnityEngine;

namespace PolyFuse.Core
{
    [Serializable]
    public struct GridCoord : IEquatable<GridCoord>
    {
        public int r; // row
        public int c; // col

        public GridCoord(int row, int col)
        {
            this.r = row;
            this.c = col;
        }

        /// <summary>
        /// Parity rule: (r + c) is even -> Up triangle (▲), (r + c) is odd -> Down triangle (▼)
        /// </summary>
        public bool IsPointingUp => (((r + c) % 2) + 2) % 2 == 0;

        public static GridCoord operator +(GridCoord a, GridCoord b) => new GridCoord(a.r + b.r, a.c + b.c);
        public static GridCoord operator -(GridCoord a, GridCoord b) => new GridCoord(a.r - b.r, a.c - b.c);
        public static bool operator ==(GridCoord a, GridCoord b) => a.r == b.r && a.c == b.c;
        public static bool operator !=(GridCoord a, GridCoord b) => !(a == b);

        public bool Equals(GridCoord other) => r == other.r && c == other.c;
        public override bool Equals(object obj) => obj is GridCoord other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(r, c);
        public override string ToString() => $"({r}, {c}) [{(IsPointingUp ? "▲" : "▼")}]";
    }

    public enum ShapeCategory
    {
        Shard,   // 1 Unit
        Blade,   // 2 Units (Diamond)
        Cleaver, // 3 Units (Trapezoid / Strip)
        Core     // 6 Units (Hexagon)
    }
}
