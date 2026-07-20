namespace RelayZero.Simulation
{
    public interface IFixedTickStep
    {
        // The host resolves this tick's inputs and consumes its event buffer before returning.
        void Step();
    }
}
