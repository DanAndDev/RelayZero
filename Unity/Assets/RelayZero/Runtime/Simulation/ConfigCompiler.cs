using System;
using System.Security.Cryptography;
using RelayZero.Foundation;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    public static class ConfigCompiler
    {
        public const int CurrentSchemaVersion = 3;

        private const float MinimumAuthoredValue = 0.01f;
        private const float MaximumPlayerSpeedMetersPerSecond = 100f;
        private const float MaximumPlayerAccelerationMetersPerSecondSquared = 1000f;
        private const float MaximumPlayerRadiusMeters = 10f;
        private const float MaximumTurnSpeedDegreesPerSecond = 10000f;
        private const float MaximumCoreSpeedMetersPerSecond = 100f;
        private const float MaximumCoreDistanceMeters = 10f;
        private const float MaximumCoreDurationSeconds = 60f;
        private const float MaximumCoreDragPerSecond = 100f;
        private const float MaximumRegulationDurationSeconds = 3600f;
        private const int MaximumScoreTargetPoints = 1000000;
        private const int MaximumScoreRateMilliPointsPerSecond = 1000000;

        public static MatchConfig Compile(in PlayerConfigValues playerValues, int schemaVersion = CurrentSchemaVersion)
        {
            CoreConfigValues coreValues = CoreConfigValues.GddDefaults;
            RegulationConfigValues regulationValues = RegulationConfigValues.GddDefaults;
            return Compile(in playerValues, in coreValues, in regulationValues, schemaVersion);
        }

        public static MatchConfig Compile(
            in PlayerConfigValues playerValues,
            in CoreConfigValues coreValues,
            int schemaVersion = CurrentSchemaVersion)
        {
            RegulationConfigValues regulationValues = RegulationConfigValues.GddDefaults;
            return Compile(in playerValues, in coreValues, in regulationValues, schemaVersion);
        }

        public static MatchConfig Compile(
            in PlayerConfigValues playerValues,
            in CoreConfigValues coreValues,
            in RegulationConfigValues regulationValues,
            int schemaVersion = CurrentSchemaVersion)
        {
            if (schemaVersion != CurrentSchemaVersion)
            {
                throw new ConfigValidationException(
                    $"Configuration schema {schemaVersion} is not supported; expected {CurrentSchemaVersion}.");
            }

            ValidatePositiveFinite(
                playerValues.NormalMaximumSpeedMetersPerSecond,
                nameof(playerValues.NormalMaximumSpeedMetersPerSecond),
                MaximumPlayerSpeedMetersPerSecond);
            ValidatePositiveFinite(
                playerValues.CarrierMaximumSpeedMetersPerSecond,
                nameof(playerValues.CarrierMaximumSpeedMetersPerSecond),
                MaximumPlayerSpeedMetersPerSecond);
            ValidatePositiveFinite(
                playerValues.AccelerationMetersPerSecondSquared,
                nameof(playerValues.AccelerationMetersPerSecondSquared),
                MaximumPlayerAccelerationMetersPerSecondSquared);
            ValidatePositiveFinite(
                playerValues.DecelerationMetersPerSecondSquared,
                nameof(playerValues.DecelerationMetersPerSecondSquared),
                MaximumPlayerAccelerationMetersPerSecondSquared);
            ValidatePositiveFinite(
                playerValues.RadiusMeters,
                nameof(playerValues.RadiusMeters),
                MaximumPlayerRadiusMeters);
            ValidatePositiveFinite(
                playerValues.TurnSpeedDegreesPerSecond,
                nameof(playerValues.TurnSpeedDegreesPerSecond),
                MaximumTurnSpeedDegreesPerSecond);

            if (playerValues.CarrierMaximumSpeedMetersPerSecond > playerValues.NormalMaximumSpeedMetersPerSecond)
            {
                throw new ConfigValidationException("Carrier maximum speed cannot exceed normal maximum speed.");
            }

            ValidatePositiveFinite(
                coreValues.InteractionRangeMeters,
                nameof(coreValues.InteractionRangeMeters),
                MaximumCoreDistanceMeters);
            ValidatePositiveFinite(
                coreValues.PickupDistanceTieThresholdMeters,
                nameof(coreValues.PickupDistanceTieThresholdMeters),
                MaximumCoreDistanceMeters);
            ValidatePositiveFinite(coreValues.RadiusMeters, nameof(coreValues.RadiusMeters), MaximumCoreDistanceMeters);
            ValidatePositiveFinite(
                coreValues.ManualDropSpeedMetersPerSecond,
                nameof(coreValues.ManualDropSpeedMetersPerSecond),
                MaximumCoreSpeedMetersPerSecond);
            ValidatePositiveFinite(
                coreValues.MaximumLooseSpeedMetersPerSecond,
                nameof(coreValues.MaximumLooseSpeedMetersPerSecond),
                MaximumCoreSpeedMetersPerSecond);
            ValidateUnitInterval(coreValues.Restitution, nameof(coreValues.Restitution));
            ValidateUnitInterval(
                coreValues.TangentialVelocityRetention,
                nameof(coreValues.TangentialVelocityRetention));
            ValidatePositiveFinite(
                coreValues.PlanarDragPerSecond,
                nameof(coreValues.PlanarDragPerSecond),
                MaximumCoreDragPerSecond);
            ValidatePositiveFinite(
                coreValues.RestSpeedThresholdMetersPerSecond,
                nameof(coreValues.RestSpeedThresholdMetersPerSecond),
                MaximumCoreSpeedMetersPerSecond);
            ValidateDuration(coreValues.ForcedDropPickupLockSeconds, nameof(coreValues.ForcedDropPickupLockSeconds));
            ValidateDuration(
                coreValues.ManualDropOpponentPickupLockSeconds,
                nameof(coreValues.ManualDropOpponentPickupLockSeconds));
            ValidateDuration(
                coreValues.ManualDropPreviousOwnerPickupLockSeconds,
                nameof(coreValues.ManualDropPreviousOwnerPickupLockSeconds));
            ValidateDuration(coreValues.RestQualificationSeconds, nameof(coreValues.RestQualificationSeconds));
            ValidateDuration(coreValues.InvalidResetDelaySeconds, nameof(coreValues.InvalidResetDelaySeconds));
            ValidateDuration(coreValues.ResettingDurationSeconds, nameof(coreValues.ResettingDurationSeconds));
            ValidateDuration(coreValues.CenterLockDurationSeconds, nameof(coreValues.CenterLockDurationSeconds));

            if (coreValues.PickupDistanceTieThresholdMeters > coreValues.InteractionRangeMeters)
            {
                throw new ConfigValidationException("Pickup distance tie threshold cannot exceed interaction range.");
            }

            if (coreValues.ManualDropSpeedMetersPerSecond > coreValues.MaximumLooseSpeedMetersPerSecond)
            {
                throw new ConfigValidationException("Manual drop speed cannot exceed the loose-core speed cap.");
            }

            if (coreValues.RestSpeedThresholdMetersPerSecond >= coreValues.MaximumLooseSpeedMetersPerSecond)
            {
                throw new ConfigValidationException("Core rest threshold must be below the loose-core speed cap.");
            }

            if (coreValues.ManualDropPreviousOwnerPickupLockSeconds <=
                coreValues.ManualDropOpponentPickupLockSeconds)
            {
                throw new ConfigValidationException(
                    "Manual drop requires the previous-owner pickup lock to exceed the opponent lock.");
            }

            ValidatePositiveFinite(
                regulationValues.CountdownSeconds,
                nameof(regulationValues.CountdownSeconds),
                MaximumCoreDurationSeconds);
            ValidatePositiveFinite(
                regulationValues.RegulationSeconds,
                nameof(regulationValues.RegulationSeconds),
                MaximumRegulationDurationSeconds);
            ValidatePositiveInt(
                regulationValues.ScoreTargetPoints,
                nameof(regulationValues.ScoreTargetPoints),
                MaximumScoreTargetPoints);
            ValidatePositiveInt(
                regulationValues.BaseScoreMilliPointsPerSecond,
                nameof(regulationValues.BaseScoreMilliPointsPerSecond),
                MaximumScoreRateMilliPointsPerSecond);
            ValidatePositiveInt(
                regulationValues.ActiveRelayScoreMilliPointsPerSecond,
                nameof(regulationValues.ActiveRelayScoreMilliPointsPerSecond),
                MaximumScoreRateMilliPointsPerSecond);
            ValidatePositiveFinite(
                regulationValues.PossessionGraceSeconds,
                nameof(regulationValues.PossessionGraceSeconds),
                MaximumCoreDurationSeconds);
            ValidatePositiveFinite(
                regulationValues.FinalizingSeconds,
                nameof(regulationValues.FinalizingSeconds),
                MaximumCoreDurationSeconds);
            if (regulationValues.ActiveRelayScoreMilliPointsPerSecond <
                regulationValues.BaseScoreMilliPointsPerSecond)
            {
                throw new ConfigValidationException(
                    "Active Relay scoring must be at least the base possession rate.");
            }

            if (regulationValues.PossessionGraceSeconds >= regulationValues.RegulationSeconds)
            {
                throw new ConfigValidationException("Possession grace must be shorter than regulation.");
            }

            PlayerMovementConfig player = new PlayerMovementConfig(
                playerValues.NormalMaximumSpeedMetersPerSecond,
                playerValues.CarrierMaximumSpeedMetersPerSecond,
                playerValues.AccelerationMetersPerSecondSquared,
                playerValues.DecelerationMetersPerSecondSquared,
                playerValues.RadiusMeters,
                math.radians(playerValues.TurnSpeedDegreesPerSecond));
            CoreConfig core = new CoreConfig(in coreValues);
            RegulationConfig regulation = new RegulationConfig(in regulationValues);
            ConfigVersion version = ComputeVersion(
                in playerValues,
                in coreValues,
                in regulationValues,
                schemaVersion);
            return new MatchConfig(schemaVersion, player, core, regulation, version);
        }

        private static void ValidatePositiveFinite(float value, string fieldName, float maximum)
        {
            if (!math.isfinite(value) || value < MinimumAuthoredValue || value > maximum)
            {
                throw new ConfigValidationException(
                    fieldName + " must be finite and in the supported range " +
                    MinimumAuthoredValue + " through " + maximum + ".");
            }
        }

        private static void ValidateUnitInterval(float value, string fieldName)
        {
            if (!math.isfinite(value) || value < 0f || value > 1f)
            {
                throw new ConfigValidationException(fieldName + " must be finite and between zero and one.");
            }
        }

        private static void ValidatePositiveInt(int value, string fieldName, int maximum)
        {
            if (value <= 0 || value > maximum)
            {
                throw new ConfigValidationException(
                    fieldName + " must be in the supported range 1 through " + maximum + ".");
            }
        }

        private static void ValidateDuration(float value, string fieldName)
        {
            ValidatePositiveFinite(value, fieldName, MaximumCoreDurationSeconds);
        }

        private static ConfigVersion ComputeVersion(
            in PlayerConfigValues playerValues,
            in CoreConfigValues coreValues,
            in RegulationConfigValues regulationValues,
            int schemaVersion)
        {
            byte[] canonicalBytes = new byte[124];
            WriteInt32LittleEndian(canonicalBytes, 0, schemaVersion);
            WriteInt32LittleEndian(canonicalBytes, 4, SimulationTime.TicksPerSecond);
            WriteInt32LittleEndian(canonicalBytes, 8, math.asint(playerValues.NormalMaximumSpeedMetersPerSecond));
            WriteInt32LittleEndian(canonicalBytes, 12, math.asint(playerValues.CarrierMaximumSpeedMetersPerSecond));
            WriteInt32LittleEndian(canonicalBytes, 16, math.asint(playerValues.AccelerationMetersPerSecondSquared));
            WriteInt32LittleEndian(canonicalBytes, 20, math.asint(playerValues.DecelerationMetersPerSecondSquared));
            WriteInt32LittleEndian(canonicalBytes, 24, math.asint(playerValues.RadiusMeters));
            WriteInt32LittleEndian(canonicalBytes, 28, math.asint(playerValues.TurnSpeedDegreesPerSecond));
            WriteInt32LittleEndian(canonicalBytes, 32, math.asint(coreValues.InteractionRangeMeters));
            WriteInt32LittleEndian(canonicalBytes, 36, math.asint(coreValues.PickupDistanceTieThresholdMeters));
            WriteInt32LittleEndian(canonicalBytes, 40, math.asint(coreValues.RadiusMeters));
            WriteInt32LittleEndian(canonicalBytes, 44, math.asint(coreValues.ManualDropSpeedMetersPerSecond));
            WriteInt32LittleEndian(canonicalBytes, 48, math.asint(coreValues.ForcedDropPickupLockSeconds));
            WriteInt32LittleEndian(canonicalBytes, 52, math.asint(coreValues.ManualDropOpponentPickupLockSeconds));
            WriteInt32LittleEndian(canonicalBytes, 56, math.asint(coreValues.ManualDropPreviousOwnerPickupLockSeconds));
            WriteInt32LittleEndian(canonicalBytes, 60, math.asint(coreValues.MaximumLooseSpeedMetersPerSecond));
            WriteInt32LittleEndian(canonicalBytes, 64, math.asint(coreValues.Restitution));
            WriteInt32LittleEndian(canonicalBytes, 68, math.asint(coreValues.TangentialVelocityRetention));
            WriteInt32LittleEndian(canonicalBytes, 72, math.asint(coreValues.PlanarDragPerSecond));
            WriteInt32LittleEndian(canonicalBytes, 76, math.asint(coreValues.RestSpeedThresholdMetersPerSecond));
            WriteInt32LittleEndian(canonicalBytes, 80, math.asint(coreValues.RestQualificationSeconds));
            WriteInt32LittleEndian(canonicalBytes, 84, math.asint(coreValues.InvalidResetDelaySeconds));
            WriteInt32LittleEndian(canonicalBytes, 88, math.asint(coreValues.ResettingDurationSeconds));
            WriteInt32LittleEndian(canonicalBytes, 92, math.asint(coreValues.CenterLockDurationSeconds));
            WriteInt32LittleEndian(canonicalBytes, 96, math.asint(regulationValues.CountdownSeconds));
            WriteInt32LittleEndian(canonicalBytes, 100, math.asint(regulationValues.RegulationSeconds));
            WriteInt32LittleEndian(canonicalBytes, 104, regulationValues.ScoreTargetPoints);
            WriteInt32LittleEndian(canonicalBytes, 108, regulationValues.BaseScoreMilliPointsPerSecond);
            WriteInt32LittleEndian(canonicalBytes, 112, regulationValues.ActiveRelayScoreMilliPointsPerSecond);
            WriteInt32LittleEndian(canonicalBytes, 116, math.asint(regulationValues.PossessionGraceSeconds));
            WriteInt32LittleEndian(canonicalBytes, 120, math.asint(regulationValues.FinalizingSeconds));

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(canonicalBytes);
                return ConfigVersion.FromBytes(hash);
            }
        }

        private static void WriteInt32LittleEndian(byte[] destination, int offset, int value)
        {
            unchecked
            {
                destination[offset] = (byte)value;
                destination[offset + 1] = (byte)(value >> 8);
                destination[offset + 2] = (byte)(value >> 16);
                destination[offset + 3] = (byte)(value >> 24);
            }
        }
    }
}
