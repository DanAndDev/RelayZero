namespace RelayZero.ControlPlane.Api;

internal sealed record HealthResponse(
    string Status,
    string Kind,
    string Environment,
    ControlPlaneBuildInfo Build);
