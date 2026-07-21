using RelayZero.Foundation;

namespace RelayZero.Simulation
{
    public struct MatchClockState
    {
        internal MatchClockState(MatchStartMode startMode, in RegulationConfig config)
        {
            Phase = startMode == MatchStartMode.Countdown
                ? MatchPhase.Countdown
                : MatchPhase.Regulation;
            PhaseStartTick = SimulationTick.Zero;
            PhaseEndTick = startMode == MatchStartMode.Countdown
                ? new SimulationTick(config.CountdownDurationTicks)
                : SimulationTick.Zero;
            RegulationTicksRemaining = config.RegulationDurationTicks;
            HasResult = false;
            Result = default;
        }

        public MatchPhase Phase { get; internal set; }

        public SimulationTick PhaseStartTick { get; internal set; }

        public SimulationTick PhaseEndTick { get; internal set; }

        public uint RegulationTicksRemaining { get; internal set; }

        public bool HasResult { get; internal set; }

        public MatchResult Result { get; internal set; }

        public uint GetCountdownTicksRemaining(SimulationTick currentTick)
        {
            return Phase == MatchPhase.Countdown && PhaseEndTick.IsNewerThan(currentTick)
                ? PhaseEndTick.DistanceSince(currentTick)
                : 0u;
        }
    }
}
