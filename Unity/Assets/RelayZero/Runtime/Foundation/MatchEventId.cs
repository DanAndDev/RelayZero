using System;

namespace RelayZero.Foundation
{
    public readonly struct MatchEventId : IEquatable<MatchEventId>
    {
        public MatchEventId(SimulationTick tick, byte index)
        {
            Tick = tick;
            Index = index;
        }

        public SimulationTick Tick { get; }

        public byte Index { get; }

        public bool Equals(MatchEventId other)
        {
            return Tick == other.Tick && Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            return obj is MatchEventId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Tick.GetHashCode() * 397) ^ Index;
            }
        }

        public override string ToString()
        {
            return Tick + ":" + Index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public static bool operator ==(MatchEventId left, MatchEventId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MatchEventId left, MatchEventId right)
        {
            return !left.Equals(right);
        }
    }
}
