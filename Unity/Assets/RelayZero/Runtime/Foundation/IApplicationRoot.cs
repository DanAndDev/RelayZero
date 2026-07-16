namespace RelayZero.Foundation
{
    public interface IApplicationRoot
    {
        BuildInfo BuildInfo { get; }

        bool IsRunning { get; }

        void Start();

        void Stop();
    }
}
