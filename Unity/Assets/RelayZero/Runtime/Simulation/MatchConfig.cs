using RelayZero.Foundation;

namespace RelayZero.Simulation
{
    public sealed class MatchConfig
    {
        internal MatchConfig(
            int schemaVersion,
            PlayerMovementConfig player,
            CoreConfig core,
            RegulationConfig regulation,
            ConfigVersion version)
        {
            SchemaVersion = schemaVersion;
            Player = player;
            Core = core;
            Regulation = regulation;
            Version = version;
        }

        public int SchemaVersion { get; }

        public int TickRate => SimulationTime.TicksPerSecond;

        public double TickDurationSeconds => SimulationTime.TickDurationSeconds;

        public PlayerMovementConfig Player { get; }

        public CoreConfig Core { get; }

        public RegulationConfig Regulation { get; }

        public ConfigVersion Version { get; }
    }
}
