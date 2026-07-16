using RelayZero.Foundation;

namespace RelayZero.Client.Application
{
    public sealed class ClientCompositionRoot : ApplicationRootBase
    {
        private readonly ClientApplication application;

        public ClientCompositionRoot(
            BuildInfo buildInfo,
            ClientApplication application)
            : base(buildInfo)
        {
            this.application = application;
        }

        public static ClientCompositionRoot CreateDefault()
        {
            return new ClientCompositionRoot(
                GeneratedBuildInfo.ForRole(ApplicationRole.Client),
                new ClientApplication());
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
