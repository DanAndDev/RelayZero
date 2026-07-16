using RelayZero.Foundation;

namespace RelayZero.Bootstrap
{
    public sealed class TestCompositionRoot : ApplicationRootBase
    {
        public TestCompositionRoot(BuildInfo buildInfo)
            : base(buildInfo)
        {
        }

        public static TestCompositionRoot CreateDefault()
        {
            return new TestCompositionRoot(GeneratedBuildInfo.ForRole(ApplicationRole.Test));
        }
    }
}
