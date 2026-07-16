namespace RelayZero.Client.Application
{
    public sealed class ClientApplication
    {
        public bool IsRunning { get; private set; }

        public void Start()
        {
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }
    }
}
