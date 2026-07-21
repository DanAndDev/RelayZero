namespace RelayZero.Simulation
{
    public enum CoreDropReason : byte
    {
        None = 0,
        Manual = 1,
        Pulse = 2,
        ShockGate = 3,
        Disconnect = 4,
        Recovery = 5,
    }
}
