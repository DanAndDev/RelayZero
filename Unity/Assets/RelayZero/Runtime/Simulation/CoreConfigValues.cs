namespace RelayZero.Simulation
{
    public readonly struct CoreConfigValues
    {
        public CoreConfigValues(
            float interactionRangeMeters,
            float pickupDistanceTieThresholdMeters,
            float radiusMeters,
            float manualDropSpeedMetersPerSecond,
            float forcedDropPickupLockSeconds,
            float manualDropOpponentPickupLockSeconds,
            float manualDropPreviousOwnerPickupLockSeconds,
            float maximumLooseSpeedMetersPerSecond,
            float restitution,
            float tangentialVelocityRetention,
            float planarDragPerSecond,
            float restSpeedThresholdMetersPerSecond,
            float restQualificationSeconds,
            float invalidResetDelaySeconds,
            float resettingDurationSeconds,
            float centerLockDurationSeconds)
        {
            InteractionRangeMeters = interactionRangeMeters;
            PickupDistanceTieThresholdMeters = pickupDistanceTieThresholdMeters;
            RadiusMeters = radiusMeters;
            ManualDropSpeedMetersPerSecond = manualDropSpeedMetersPerSecond;
            ForcedDropPickupLockSeconds = forcedDropPickupLockSeconds;
            ManualDropOpponentPickupLockSeconds = manualDropOpponentPickupLockSeconds;
            ManualDropPreviousOwnerPickupLockSeconds = manualDropPreviousOwnerPickupLockSeconds;
            MaximumLooseSpeedMetersPerSecond = maximumLooseSpeedMetersPerSecond;
            Restitution = restitution;
            TangentialVelocityRetention = tangentialVelocityRetention;
            PlanarDragPerSecond = planarDragPerSecond;
            RestSpeedThresholdMetersPerSecond = restSpeedThresholdMetersPerSecond;
            RestQualificationSeconds = restQualificationSeconds;
            InvalidResetDelaySeconds = invalidResetDelaySeconds;
            ResettingDurationSeconds = resettingDurationSeconds;
            CenterLockDurationSeconds = centerLockDurationSeconds;
        }

        public static CoreConfigValues GddDefaults => new CoreConfigValues(
            1.1f,
            0.05f,
            0.30f,
            4.5f,
            0.35f,
            0.20f,
            0.75f,
            12f,
            0.55f,
            0.85f,
            3f,
            0.05f,
            0.25f,
            3f,
            1.5f,
            0.5f);

        public float InteractionRangeMeters { get; }

        public float PickupDistanceTieThresholdMeters { get; }

        public float RadiusMeters { get; }

        public float ManualDropSpeedMetersPerSecond { get; }

        public float ForcedDropPickupLockSeconds { get; }

        public float ManualDropOpponentPickupLockSeconds { get; }

        public float ManualDropPreviousOwnerPickupLockSeconds { get; }

        public float MaximumLooseSpeedMetersPerSecond { get; }

        public float Restitution { get; }

        public float TangentialVelocityRetention { get; }

        public float PlanarDragPerSecond { get; }

        public float RestSpeedThresholdMetersPerSecond { get; }

        public float RestQualificationSeconds { get; }

        public float InvalidResetDelaySeconds { get; }

        public float ResettingDurationSeconds { get; }

        public float CenterLockDurationSeconds { get; }
    }
}
