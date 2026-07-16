using Microsoft.AspNetCore.Builder;
using RelayZero.ControlPlane.Api;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
ControlPlaneBuildInfo buildInfo = ControlPlaneBuildIdentity.Current;

app.MapGet("/", () => Results.Ok(new ControlPlaneShellResponse(
    "Relay Zero Control Plane",
    "development-shell",
    buildInfo)));

app.MapGet("/health/live", () => Results.Ok(new HealthResponse(
    "ok",
    "live",
    app.Environment.EnvironmentName,
    buildInfo)));

app.MapGet("/health/ready", () => Results.Ok(new ReadinessResponse(
    "ok",
    "ready",
    app.Environment.EnvironmentName,
    buildInfo,
    ControlPlaneReadinessChecks.DevelopmentShell)));

app.MapGet("/healthz", () => Results.Ok(new HealthResponse(
    "ok",
    "compatibility",
    app.Environment.EnvironmentName,
    buildInfo)));

app.Run();
