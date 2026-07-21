using Unity.Mathematics;

namespace RelayZero.Simulation
{
    public readonly struct CoreConfig
    {
        internal CoreConfig(in CoreConfigValues values)
        {
            InteractionRangeMeters = values.InteractionRangeMeters;
            InteractionRangeSquared = values.InteractionRangeMeters * values.InteractionRangeMeters;
            PickupDistanceTieThresholdMeters = values.PickupDistanceTieThresholdMeters;
            RadiusMeters = values.RadiusMeters;
            RadiusSquared = values.RadiusMeters * values.RadiusMeters;
            ManualDropSpeedMetersPerSecond = values.ManualDropSpeedMetersPerSecond;
            ForcedDropPickupLockTicks = Foundation.SimulationTime.SecondsToTicksCeiling(
                values.ForcedDropPickupLockSeconds);
            ManualDropOpponentPickupLockTicks = Foundation.SimulationTime.SecondsToTicksCeiling(
                values.ManualDropOpponentPickupLockSeconds);
            ManualDropPreviousOwnerPickupLockTicks = Foundation.SimulationTime.SecondsToTicksCeiling(
                values.ManualDropPreviousOwnerPickupLockSeconds);
            MaximumLooseSpeedMetersPerSecond = values.MaximumLooseSpeedMetersPerSecond;
            MaximumLooseSpeedSquared = values.MaximumLooseSpeedMetersPerSecond *
                values.MaximumLooseSpeedMetersPerSecond;
            Restitution = values.Restitution;
            TangentialVelocityRetention = values.TangentialVelocityRetention;
            PlanarDragPerSecond = values.PlanarDragPerSecond;
            PerTickDragMultiplier = math.exp(-values.PlanarDragPerSecond / Foundation.SimulationTime.TicksPerSecond);
            RestSpeedThresholdMetersPerSecond = values.RestSpeedThresholdMetersPerSecond;
            RestSpeedThresholdSquared = values.RestSpeedThresholdMetersPerSecond *
                values.RestSpeedThresholdMetersPerSecond;
            RestQualificationTicks = Foundation.SimulationTime.SecondsToTicksCeiling(
                values.RestQualificationSeconds);
            InvalidResetDelayTicks = Foundation.SimulationTime.SecondsToTicksCeiling(
                values.InvalidResetDelaySeconds);
            ResettingDurationTicks = Foundation.SimulationTime.SecondsToTicksCeiling(
                values.ResettingDurationSeconds);
            CenterLockDurationTicks = Foundation.SimulationTime.SecondsToTicksCeiling(
                values.CenterLockDurationSeconds);
        }

        public float InteractionRangeMeters { get; }

        public float InteractionRangeSquared { get; }

        public float PickupDistanceTieThresholdMeters { get; }

        public float RadiusMeters { get; }

        public float RadiusSquared { get; }

        public float ManualDropSpeedMetersPerSecond { get; }

        public uint ForcedDropPickupLockTicks { get; }

        public uint ManualDropOpponentPickupLockTicks { get; }

        public uint ManualDropPreviousOwnerPickupLockTicks { get; }

        public float MaximumLooseSpeedMetersPerSecond { get; }

        public float MaximumLooseSpeedSquared { get; }

        public float Restitution { get; }

        public float TangentialVelocityRetention { get; }

        public float PlanarDragPerSecond { get; }

        public float PerTickDragMultiplier { get; }

        public float RestSpeedThresholdMetersPerSecond { get; }

        public float RestSpeedThresholdSquared { get; }

        public uint RestQualificationTicks { get; }

        public uint InvalidResetDelayTicks { get; }

        public uint ResettingDurationTicks { get; }

        public uint CenterLockDurationTicks { get; }
    }
}
