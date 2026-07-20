using System;

namespace RelayZero.Foundation
{
    public readonly struct MatchId : IEquatable<MatchId>
    {
        public static readonly MatchId None = default;

        public MatchId(Guid value)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException("A match ID cannot be empty.", nameof(value));
            }

            Value = value;
        }

        public Guid Value { get; }

        public bool IsValid => Value != Guid.Empty;

        public static MatchId New()
        {
            return new MatchId(Guid.NewGuid());
        }

        public bool Equals(MatchId other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is MatchId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString("N");
        }

        public static bool operator ==(MatchId left, MatchId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MatchId left, MatchId right)
        {
            return !left.Equals(right);
        }
    }
}
