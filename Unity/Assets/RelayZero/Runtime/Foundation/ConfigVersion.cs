using System;
using System.Globalization;

namespace RelayZero.Foundation
{
    public readonly struct ConfigVersion : IEquatable<ConfigVersion>
    {
        public static readonly ConfigVersion None = default;

        public ConfigVersion(ulong upper, ulong lower)
        {
            Upper = upper;
            Lower = lower;
        }

        public ulong Upper { get; }

        public ulong Lower { get; }

        public bool IsValid => Upper != 0ul || Lower != 0ul;

        public static ConfigVersion FromBytes(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 16)
            {
                throw new ArgumentException("A configuration version requires at least 16 bytes.", nameof(bytes));
            }

            ulong upper = ReadUInt64BigEndian(bytes.Slice(0, 8));
            ulong lower = ReadUInt64BigEndian(bytes.Slice(8, 8));
            return new ConfigVersion(upper, lower);
        }

        public bool Equals(ConfigVersion other)
        {
            return Upper == other.Upper && Lower == other.Lower;
        }

        public override bool Equals(object obj)
        {
            return obj is ConfigVersion other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Upper.GetHashCode() * 397) ^ Lower.GetHashCode();
            }
        }

        public override string ToString()
        {
            return Upper.ToString("x16", CultureInfo.InvariantCulture) +
                   Lower.ToString("x16", CultureInfo.InvariantCulture);
        }

        public static bool operator ==(ConfigVersion left, ConfigVersion right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ConfigVersion left, ConfigVersion right)
        {
            return !left.Equals(right);
        }

        private static ulong ReadUInt64BigEndian(ReadOnlySpan<byte> bytes)
        {
            ulong result = 0ul;
            for (int index = 0; index < 8; index++)
            {
                result = (result << 8) | bytes[index];
            }

            return result;
        }
    }
}
