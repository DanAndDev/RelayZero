using System;
using UnityEngine;

namespace RelayZero.Simulation.Authoring
{
    [CreateAssetMenu(fileName = "MatchConfig", menuName = "Relay Zero/Simulation/Match Config")]
    public sealed class MatchConfigAsset : ScriptableObject
    {
        [SerializeField]
        private int schemaVersion = ConfigCompiler.CurrentSchemaVersion;

        [SerializeField]
        private PlayerConfigAsset playerConfig = null!;

        [SerializeField]
        private CoreConfigAsset coreConfig = null!;

        [SerializeField]
        private RegulationConfigAsset regulationConfig = null!;

        public int SchemaVersion => schemaVersion;

        public PlayerConfigAsset PlayerConfig => playerConfig;

        public CoreConfigAsset CoreConfig => coreConfig;

        public RegulationConfigAsset RegulationConfig => regulationConfig;

        public MatchConfig Compile()
        {
            if (playerConfig == null)
            {
                throw new InvalidOperationException("Match configuration requires a PlayerConfigAsset.");
            }

            if (coreConfig == null)
            {
                throw new InvalidOperationException("Match configuration requires a CoreConfigAsset.");
            }

            if (regulationConfig == null)
            {
                throw new InvalidOperationException("Match configuration requires a RegulationConfigAsset.");
            }

            PlayerConfigValues playerValues = playerConfig.CreateValues();
            CoreConfigValues coreValues = coreConfig.CreateValues();
            RegulationConfigValues regulationValues = regulationConfig.CreateValues();
            return ConfigCompiler.Compile(in playerValues, in coreValues, in regulationValues, schemaVersion);
        }
    }
}
