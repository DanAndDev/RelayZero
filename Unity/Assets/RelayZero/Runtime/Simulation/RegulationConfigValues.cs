namespace RelayZero.Simulation
{
    public readonly struct RegulationConfigValues
    {
        public RegulationConfigValues(
            float countdownSeconds,
            float regulationSeconds,
            int scoreTargetPoints,
            int baseScoreMilliPointsPerSecond,
            int activeRelayScoreMilliPointsPerSecond,
            float possessionGraceSeconds,
            float finalizingSeconds)
        {
            CountdownSeconds = countdownSeconds;
            RegulationSeconds = regulationSeconds;
            ScoreTargetPoints = scoreTargetPoints;
            BaseScoreMilliPointsPerSecond = baseScoreMilliPointsPerSecond;
            ActiveRelayScoreMilliPointsPerSecond = activeRelayScoreMilliPointsPerSecond;
            PossessionGraceSeconds = possessionGraceSeconds;
            FinalizingSeconds = finalizingSeconds;
        }

        public float CountdownSeconds { get; }

        public float RegulationSeconds { get; }

        public int ScoreTargetPoints { get; }

        public int BaseScoreMilliPointsPerSecond { get; }

        public int ActiveRelayScoreMilliPointsPerSecond { get; }

        public float PossessionGraceSeconds { get; }

        public float FinalizingSeconds { get; }

        public static RegulationConfigValues GddDefaults => new RegulationConfigValues(
            3f,
            180f,
            100,
            1000,
            2000,
            0.5f,
            1.5f);
    }
}
