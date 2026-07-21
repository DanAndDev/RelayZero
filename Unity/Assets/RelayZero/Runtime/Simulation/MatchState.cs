using RelayZero.Foundation;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    public sealed class MatchState
    {
        public const int PlayerCount = 2;

        private readonly PlayerState[] players;

        internal MatchState(
            in MatchInitialization initialization,
            float2 corePosition,
            in RegulationConfig regulationConfig)
        {
            MatchId = initialization.MatchId;
            MatchSeed = initialization.MatchSeed;
            Tick = SimulationTick.Zero;
            players = new PlayerState[PlayerCount];
            players[0] = new PlayerState(
                initialization.Roster.PlayerZero,
                PlayerSlot.Zero,
                initialization.PlayerZeroPosition,
                initialization.PlayerZeroFacingDirection);
            players[1] = new PlayerState(
                initialization.Roster.PlayerOne,
                PlayerSlot.One,
                initialization.PlayerOnePosition,
                initialization.PlayerOneFacingDirection);
            Core = new CoreRuntimeState(corePosition);
            Score = default;
            Clock = new MatchClockState(initialization.StartMode, in regulationConfig);
        }

        public MatchId MatchId { get; }

        public ulong MatchSeed { get; }

        public SimulationTick Tick { get; internal set; }

        public CoreRuntimeState Core { get; internal set; }

        public ScoreRuntimeState Score { get; internal set; }

        public MatchClockState Clock { get; internal set; }

        public PlayerState GetPlayer(PlayerSlot slot)
        {
            return players[slot.Index];
        }

        internal void SetPlayer(PlayerSlot slot, in PlayerState player)
        {
            players[slot.Index] = player;
        }
    }
}
