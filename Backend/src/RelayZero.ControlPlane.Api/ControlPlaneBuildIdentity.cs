using System.Reflection;

namespace RelayZero.ControlPlane.Api;

internal static class ControlPlaneBuildIdentity
{
    public static ControlPlaneBuildInfo Current
    {
        get
        {
            Assembly assembly = typeof(ControlPlaneBuildIdentity).Assembly;
            AssemblyInformationalVersionAttribute? informationalVersion =
                assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            return new ControlPlaneBuildInfo(
                "ControlPlane",
                informationalVersion?.InformationalVersion ?? "0.1.0",
                Environment.GetEnvironmentVariable("RELAYZERO_BUILD_COMMIT") ?? "unknown",
                "protocol-placeholder",
                "configuration-placeholder",
                string.Equals(
                    Environment.GetEnvironmentVariable("RELAYZERO_BUILD_DIRTY"),
                    "true",
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}
