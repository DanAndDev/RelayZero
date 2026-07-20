using UnityEngine;

namespace RelayZero.Simulation.Authoring
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Relay Zero/Simulation/Player Config")]
    public sealed class PlayerConfigAsset : ScriptableObject
    {
        [SerializeField]
        [Min(0.01f)]
        private float normalMaximumSpeedMetersPerSecond = 6.2f;

        [SerializeField]
        [Min(0.01f)]
        private float carrierMaximumSpeedMetersPerSecond = 5.3f;

        [SerializeField]
        [Min(0.01f)]
        private float accelerationMetersPerSecondSquared = 38f;

        [SerializeField]
        [Min(0.01f)]
        private float decelerationMetersPerSecondSquared = 48f;

        [SerializeField]
        [Min(0.01f)]
        private float radiusMeters = 0.45f;

        [SerializeField]
        [Min(0.01f)]
        private float turnSpeedDegreesPerSecond = 720f;

        public float NormalMaximumSpeedMetersPerSecond => normalMaximumSpeedMetersPerSecond;

        public float CarrierMaximumSpeedMetersPerSecond => carrierMaximumSpeedMetersPerSecond;

        public float AccelerationMetersPerSecondSquared => accelerationMetersPerSecondSquared;

        public float DecelerationMetersPerSecondSquared => decelerationMetersPerSecondSquared;

        public float RadiusMeters => radiusMeters;

        public float TurnSpeedDegreesPerSecond => turnSpeedDegreesPerSecond;

        public PlayerConfigValues CreateValues()
        {
            return new PlayerConfigValues(
                normalMaximumSpeedMetersPerSecond,
                carrierMaximumSpeedMetersPerSecond,
                accelerationMetersPerSecondSquared,
                decelerationMetersPerSecondSquared,
                radiusMeters,
                turnSpeedDegreesPerSecond);
        }
    }
}
