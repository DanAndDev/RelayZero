namespace RelayZero.Simulation
{
    public enum MatchPhase : byte
    {
        Countdown = 0,
        Regulation = 1,
        OvertimeReset = 2,
        Finalizing = 3,
        Results = 4,
    }
}
