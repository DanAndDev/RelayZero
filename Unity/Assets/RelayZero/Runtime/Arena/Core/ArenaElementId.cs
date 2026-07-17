using System;

namespace RelayZero.Arena
{
    public readonly struct ArenaElementId : IEquatable<ArenaElementId>, IComparable<ArenaElementId>
    {
        public static readonly ArenaElementId None = new ArenaElementId(0);

        public ArenaElementId(ushort value)
        {
            Value = value;
        }

        public ushort Value { get; }

        public bool IsValid
        {
            get { return Value != 0; }
        }

        public static ArenaElementId FromStableId(string stableId)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                throw new ArgumentException("Stable ID cannot be empty.", nameof(stableId));
            }

            uint hash = 2166136261u;
            string canonical = stableId.Trim();
            for (int i = 0; i < canonical.Length; i++)
            {
                char character = canonical[i];
                hash ^= (byte)(character & 0xff);
                hash *= 16777619u;
                hash ^= (byte)(character >> 8);
                hash *= 16777619u;
            }

            ushort folded = (ushort)((hash >> 16) ^ (hash & 0xffff));
            return new ArenaElementId(folded == 0 ? (ushort)1 : folded);
        }

        public int CompareTo(ArenaElementId other)
        {
            return Value.CompareTo(other.Value);
        }

        public bool Equals(ArenaElementId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ArenaElementId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public static bool operator ==(ArenaElementId left, ArenaElementId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ArenaElementId left, ArenaElementId right)
        {
            return !left.Equals(right);
        }
    }
}
