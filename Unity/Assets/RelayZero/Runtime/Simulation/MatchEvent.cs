using RelayZero.Foundation;

namespace RelayZero.Simulation
{
    public readonly struct MatchEvent
    {
        internal MatchEvent(MatchEventId id, MatchEventType type, PlayerSlot playerSlot, int value)
        {
            Id = id;
            Type = type;
            PlayerSlot = playerSlot;
            Value = value;
        }

        public MatchEventId Id { get; }

        public MatchEventType Type { get; }

        public PlayerSlot PlayerSlot { get; }

        public int Value { get; }
    }
}
