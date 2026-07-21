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

            Assert.That(config.SchemaVersion, Is.EqualTo(3));
            Assert.That(config.TickRate, Is.EqualTo(60));
            Assert.That(config.Player.NormalMaximumSpeedMetersPerSecond, Is.EqualTo(6.2f));
            Assert.That(config.Player.CarrierMaximumSpeedMetersPerSecond, Is.EqualTo(5.3f));
            Assert.That(config.Player.AccelerationMetersPerSecondSquared, Is.EqualTo(38f));
            Assert.That(config.Player.DecelerationMetersPerSecondSquared, Is.EqualTo(48f));
            Assert.That(config.Player.RadiusMeters, Is.EqualTo(0.45f));
            Assert.That(config.Player.TurnSpeedRadiansPerSecond, Is.EqualTo(math.radians(720f)).Within(0.00001f));
            Assert.That(config.Player.NormalMaximumSpeedSquared, Is.EqualTo(6.2f * 6.2f));
            Assert.That(config.Player.RadiusSquared, Is.EqualTo(0.45f * 0.45f));
            Assert.That(config.Core.InteractionRangeMeters, Is.EqualTo(1.1f));
            Assert.That(config.Core.PickupDistanceTieThresholdMeters, Is.EqualTo(0.05f));
            Assert.That(config.Core.RadiusMeters, Is.EqualTo(0.30f));
            Assert.That(config.Core.ManualDropSpeedMetersPerSecond, Is.EqualTo(4.5f));
            Assert.That(config.Core.ForcedDropPickupLockTicks, Is.EqualTo(21u));
            Assert.That(config.Core.ManualDropOpponentPickupLockTicks, Is.EqualTo(12u));
            Assert.That(config.Core.ManualDropPreviousOwnerPickupLockTicks, Is.EqualTo(45u));
            Assert.That(config.Core.Restitution, Is.EqualTo(0.55f));
            Assert.That(config.Core.TangentialVelocityRetention, Is.EqualTo(0.85f));
            Assert.That(config.Core.RestQualificationTicks, Is.EqualTo(15u));
            Assert.That(config.Core.InvalidResetDelayTicks, Is.EqualTo(180u));
            Assert.That(config.Core.ResettingDurationTicks, Is.EqualTo(90u));
            Assert.That(config.Core.CenterLockDurationTicks, Is.EqualTo(30u));
            Assert.That(config.Regulation.CountdownDurationTicks, Is.EqualTo(180u));
            Assert.That(config.Regulation.RegulationDurationTicks, Is.EqualTo(10800u));
            Assert.That(config.Regulation.ScoreTargetPoints, Is.EqualTo(100));
            Assert.That(config.Regulation.ScoreTargetMilliPoints, Is.EqualTo(100000));
            Assert.That(config.Regulation.BaseScoreMilliPointsPerSecond, Is.EqualTo(1000));
            Assert.That(config.Regulation.ActiveRelayScoreMilliPointsPerSecond, Is.EqualTo(2000));
            Assert.That(config.Regulation.PossessionGraceTicks, Is.EqualTo(30u));
            Assert.That(config.Regulation.FinalizingDurationTicks, Is.EqualTo(90u));
            Assert.That(config.Version.IsValid, Is.True);
            Assert.That(config.Version.ToString(), Is.EqualTo("e29e6076766f6c920c23d1d5045edf85"));
        }

        [Test]
        public void CanonicalConfigVersionIsStableAndSensitiveToGameplayValues()
        {
            PlayerConfigValues defaults = PlayerConfigValues.GddDefaults;
            MatchConfig first = ConfigCompiler.Compile(in defaults);
            MatchConfig second = ConfigCompiler.Compile(in defaults);
            PlayerConfigValues changed = new PlayerConfigValues(6.21f, 5.3f, 38f, 48f, 0.45f, 720f);
            MatchConfig changedConfig = ConfigCompiler.Compile(in changed);
            CoreConfigValues changedCore = CreateCoreValues(radiusMeters: 0.31f);
            MatchConfig changedCoreConfig = ConfigCompiler.Compile(in defaults, in changedCore);
            CoreConfigValues defaultCore = CoreConfigValues.GddDefaults;
            RegulationConfigValues changedRegulation = CreateRegulationValues(scoreTargetPoints: 101);
            MatchConfig changedRegulationConfig = ConfigCompiler.Compile(
                in defaults,
                in defaultCore,
                in changedRegulation);

            Assert.That(second.Version, Is.EqualTo(first.Version));
            Assert.That(changedConfig.Version, Is.Not.EqualTo(first.Version));
            Assert.That(changedCoreConfig.Version, Is.Not.EqualTo(first.Version));
            Assert.That(changedRegulationConfig.Version, Is.Not.EqualTo(first.Version));
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

        [Test]
        public void ConfigCompilerRejectsInvalidCoreValuesAndCrossFieldViolations()
        {
            PlayerConfigValues player = PlayerConfigValues.GddDefaults;
            CoreConfigValues nanRadius = CreateCoreValues(radiusMeters: float.NaN);
            CoreConfigValues invertedManualLocks = CreateCoreValues(
                manualOpponentLockSeconds: 0.75f,
                manualPreviousOwnerLockSeconds: 0.20f);
            CoreConfigValues excessiveManualSpeed = CreateCoreValues(
                manualDropSpeed: 13f,
                maximumLooseSpeed: 12f);
            CoreConfigValues invalidRestitution = CreateCoreValues(restitution: 1.1f);

            Assert.Throws<ConfigValidationException>(() => ConfigCompiler.Compile(in player, in nanRadius));
            Assert.Throws<ConfigValidationException>(() => ConfigCompiler.Compile(in player, in invertedManualLocks));
            Assert.Throws<ConfigValidationException>(() => ConfigCompiler.Compile(in player, in excessiveManualSpeed));
            Assert.Throws<ConfigValidationException>(() => ConfigCompiler.Compile(in player, in invalidRestitution));
        }

        [Test]
        public void ConfigCompilerRejectsInvalidRegulationValuesAndCrossFieldViolations()
        {
            PlayerConfigValues player = PlayerConfigValues.GddDefaults;
            CoreConfigValues core = CoreConfigValues.GddDefaults;
            RegulationConfigValues zeroTarget = CreateRegulationValues(scoreTargetPoints: 0);
            RegulationConfigValues invertedRates = CreateRegulationValues(
                baseRate: 2001,
                activeRelayRate: 2000);
            RegulationConfigValues graceLongerThanRegulation = CreateRegulationValues(
                regulationSeconds: 0.5f,
                possessionGraceSeconds: 0.5f);

            Assert.Throws<ConfigValidationException>(
                () => ConfigCompiler.Compile(in player, in core, in zeroTarget));
            Assert.Throws<ConfigValidationException>(
                () => ConfigCompiler.Compile(in player, in core, in invertedRates));
            Assert.Throws<ConfigValidationException>(
                () => ConfigCompiler.Compile(in player, in core, in graceLongerThanRegulation));
        }

        private static CoreConfigValues CreateCoreValues(
            float radiusMeters = 0.30f,
            float manualDropSpeed = 4.5f,
            float manualOpponentLockSeconds = 0.20f,
            float manualPreviousOwnerLockSeconds = 0.75f,
            float maximumLooseSpeed = 12f,
            float restitution = 0.55f)
        {
            return new CoreConfigValues(
                1.1f,
                0.05f,
                radiusMeters,
                manualDropSpeed,
                0.35f,
                manualOpponentLockSeconds,
                manualPreviousOwnerLockSeconds,
                maximumLooseSpeed,
                restitution,
                0.85f,
                3f,
                0.05f,
                0.25f,
                3f,
                1.5f,
                0.5f);
        }

        private static RegulationConfigValues CreateRegulationValues(
            float regulationSeconds = 180f,
            int scoreTargetPoints = 100,
            int baseRate = 1000,
            int activeRelayRate = 2000,
            float possessionGraceSeconds = 0.5f)
        {
            return new RegulationConfigValues(
                3f,
                regulationSeconds,
                scoreTargetPoints,
                baseRate,
                activeRelayRate,
                possessionGraceSeconds,
                1.5f);
        }
    }
}
