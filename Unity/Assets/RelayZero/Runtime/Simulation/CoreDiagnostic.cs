using RelayZero.Arena;
using RelayZero.Foundation;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    public enum CoreDiagnosticKind : byte
    {
        None = 0,
        StateChanged = 1,
        PickupRejectedRange = 2,
        PickupRejectedOccluded = 3,
        PickupRejectedLocked = 4,
        Bounce = 5,
        InvalidOrUnreachable = 6,
        Rested = 7,
    }

    public readonly struct CoreDiagnostic
    {
        internal CoreDiagnostic(
            SimulationTick tick,
            CoreDiagnosticKind kind,
            CoreMode mode,
            bool hasPlayerSlot,
            PlayerSlot playerSlot,
            ArenaElementId elementId,
            float2 position,
            float2 velocity,
            float2 normal)
        {
            Tick = tick;
            Kind = kind;
            Mode = mode;
            HasPlayerSlot = hasPlayerSlot;
            PlayerSlot = playerSlot;
            ElementId = elementId;
            Position = position;
            Velocity = velocity;
            Normal = normal;
        }

        public SimulationTick Tick { get; }
        public CoreDiagnosticKind Kind { get; }
        public CoreMode Mode { get; }
        public bool HasPlayerSlot { get; }
        public PlayerSlot PlayerSlot { get; }
        public ArenaElementId ElementId { get; }
        public float2 Position { get; }
        public float2 Velocity { get; }
        public float2 Normal { get; }
    }
}
