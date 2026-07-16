namespace RelayZero.ControlPlane.Api;

internal sealed record ReadinessResponse(
    string Status,
    string Kind,
    string Environment,
    ControlPlaneBuildInfo Build,
    string[] Checks);
