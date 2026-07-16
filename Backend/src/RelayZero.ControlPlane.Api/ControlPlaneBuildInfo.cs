namespace RelayZero.ControlPlane.Api;

internal sealed record ControlPlaneBuildInfo(
    string Role,
    string Version,
    string Commit,
    string ProtocolVersion,
    string ConfigurationVersion,
    bool IsDirty);
