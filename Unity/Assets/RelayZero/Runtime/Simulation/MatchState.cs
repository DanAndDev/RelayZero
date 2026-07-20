using RelayZero.Foundation;

namespace RelayZero.Simulation
{
    public sealed class MatchState
    {
        public const int PlayerCount = 2;

        private readonly PlayerState[] players;

        internal MatchState(in MatchInitialization initialization)
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
        }

        public MatchId MatchId { get; }

        public ulong MatchSeed { get; }

        public SimulationTick Tick { get; internal set; }

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
