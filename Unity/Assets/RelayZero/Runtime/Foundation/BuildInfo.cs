using System;

namespace RelayZero.Foundation
{
    public readonly struct BuildInfo : IEquatable<BuildInfo>
    {
        public BuildInfo(
            string version,
            string commit,
            ApplicationRole role,
            string protocolVersion,
            string configurationVersion,
            bool isDirty)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                throw new ArgumentException("Build version is required.", nameof(version));
            }

            if (string.IsNullOrWhiteSpace(commit))
            {
                throw new ArgumentException("Build commit is required.", nameof(commit));
            }

            if (string.IsNullOrWhiteSpace(protocolVersion))
            {
                throw new ArgumentException("Protocol version is required.", nameof(protocolVersion));
            }

            if (string.IsNullOrWhiteSpace(configurationVersion))
            {
                throw new ArgumentException("Configuration version is required.", nameof(configurationVersion));
            }

            Version = version;
            Commit = commit;
            Role = role;
            ProtocolVersion = protocolVersion;
            ConfigurationVersion = configurationVersion;
            IsDirty = isDirty;
        }

        public string Version { get; }

        public string Commit { get; }

        public ApplicationRole Role { get; }

        public string ProtocolVersion { get; }

        public string ConfigurationVersion { get; }

        public bool IsDirty { get; }

        public string ToDisplayString()
        {
            string dirtySuffix = IsDirty ? "+dirty" : string.Empty;
            return $"{Role} {Version} ({Commit}{dirtySuffix}) protocol={ProtocolVersion} config={ConfigurationVersion}";
        }

        public bool Equals(BuildInfo other)
        {
            return Version == other.Version &&
                Commit == other.Commit &&
                Role == other.Role &&
                ProtocolVersion == other.ProtocolVersion &&
                ConfigurationVersion == other.ConfigurationVersion &&
                IsDirty == other.IsDirty;
        }

        public override bool Equals(object obj)
        {
            return obj is BuildInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Version.GetHashCode();
                hashCode = (hashCode * 397) ^ Commit.GetHashCode();
                hashCode = (hashCode * 397) ^ Role.GetHashCode();
                hashCode = (hashCode * 397) ^ ProtocolVersion.GetHashCode();
                hashCode = (hashCode * 397) ^ ConfigurationVersion.GetHashCode();
                hashCode = (hashCode * 397) ^ IsDirty.GetHashCode();
                return hashCode;
            }
        }
    }
}
