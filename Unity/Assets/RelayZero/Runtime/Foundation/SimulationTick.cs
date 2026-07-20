using System;

namespace RelayZero.Foundation
{
    public readonly struct SimulationTick : IEquatable<SimulationTick>
    {
        public static readonly SimulationTick Zero = default;

        public SimulationTick(uint value)
        {
            Value = value;
        }

        public uint Value { get; }

        public SimulationTick Next()
        {
            return new SimulationTick(unchecked(Value + 1u));
        }

        public bool IsNewerThan(SimulationTick other)
        {
            return unchecked((int)(Value - other.Value)) > 0;
        }

        public bool IsNewerThanOrEqualTo(SimulationTick other)
        {
            return Value == other.Value || IsNewerThan(other);
        }

        public uint DistanceSince(SimulationTick older)
        {
            return unchecked(Value - older.Value);
        }

        public bool Equals(SimulationTick other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is SimulationTick other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked((int)Value);
        }

        public override string ToString()
        {
            return Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public static SimulationTick operator +(SimulationTick tick, uint amount)
        {
            return new SimulationTick(unchecked(tick.Value + amount));
        }

        public static bool operator ==(SimulationTick left, SimulationTick right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SimulationTick left, SimulationTick right)
        {
            return !left.Equals(right);
        }
    }
}
