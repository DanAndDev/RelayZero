using System;

namespace RelayZero.Foundation
{
    public readonly struct PlayerSlot : IEquatable<PlayerSlot>, IComparable<PlayerSlot>
    {
        public static readonly PlayerSlot Zero = new PlayerSlot(0);
        public static readonly PlayerSlot One = new PlayerSlot(1);

        public PlayerSlot(byte value)
        {
            if (value > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Player slot must be 0 or 1.");
            }

            Value = value;
        }

        public byte Value { get; }

        public int Index => Value;

        public PlayerSlot Other => Value == 0 ? One : Zero;

        public int CompareTo(PlayerSlot other)
        {
            return Value.CompareTo(other.Value);
        }

        public bool Equals(PlayerSlot other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerSlot other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public static bool operator ==(PlayerSlot left, PlayerSlot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PlayerSlot left, PlayerSlot right)
        {
            return !left.Equals(right);
        }
    }
}
