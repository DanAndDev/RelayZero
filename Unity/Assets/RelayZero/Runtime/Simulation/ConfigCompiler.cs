using System;
using System.Security.Cryptography;
using RelayZero.Foundation;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    public static class ConfigCompiler
    {
        public const int CurrentSchemaVersion = 1;

        private const float MinimumAuthoredValue = 0.01f;
        private const float MaximumPlayerSpeedMetersPerSecond = 100f;
        private const float MaximumPlayerAccelerationMetersPerSecondSquared = 1000f;
        private const float MaximumPlayerRadiusMeters = 10f;
        private const float MaximumTurnSpeedDegreesPerSecond = 10000f;

        public static MatchConfig Compile(in PlayerConfigValues playerValues, int schemaVersion = CurrentSchemaVersion)
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

            PlayerMovementConfig player = new PlayerMovementConfig(
                playerValues.NormalMaximumSpeedMetersPerSecond,
                playerValues.CarrierMaximumSpeedMetersPerSecond,
                playerValues.AccelerationMetersPerSecondSquared,
                playerValues.DecelerationMetersPerSecondSquared,
                playerValues.RadiusMeters,
                math.radians(playerValues.TurnSpeedDegreesPerSecond));
            ConfigVersion version = ComputeVersion(in playerValues, schemaVersion);
            return new MatchConfig(schemaVersion, player, version);
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

        private static ConfigVersion ComputeVersion(in PlayerConfigValues playerValues, int schemaVersion)
        {
            byte[] canonicalBytes = new byte[32];
            WriteInt32LittleEndian(canonicalBytes, 0, schemaVersion);
            WriteInt32LittleEndian(canonicalBytes, 4, SimulationTime.TicksPerSecond);
            WriteInt32LittleEndian(canonicalBytes, 8, math.asint(playerValues.NormalMaximumSpeedMetersPerSecond));
            WriteInt32LittleEndian(canonicalBytes, 12, math.asint(playerValues.CarrierMaximumSpeedMetersPerSecond));
            WriteInt32LittleEndian(canonicalBytes, 16, math.asint(playerValues.AccelerationMetersPerSecondSquared));
            WriteInt32LittleEndian(canonicalBytes, 20, math.asint(playerValues.DecelerationMetersPerSecondSquared));
            WriteInt32LittleEndian(canonicalBytes, 24, math.asint(playerValues.RadiusMeters));
            WriteInt32LittleEndian(canonicalBytes, 28, math.asint(playerValues.TurnSpeedDegreesPerSecond));

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
