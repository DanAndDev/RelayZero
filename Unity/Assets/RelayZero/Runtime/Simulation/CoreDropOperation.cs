using RelayZero.Foundation;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    internal readonly struct CoreDropOperation
    {
        public CoreDropOperation(
            CoreDropReason reason,
            PlayerSlot previousOwner,
            float2 initialPosition,
            float2 initialVelocity,
            SimulationTick modeLockEndTick,
            SimulationTick playerZeroPickupLockEndTick,
            SimulationTick playerOnePickupLockEndTick,
            uint actionId)
        {
            Reason = reason;
            PreviousOwner = previousOwner;
            InitialPosition = initialPosition;
            InitialVelocity = initialVelocity;
            ModeLockEndTick = modeLockEndTick;
            PlayerZeroPickupLockEndTick = playerZeroPickupLockEndTick;
            PlayerOnePickupLockEndTick = playerOnePickupLockEndTick;
            ActionId = actionId;
        }

        public CoreDropReason Reason { get; }
        public PlayerSlot PreviousOwner { get; }
        public float2 InitialPosition { get; }
        public float2 InitialVelocity { get; }
        public SimulationTick ModeLockEndTick { get; }
        public SimulationTick PlayerZeroPickupLockEndTick { get; }
        public SimulationTick PlayerOnePickupLockEndTick { get; }
        public uint ActionId { get; }
    }
}
