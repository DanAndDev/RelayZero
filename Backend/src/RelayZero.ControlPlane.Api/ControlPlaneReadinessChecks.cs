namespace RelayZero.ControlPlane.Api;

internal static class ControlPlaneReadinessChecks
{
    public static readonly string[] DevelopmentShell =
    [
        "configuration:development-placeholder",
        "store:not-required-yet",
        "allocator:not-required-yet",
        "signing-key:not-required-yet",
        "background-workers:not-required-yet",
    ];
}
