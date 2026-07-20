namespace RelayZero.Simulation
{
    public readonly struct PlayerConfigValues
    {
        public PlayerConfigValues(
            float normalMaximumSpeedMetersPerSecond,
            float carrierMaximumSpeedMetersPerSecond,
            float accelerationMetersPerSecondSquared,
            float decelerationMetersPerSecondSquared,
            float radiusMeters,
            float turnSpeedDegreesPerSecond)
        {
            NormalMaximumSpeedMetersPerSecond = normalMaximumSpeedMetersPerSecond;
            CarrierMaximumSpeedMetersPerSecond = carrierMaximumSpeedMetersPerSecond;
            AccelerationMetersPerSecondSquared = accelerationMetersPerSecondSquared;
            DecelerationMetersPerSecondSquared = decelerationMetersPerSecondSquared;
            RadiusMeters = radiusMeters;
            TurnSpeedDegreesPerSecond = turnSpeedDegreesPerSecond;
        }

        public static PlayerConfigValues GddDefaults => new PlayerConfigValues(
            6.2f,
            5.3f,
            38f,
            48f,
            0.45f,
            720f);

        public float NormalMaximumSpeedMetersPerSecond { get; }

        public float CarrierMaximumSpeedMetersPerSecond { get; }

        public float AccelerationMetersPerSecondSquared { get; }

        public float DecelerationMetersPerSecondSquared { get; }

        public float RadiusMeters { get; }

        public float TurnSpeedDegreesPerSecond { get; }
    }
}
