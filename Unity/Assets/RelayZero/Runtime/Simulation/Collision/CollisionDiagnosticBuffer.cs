using System;
using RelayZero.Arena;
using RelayZero.Foundation;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    public sealed class CollisionDiagnosticBuffer
    {
        public const int DefaultCapacity = 32;

        private readonly CollisionDiagnostic[] entries;

        public CollisionDiagnosticBuffer(int capacity = DefaultCapacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            entries = new CollisionDiagnostic[capacity];
        }

        public int Count { get; private set; }

        public int Capacity => entries.Length;

        public int DroppedCount { get; private set; }

        public SimulationTick Tick { get; private set; }

        public CollisionDiagnostic this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return entries[index];
            }
        }

        internal void Reset(SimulationTick tick)
        {
            Tick = tick;
            Count = 0;
            DroppedCount = 0;
        }

        internal void Add(
            PlayerSlot slot,
            CollisionDiagnosticKind kind,
            ArenaElementId elementId,
            float timeOfImpact,
            float2 sweepStart,
            float2 requestedDisplacement,
            float2 contactPosition,
            float2 normal,
            float2 remainingDisplacement,
            int iteration)
        {
            if (Count >= entries.Length)
            {
                DroppedCount++;
                return;
            }

            entries[Count++] = new CollisionDiagnostic(
                Tick,
                slot,
                kind,
                elementId,
                timeOfImpact,
                sweepStart,
                requestedDisplacement,
                contactPosition,
                normal,
                remainingDisplacement,
                (byte)math.clamp(iteration, 0, byte.MaxValue));
        }
    }
}
