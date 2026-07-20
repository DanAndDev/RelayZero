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

        public int SchemaVersion => schemaVersion;

        public PlayerConfigAsset PlayerConfig => playerConfig;

        public MatchConfig Compile()
        {
            if (playerConfig == null)
            {
                throw new InvalidOperationException("Match configuration requires a PlayerConfigAsset.");
            }

            PlayerConfigValues values = playerConfig.CreateValues();
            return ConfigCompiler.Compile(in values, schemaVersion);
        }
    }
}
