using RelayZero.Foundation;

namespace RelayZero.Server
{
    public sealed class DedicatedServerCompositionRoot : ApplicationRootBase
    {
        private readonly DedicatedServerApplication application;

        public DedicatedServerCompositionRoot(
            BuildInfo buildInfo,
            DedicatedServerApplication application)
            : base(buildInfo)
        {
            this.application = application;
        }

        public static DedicatedServerCompositionRoot CreateDefault()
        {
            return new DedicatedServerCompositionRoot(
                GeneratedBuildInfo.ForRole(ApplicationRole.DedicatedServer),
                new DedicatedServerApplication());
        }

        protected override void OnStart()
        {
            application.Start();
        }

        protected override void OnStop()
        {
            application.Stop();
        }
    }
}
