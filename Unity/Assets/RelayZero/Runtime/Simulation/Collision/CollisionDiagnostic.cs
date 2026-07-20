using RelayZero.Arena;
using RelayZero.Foundation;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    public enum CollisionDiagnosticKind : byte
    {
        StaticImpact,
        PlayerPairCorrection,
        Recovery,
        IterationLimit,
    }

    public readonly struct CollisionDiagnostic
    {
        internal CollisionDiagnostic(
            SimulationTick tick,
            PlayerSlot slot,
            CollisionDiagnosticKind kind,
            ArenaElementId elementId,
            float timeOfImpact,
            float2 sweepStart,
            float2 requestedDisplacement,
            float2 contactPosition,
            float2 normal,
            float2 remainingDisplacement,
            byte iteration)
        {
            Tick = tick;
            Slot = slot;
            Kind = kind;
            ElementId = elementId;
            TimeOfImpact = timeOfImpact;
            SweepStart = sweepStart;
            RequestedDisplacement = requestedDisplacement;
            ContactPosition = contactPosition;
            Normal = normal;
            RemainingDisplacement = remainingDisplacement;
            Iteration = iteration;
        }

        public SimulationTick Tick { get; }

        public PlayerSlot Slot { get; }

        public CollisionDiagnosticKind Kind { get; }

        public ArenaElementId ElementId { get; }

        public float TimeOfImpact { get; }

        public float2 SweepStart { get; }

        public float2 RequestedDisplacement { get; }

        public float2 ContactPosition { get; }

        public float2 Normal { get; }

        public float2 RemainingDisplacement { get; }

        public byte Iteration { get; }
    }
}
