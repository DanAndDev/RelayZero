using RelayZero.Foundation;

namespace RelayZero.Simulation
{
    public enum MatchResultOutcome : byte
    {
        None = 0,
        PlayerVictory = 1,
        Draw = 2,
        NoContest = 3,
    }

    public enum MatchResultReason : byte
    {
        None = 0,
        TargetScore = 1,
        RegulationTimer = 2,
        OvertimeCapture = 3,
        OvertimeDraw = 4,
        Forfeit = 5,
        FatalFailure = 6,
    }

    public readonly struct MatchResult
    {
        internal MatchResult(
            MatchResultOutcome outcome,
            MatchResultReason reason,
            PlayerSlot winner,
            SimulationTick finalizedTick,
            int playerZeroScoreMilliPoints,
            int playerOneScoreMilliPoints,
            uint regulationTicksRemaining)
        {
            Outcome = outcome;
            Reason = reason;
            HasWinner = outcome == MatchResultOutcome.PlayerVictory;
            Winner = winner;
            FinalizedTick = finalizedTick;
            PlayerZeroScoreMilliPoints = playerZeroScoreMilliPoints;
            PlayerOneScoreMilliPoints = playerOneScoreMilliPoints;
            RegulationTicksRemaining = regulationTicksRemaining;
        }

        public MatchResultOutcome Outcome { get; }

        public MatchResultReason Reason { get; }

        public bool HasWinner { get; }

        public PlayerSlot Winner { get; }

        public SimulationTick FinalizedTick { get; }

        public int PlayerZeroScoreMilliPoints { get; }

        public int PlayerOneScoreMilliPoints { get; }

        public uint RegulationTicksRemaining { get; }

        public bool IsFinal => Outcome != MatchResultOutcome.None;
    }
}
