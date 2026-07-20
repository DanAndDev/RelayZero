using RelayZero.Foundation;

namespace RelayZero.Simulation
{
    public sealed class MatchConfig
    {
        internal MatchConfig(int schemaVersion, PlayerMovementConfig player, ConfigVersion version)
        {
            SchemaVersion = schemaVersion;
            Player = player;
            Version = version;
        }

        public int SchemaVersion { get; }

        public int TickRate => SimulationTime.TicksPerSecond;

        public double TickDurationSeconds => SimulationTime.TickDurationSeconds;

        public PlayerMovementConfig Player { get; }

        public ConfigVersion Version { get; }
    }
}
