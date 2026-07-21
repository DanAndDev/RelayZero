using NUnit.Framework;
using RelayZero.Editor.Simulation;
using RelayZero.Simulation;
using RelayZero.Simulation.Authoring;
using UnityEditor;

namespace RelayZero.Tests.EditMode.Simulation
{
    public sealed class SimulationAuthoringAssetTests
    {
        private const string MatchConfigPath = "Assets/RelayZero/Config/Simulation/MatchConfig.asset";

        [Test]
        public void CheckedInAuthoringAssetsCompileToGddMovementValues()
        {
            MatchConfigAsset asset = AssetDatabase.LoadAssetAtPath<MatchConfigAsset>(MatchConfigPath);

            Assert.That(asset, Is.Not.Null, "Generate the default simulation configuration before running tests.");
            Assert.That(asset.PlayerConfig, Is.Not.Null);
            Assert.That(asset.CoreConfig, Is.Not.Null);
            Assert.That(asset.RegulationConfig, Is.Not.Null);
            Assert.That(asset.SchemaVersion, Is.EqualTo(ConfigCompiler.CurrentSchemaVersion));
            MatchConfig runtime = asset.Compile();
            Assert.That(runtime.Player.NormalMaximumSpeedMetersPerSecond, Is.EqualTo(6.2f));
            Assert.That(runtime.Player.CarrierMaximumSpeedMetersPerSecond, Is.EqualTo(5.3f));
            Assert.That(runtime.Player.AccelerationMetersPerSecondSquared, Is.EqualTo(38f));
            Assert.That(runtime.Player.DecelerationMetersPerSecondSquared, Is.EqualTo(48f));
            Assert.That(runtime.Player.RadiusMeters, Is.EqualTo(0.45f));
            Assert.That(asset.PlayerConfig.TurnSpeedDegreesPerSecond, Is.EqualTo(720f));
            Assert.That(runtime.Core.InteractionRangeMeters, Is.EqualTo(1.1f));
            Assert.That(runtime.Core.RadiusMeters, Is.EqualTo(0.3f));
            Assert.That(runtime.Core.ManualDropSpeedMetersPerSecond, Is.EqualTo(4.5f));
            Assert.That(runtime.Core.ForcedDropPickupLockTicks, Is.EqualTo(21u));
            Assert.That(runtime.Core.ManualDropOpponentPickupLockTicks, Is.EqualTo(12u));
            Assert.That(runtime.Core.ManualDropPreviousOwnerPickupLockTicks, Is.EqualTo(45u));
            Assert.That(runtime.Regulation.CountdownDurationTicks, Is.EqualTo(180u));
            Assert.That(runtime.Regulation.RegulationDurationTicks, Is.EqualTo(10800u));
            Assert.That(runtime.Regulation.ScoreTargetMilliPoints, Is.EqualTo(100000));
            Assert.That(runtime.Regulation.BaseScoreMilliPointsPerSecond, Is.EqualTo(1000));
            Assert.That(runtime.Regulation.ActiveRelayScoreMilliPointsPerSecond, Is.EqualTo(2000));
            Assert.That(runtime.Regulation.PossessionGraceTicks, Is.EqualTo(30u));
            Assert.That(runtime.Regulation.FinalizingDurationTicks, Is.EqualTo(90u));
            Assert.That(runtime.Version.ToString(), Is.EqualTo("e29e6076766f6c920c23d1d5045edf85"));
        }

        [Test]
        public void ValidatorReportPrintsTheApprovedMovementAndTickConversions()
        {
            string report = SimulationConfigEditor.ValidateConfiguration();

            Assert.That(report, Does.Contain("Normal maximum: 6.2 m/s"));
            Assert.That(report, Does.Contain("Carrier maximum: 5.3 m/s"));
            Assert.That(report, Does.Contain("Dash: 0.16 s -> 10 ticks"));
            Assert.That(report, Does.Contain("Pulse anticipation: 0.18 s -> 11 ticks"));
            Assert.That(report, Does.Contain("Possession grace: 0.5 s -> 30 ticks"));
            Assert.That(report, Does.Contain("Countdown: 3 s -> 180 ticks"));
            Assert.That(report, Does.Contain("Regulation: 180 s -> 10800 ticks"));
            Assert.That(report, Does.Contain("Score target: 100 points = 100000 milli-points"));
            Assert.That(report, Does.Contain("Base scoring: 1000 milli-points/s"));
            Assert.That(report, Does.Contain("Active Relay scoring: 2000 milli-points/s total"));
            Assert.That(report, Does.Contain("Finalizing: 1.5 s -> 90 ticks"));
            Assert.That(report, Does.Contain("Forced-drop pickup lock: 0.35 s -> 21 ticks"));
            Assert.That(report, Does.Contain("Manual-drop opponent lock: 0.2 s -> 12 ticks"));
            Assert.That(report, Does.Contain("Manual-drop previous-owner lock: 0.75 s -> 45 ticks"));
            Assert.That(report, Does.Contain("Core invalid/unreachable: 3 s -> 180 ticks"));
            Assert.That(report, Does.Contain("Core resetting: 1.5 s -> 90 ticks"));
            Assert.That(report, Does.Contain("Core center lock: 0.5 s -> 30 ticks"));
            Assert.That(report, Does.Contain("Loose-core movement values:"));
            Assert.That(report, Does.Contain("Shock stun: 0.65 s -> 39 ticks"));
            Assert.That(report, Does.Contain("Terminal channel: 0.9 s -> 54 ticks"));
            Assert.That(report, Does.Contain("Power transition: 1 s -> 60 ticks"));
        }
    }
}
