using UnityEngine;

namespace RelayZero.Simulation.Authoring
{
    [CreateAssetMenu(fileName = "RegulationConfig", menuName = "Relay Zero/Simulation/Regulation Config")]
    public sealed class RegulationConfigAsset : ScriptableObject
    {
        [SerializeField, Min(0.01f)]
        private float countdownSeconds = 3f;

        [SerializeField, Min(0.01f)]
        private float regulationSeconds = 180f;

        [SerializeField, Min(1)]
        private int scoreTargetPoints = 100;

        [SerializeField, Min(1)]
        private int baseScoreMilliPointsPerSecond = 1000;

        [SerializeField, Min(1)]
        private int activeRelayScoreMilliPointsPerSecond = 2000;

        [SerializeField, Min(0.01f)]
        private float possessionGraceSeconds = 0.5f;

        [SerializeField, Min(0.01f)]
        private float finalizingSeconds = 1.5f;

        public float CountdownSeconds => countdownSeconds;
        public float RegulationSeconds => regulationSeconds;
        public int ScoreTargetPoints => scoreTargetPoints;
        public int BaseScoreMilliPointsPerSecond => baseScoreMilliPointsPerSecond;
        public int ActiveRelayScoreMilliPointsPerSecond => activeRelayScoreMilliPointsPerSecond;
        public float PossessionGraceSeconds => possessionGraceSeconds;
        public float FinalizingSeconds => finalizingSeconds;

        public RegulationConfigValues CreateValues()
        {
            return new RegulationConfigValues(
                countdownSeconds,
                regulationSeconds,
                scoreTargetPoints,
                baseScoreMilliPointsPerSecond,
                activeRelayScoreMilliPointsPerSecond,
                possessionGraceSeconds,
                finalizingSeconds);
        }
    }
}
