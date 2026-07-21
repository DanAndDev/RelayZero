namespace RelayZero.Simulation
{
    public enum PlayerLocomotionMode : byte
    {
        Normal = 0,
        Dashing = 1,
        Knockback = 2,
        Stunned = 3,
    }

    public enum PlayerActionMode : byte
    {
        None = 0,
        PulseWindup = 1,
        TerminalChannel = 2,
    }

    public enum PlayerConnectionMode : byte
    {
        Connected = 0,
        Interrupted = 1,
        Forfeited = 2,
    }
}
