using System;
using RelayZero.Foundation;

namespace RelayZero.Simulation
{
    public sealed class MatchEventBuffer
    {
        public const int Capacity = 64;

        private readonly MatchEvent[] events = new MatchEvent[Capacity];

        public int Count { get; private set; }

        public SimulationTick Tick { get; private set; }

        public MatchEvent this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return events[index];
            }
        }

        public void Reset(SimulationTick tick)
        {
            Tick = tick;
            Count = 0;
        }

        public void Add(MatchEventType type, PlayerSlot playerSlot, int value = 0)
        {
            if (type == MatchEventType.None)
            {
                throw new ArgumentOutOfRangeException(nameof(type), "An emitted event requires a concrete type.");
            }

            if (Count >= Capacity)
            {
                throw new InvalidOperationException(
                    "The per-tick match event capacity was exceeded; authoritative events cannot be dropped.");
            }

            byte eventIndex = checked((byte)Count);
            events[Count] = new MatchEvent(new MatchEventId(Tick, eventIndex), type, playerSlot, value);
            Count++;
        }

        public ReadOnlySpan<MatchEvent> AsSpan()
        {
            return new ReadOnlySpan<MatchEvent>(events, 0, Count);
        }
    }
}
