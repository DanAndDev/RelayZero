using RelayZero.Foundation;

namespace RelayZero.Simulation
{
    public readonly struct MatchEvent
    {
        internal MatchEvent(
            MatchEventId id,
            MatchEventType type,
            bool hasPlayerSlot,
            PlayerSlot playerSlot,
            int value)
        {
            Id = id;
            Type = type;
            HasPlayerSlot = hasPlayerSlot;
            PlayerSlot = playerSlot;
            Value = value;
        }

        public MatchEventId Id { get; }

        public MatchEventType Type { get; }

        public bool HasPlayerSlot { get; }

        public PlayerSlot PlayerSlot { get; }

        public int Value { get; }
    }
}
