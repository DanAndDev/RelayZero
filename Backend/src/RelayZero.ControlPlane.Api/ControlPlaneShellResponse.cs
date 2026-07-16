namespace RelayZero.ControlPlane.Api;

internal sealed record ControlPlaneShellResponse(
    string Name,
    string Status,
    ControlPlaneBuildInfo Build);
