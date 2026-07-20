namespace RelayZero.Simulation
{
    public readonly struct PlayerMovementConfig
    {
        internal PlayerMovementConfig(
            float normalMaximumSpeedMetersPerSecond,
            float carrierMaximumSpeedMetersPerSecond,
            float accelerationMetersPerSecondSquared,
            float decelerationMetersPerSecondSquared,
            float radiusMeters,
            float turnSpeedRadiansPerSecond)
        {
            NormalMaximumSpeedMetersPerSecond = normalMaximumSpeedMetersPerSecond;
            CarrierMaximumSpeedMetersPerSecond = carrierMaximumSpeedMetersPerSecond;
            AccelerationMetersPerSecondSquared = accelerationMetersPerSecondSquared;
            DecelerationMetersPerSecondSquared = decelerationMetersPerSecondSquared;
            RadiusMeters = radiusMeters;
            TurnSpeedRadiansPerSecond = turnSpeedRadiansPerSecond;
            NormalMaximumSpeedSquared = normalMaximumSpeedMetersPerSecond * normalMaximumSpeedMetersPerSecond;
            CarrierMaximumSpeedSquared = carrierMaximumSpeedMetersPerSecond * carrierMaximumSpeedMetersPerSecond;
            RadiusSquared = radiusMeters * radiusMeters;
        }

        public float NormalMaximumSpeedMetersPerSecond { get; }

        public float CarrierMaximumSpeedMetersPerSecond { get; }

        public float AccelerationMetersPerSecondSquared { get; }

        public float DecelerationMetersPerSecondSquared { get; }

        public float RadiusMeters { get; }

        public float TurnSpeedRadiansPerSecond { get; }

        public float NormalMaximumSpeedSquared { get; }

        public float CarrierMaximumSpeedSquared { get; }

        public float RadiusSquared { get; }
    }
}
