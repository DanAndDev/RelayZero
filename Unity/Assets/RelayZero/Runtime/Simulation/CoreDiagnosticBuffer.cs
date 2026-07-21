using System;
using RelayZero.Arena;
using RelayZero.Foundation;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    public sealed class CoreDiagnosticBuffer
    {
        public const int Capacity = 16;

        private readonly CoreDiagnostic[] diagnostics = new CoreDiagnostic[Capacity];

        public SimulationTick Tick { get; private set; }
        public int Count { get; private set; }
        public int DroppedCount { get; private set; }

        public CoreDiagnostic this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return diagnostics[index];
            }
        }

        internal void Reset(SimulationTick tick)
        {
            Tick = tick;
            Count = 0;
            DroppedCount = 0;
        }

        internal void Add(
            CoreDiagnosticKind kind,
            CoreMode mode,
            float2 position,
            float2 velocity,
            float2 normal,
            ArenaElementId elementId,
            bool hasPlayerSlot = false,
            PlayerSlot playerSlot = default)
        {
            if (Count == Capacity)
            {
                DroppedCount++;
                return;
            }

            diagnostics[Count++] = new CoreDiagnostic(
                Tick,
                kind,
                mode,
                hasPlayerSlot,
                playerSlot,
                elementId,
                position,
                velocity,
                normal);
        }
    }
}
