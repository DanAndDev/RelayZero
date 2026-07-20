using System;
using NUnit.Framework;
using RelayZero.Foundation;
using RelayZero.Simulation;
using Unity.Mathematics;

namespace RelayZero.Tests.EditMode.Simulation
{
    public sealed class ConfigurationCompilationTests
    {
        [TestCase(0.0d, 0u)]
        [TestCase(0.16d, 10u)]
        [TestCase(0.18d, 11u)]
        [TestCase(0.5d, 30u)]
        [TestCase(0.65d, 39u)]
        [TestCase(0.9d, 54u)]
        [TestCase(1.0d, 60u)]
        [TestCase(4.15d, 249u)]
        public void DurationConversionUsesCeiling(double seconds, uint expectedTicks)
        {
            Assert.That(SimulationTime.SecondsToTicksCeiling(seconds), Is.EqualTo(expectedTicks));
        }

        [Test]
        public void DurationMateriallyAboveAnExactTickStillUsesTheNextTick()
        {
            Assert.That(SimulationTime.SecondsToTicksCeiling(4.150001d), Is.EqualTo(250u));
        }

        [Test]
        public void AnyPositiveAuthoredDurationOccupiesAtLeastOneTick()
        {
            Assert.That(SimulationTime.SecondsToTicksCeiling(float.Epsilon), Is.EqualTo(1u));
            Assert.That(SimulationTime.SecondsToTicksCeiling(double.Epsilon), Is.EqualTo(1u));
        }

        [TestCase(0.1f, 6u)]
        [TestCase(0.2f, 12u)]
        [TestCase(0.3f, 18u)]
        [TestCase(0.8f, 48u)]
        [TestCase(0.16f, 10u)]
        [TestCase(0.18f, 11u)]
        [TestCase(0.65f, 39u)]
        public void FloatAuthoredDurationsDoNotGainTicksFromBinaryRepresentation(float seconds, uint expectedTicks)
        {
            Assert.That(SimulationTime.SecondsToTicksCeiling(seconds), Is.EqualTo(expectedTicks));
        }

        [Test]
        public void DurationConversionRejectsInvalidValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SimulationTime.SecondsToTicksCeiling(-0.01d));
            Assert.Throws<ArgumentOutOfRangeException>(() => SimulationTime.SecondsToTicksCeiling(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => SimulationTime.SecondsToTicksCeiling(double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => SimulationTime.SecondsToTicksCeiling(double.NegativeInfinity));
        }

        [Test]
        public void GddDefaultsCompileIntoImmutableRuntimeValues()
        {
            PlayerConfigValues values = PlayerConfigValues.GddDefaults;
            MatchConfig config = ConfigCompiler.Compile(in values);

            Assert.That(config.TickRate, Is.EqualTo(60));
            Assert.That(config.Player.NormalMaximumSpeedMetersPerSecond, Is.EqualTo(6.2f));
            Assert.That(config.Player.CarrierMaximumSpeedMetersPerSecond, Is.EqualTo(5.3f));
            Assert.That(config.Player.AccelerationMetersPerSecondSquared, Is.EqualTo(38f));
            Assert.That(config.Player.DecelerationMetersPerSecondSquared, Is.EqualTo(48f));
            Assert.That(config.Player.RadiusMeters, Is.EqualTo(0.45f));
            Assert.That(config.Player.TurnSpeedRadiansPerSecond, Is.EqualTo(math.radians(720f)).Within(0.00001f));
            Assert.That(config.Player.NormalMaximumSpeedSquared, Is.EqualTo(6.2f * 6.2f));
            Assert.That(config.Player.RadiusSquared, Is.EqualTo(0.45f * 0.45f));
            Assert.That(config.Version.IsValid, Is.True);
            Assert.That(config.Version.ToString(), Is.EqualTo("b438225632213b13d31dd686b0cd0578"));
        }

        [Test]
        public void CanonicalConfigVersionIsStableAndSensitiveToGameplayValues()
        {
            PlayerConfigValues defaults = PlayerConfigValues.GddDefaults;
            MatchConfig first = ConfigCompiler.Compile(in defaults);
            MatchConfig second = ConfigCompiler.Compile(in defaults);
            PlayerConfigValues changed = new PlayerConfigValues(6.21f, 5.3f, 38f, 48f, 0.45f, 720f);
            MatchConfig changedConfig = ConfigCompiler.Compile(in changed);

            Assert.That(second.Version, Is.EqualTo(first.Version));
            Assert.That(changedConfig.Version, Is.Not.EqualTo(first.Version));
        }

        [Test]
        public void ConfigCompilerRejectsNonFiniteAndCrossFieldViolations()
        {
            PlayerConfigValues nanSpeed = new PlayerConfigValues(float.NaN, 5.3f, 38f, 48f, 0.45f, 720f);
            PlayerConfigValues infiniteAcceleration = new PlayerConfigValues(6.2f, 5.3f, float.PositiveInfinity, 48f, 0.45f, 720f);
            PlayerConfigValues negativeRadius = new PlayerConfigValues(6.2f, 5.3f, 38f, 48f, -0.45f, 720f);
            PlayerConfigValues fasterCarrier = new PlayerConfigValues(6.2f, 6.3f, 38f, 48f, 0.45f, 720f);
            PlayerConfigValues extremeSpeed = new PlayerConfigValues(float.MaxValue, 5.3f, 38f, 48f, 0.45f, 720f);
            PlayerConfigValues subnormalRadius = new PlayerConfigValues(6.2f, 5.3f, 38f, 48f, float.Epsilon, 720f);

            Assert.Throws<ConfigValidationException>(() => ConfigCompiler.Compile(in nanSpeed));
            Assert.Throws<ConfigValidationException>(() => ConfigCompiler.Compile(in infiniteAcceleration));
            Assert.Throws<ConfigValidationException>(() => ConfigCompiler.Compile(in negativeRadius));
            Assert.Throws<ConfigValidationException>(() => ConfigCompiler.Compile(in fasterCarrier));
            Assert.Throws<ConfigValidationException>(() => ConfigCompiler.Compile(in extremeSpeed));
            Assert.Throws<ConfigValidationException>(() => ConfigCompiler.Compile(in subnormalRadius));
        }
    }
}
