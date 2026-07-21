using UnityEngine;

namespace RelayZero.Simulation.Authoring
{
    [CreateAssetMenu(fileName = "CoreConfig", menuName = "Relay Zero/Simulation/Core Config")]
    public sealed class CoreConfigAsset : ScriptableObject
    {
        [SerializeField, Min(0.01f)]
        private float interactionRangeMeters = 1.1f;

        [SerializeField, Min(0.001f)]
        private float pickupDistanceTieThresholdMeters = 0.05f;

        [SerializeField, Min(0.01f)]
        private float radiusMeters = 0.30f;

        [SerializeField, Min(0.01f)]
        private float manualDropSpeedMetersPerSecond = 4.5f;

        [SerializeField, Min(0.01f)]
        private float forcedDropPickupLockSeconds = 0.35f;

        [SerializeField, Min(0.01f)]
        private float manualDropOpponentPickupLockSeconds = 0.20f;

        [SerializeField, Min(0.01f)]
        private float manualDropPreviousOwnerPickupLockSeconds = 0.75f;

        [SerializeField, Min(0.01f)]
        private float maximumLooseSpeedMetersPerSecond = 12f;

        [SerializeField, Range(0f, 1f)]
        private float restitution = 0.55f;

        [SerializeField, Range(0f, 1f)]
        private float tangentialVelocityRetention = 0.85f;

        [SerializeField, Min(0f)]
        private float planarDragPerSecond = 3f;

        [SerializeField, Min(0.001f)]
        private float restSpeedThresholdMetersPerSecond = 0.05f;

        [SerializeField, Min(0.01f)]
        private float restQualificationSeconds = 0.25f;

        [SerializeField, Min(0.01f)]
        private float invalidResetDelaySeconds = 3f;

        [SerializeField, Min(0.01f)]
        private float resettingDurationSeconds = 1.5f;

        [SerializeField, Min(0.01f)]
        private float centerLockDurationSeconds = 0.5f;

        public float InteractionRangeMeters => interactionRangeMeters;
        public float PickupDistanceTieThresholdMeters => pickupDistanceTieThresholdMeters;
        public float RadiusMeters => radiusMeters;
        public float ManualDropSpeedMetersPerSecond => manualDropSpeedMetersPerSecond;
        public float ForcedDropPickupLockSeconds => forcedDropPickupLockSeconds;
        public float ManualDropOpponentPickupLockSeconds => manualDropOpponentPickupLockSeconds;
        public float ManualDropPreviousOwnerPickupLockSeconds => manualDropPreviousOwnerPickupLockSeconds;
        public float MaximumLooseSpeedMetersPerSecond => maximumLooseSpeedMetersPerSecond;
        public float Restitution => restitution;
        public float TangentialVelocityRetention => tangentialVelocityRetention;
        public float PlanarDragPerSecond => planarDragPerSecond;
        public float RestSpeedThresholdMetersPerSecond => restSpeedThresholdMetersPerSecond;
        public float RestQualificationSeconds => restQualificationSeconds;
        public float InvalidResetDelaySeconds => invalidResetDelaySeconds;
        public float ResettingDurationSeconds => resettingDurationSeconds;
        public float CenterLockDurationSeconds => centerLockDurationSeconds;

        public CoreConfigValues CreateValues()
        {
            return new CoreConfigValues(
                interactionRangeMeters,
                pickupDistanceTieThresholdMeters,
                radiusMeters,
                manualDropSpeedMetersPerSecond,
                forcedDropPickupLockSeconds,
                manualDropOpponentPickupLockSeconds,
                manualDropPreviousOwnerPickupLockSeconds,
                maximumLooseSpeedMetersPerSecond,
                restitution,
                tangentialVelocityRetention,
                planarDragPerSecond,
                restSpeedThresholdMetersPerSecond,
                restQualificationSeconds,
                invalidResetDelaySeconds,
                resettingDurationSeconds,
                centerLockDurationSeconds);
        }
    }
}
