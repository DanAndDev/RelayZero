using System;

namespace RelayZero.Foundation
{
    public readonly struct CommandSequence : IEquatable<CommandSequence>
    {
        public static readonly CommandSequence Zero = default;

        public CommandSequence(uint value)
        {
            Value = value;
        }

        public uint Value { get; }

        public CommandSequence Next()
        {
            return new CommandSequence(unchecked(Value + 1u));
        }

        public bool IsNewerThan(CommandSequence other)
        {
            return unchecked((int)(Value - other.Value)) > 0;
        }

        public bool IsNewerThanOrEqualTo(CommandSequence other)
        {
            return Value == other.Value || IsNewerThan(other);
        }

        public uint DistanceSince(CommandSequence older)
        {
            return unchecked(Value - older.Value);
        }

        public bool Equals(CommandSequence other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is CommandSequence other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked((int)Value);
        }

        public override string ToString()
        {
            return Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public static bool operator ==(CommandSequence left, CommandSequence right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CommandSequence left, CommandSequence right)
        {
            return !left.Equals(right);
        }
    }
}
