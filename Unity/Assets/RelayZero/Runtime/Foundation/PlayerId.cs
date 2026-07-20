using System;

namespace RelayZero.Foundation
{
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public static readonly PlayerId None = default;

        public PlayerId(Guid value)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException("A player ID cannot be empty.", nameof(value));
            }

            Value = value;
        }

        public Guid Value { get; }

        public bool IsValid => Value != Guid.Empty;

        public bool Equals(PlayerId other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString("N");
        }

        public static bool operator ==(PlayerId left, PlayerId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PlayerId left, PlayerId right)
        {
            return !left.Equals(right);
        }
    }
}
