using System;

namespace RelayZero.Foundation
{
    public abstract class ApplicationRootBase : IApplicationRoot
    {
        protected ApplicationRootBase(BuildInfo buildInfo)
        {
            BuildInfo = buildInfo;
        }

        public BuildInfo BuildInfo { get; }

        public bool IsRunning { get; private set; }

        public void Start()
        {
            if (IsRunning)
            {
                throw new InvalidOperationException($"{GetType().Name} is already running.");
            }

            OnStart();
            IsRunning = true;
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            OnStop();
            IsRunning = false;
        }

        protected virtual void OnStart()
        {
        }

        protected virtual void OnStop()
        {
        }
    }
}
