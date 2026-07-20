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
            Assert.That(asset.SchemaVersion, Is.EqualTo(ConfigCompiler.CurrentSchemaVersion));
            MatchConfig runtime = asset.Compile();
            Assert.That(runtime.Player.NormalMaximumSpeedMetersPerSecond, Is.EqualTo(6.2f));
            Assert.That(runtime.Player.CarrierMaximumSpeedMetersPerSecond, Is.EqualTo(5.3f));
            Assert.That(runtime.Player.AccelerationMetersPerSecondSquared, Is.EqualTo(38f));
            Assert.That(runtime.Player.DecelerationMetersPerSecondSquared, Is.EqualTo(48f));
            Assert.That(runtime.Player.RadiusMeters, Is.EqualTo(0.45f));
            Assert.That(asset.PlayerConfig.TurnSpeedDegreesPerSecond, Is.EqualTo(720f));
            Assert.That(runtime.Version.ToString(), Is.EqualTo("b438225632213b13d31dd686b0cd0578"));
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
            Assert.That(report, Does.Contain("Shock stun: 0.65 s -> 39 ticks"));
            Assert.That(report, Does.Contain("Terminal channel: 0.9 s -> 54 ticks"));
            Assert.That(report, Does.Contain("Power transition: 1 s -> 60 ticks"));
        }
    }
}
