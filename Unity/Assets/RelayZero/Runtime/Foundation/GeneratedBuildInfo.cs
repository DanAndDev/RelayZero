namespace RelayZero.Foundation
{
    public static partial class GeneratedBuildInfo
    {
        public static BuildInfo ForRole(ApplicationRole role)
        {
            GeneratedBuildInfoValues values = GeneratedBuildInfoValues.Fallback;
            Fill(ref values);

            return new BuildInfo(
                values.Version,
                values.Commit,
                role,
                values.ProtocolVersion,
                values.ConfigurationVersion,
                values.IsDirty);
        }

        static partial void Fill(ref GeneratedBuildInfoValues values);
    }
}
