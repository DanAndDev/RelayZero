namespace RelayZero.Simulation
{
    public enum MatchEventType : byte
    {
        None = 0,
        PlayerStateChanged = 1,
        CorePickedUp = 2,
        CoreDropped = 3,
        CoreResetStarted = 4,
        CoreResetCompleted = 5,
        ScoreChanged = 6,
        MatchPhaseChanged = 7,
        MatchFinalized = 8,
        RegulationTieDetected = 9,
    }
}
