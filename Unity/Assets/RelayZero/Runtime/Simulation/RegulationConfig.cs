using RelayZero.Foundation;

namespace RelayZero.Simulation
{
    public readonly struct RegulationConfig
    {
        public const int MilliPointsPerPoint = 1000;

        internal RegulationConfig(in RegulationConfigValues values)
        {
            CountdownDurationTicks = SimulationTime.SecondsToTicksCeiling(values.CountdownSeconds);
            RegulationDurationTicks = SimulationTime.SecondsToTicksCeiling(values.RegulationSeconds);
            ScoreTargetMilliPoints = checked(values.ScoreTargetPoints * MilliPointsPerPoint);
            BaseScoreMilliPointsPerSecond = values.BaseScoreMilliPointsPerSecond;
            ActiveRelayScoreMilliPointsPerSecond = values.ActiveRelayScoreMilliPointsPerSecond;
            PossessionGraceTicks = SimulationTime.SecondsToTicksCeiling(values.PossessionGraceSeconds);
            FinalizingDurationTicks = SimulationTime.SecondsToTicksCeiling(values.FinalizingSeconds);
        }

        public uint CountdownDurationTicks { get; }

        public uint RegulationDurationTicks { get; }

        public int ScoreTargetMilliPoints { get; }

        public int ScoreTargetPoints => ScoreTargetMilliPoints / MilliPointsPerPoint;

        public int BaseScoreMilliPointsPerSecond { get; }

        public int ActiveRelayScoreMilliPointsPerSecond { get; }

        public uint PossessionGraceTicks { get; }

        public uint FinalizingDurationTicks { get; }
    }
}
