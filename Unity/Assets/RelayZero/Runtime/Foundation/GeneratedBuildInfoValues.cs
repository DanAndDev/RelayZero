namespace RelayZero.Foundation
{
    public readonly struct GeneratedBuildInfoValues
    {
        public GeneratedBuildInfoValues(
            string version,
            string commit,
            string protocolVersion,
            string configurationVersion,
            bool isDirty)
        {
            Version = version;
            Commit = commit;
            ProtocolVersion = protocolVersion;
            ConfigurationVersion = configurationVersion;
            IsDirty = isDirty;
        }

        public static GeneratedBuildInfoValues Fallback
        {
            get
            {
                return new GeneratedBuildInfoValues(
                    "0.1.0",
                    "unknown",
                    "protocol-placeholder",
                    "configuration-placeholder",
                    true);
            }
        }

        public string Version { get; }

        public string Commit { get; }

        public string ProtocolVersion { get; }

        public string ConfigurationVersion { get; }

        public bool IsDirty { get; }
    }
}
