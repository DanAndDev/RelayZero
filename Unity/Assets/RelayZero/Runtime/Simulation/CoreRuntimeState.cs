using RelayZero.Foundation;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    public struct CoreRuntimeState
    {
        internal CoreRuntimeState(float2 resetPosition)
        {
            Mode = CoreMode.Locked;
            HasOwner = false;
            Owner = PlayerSlot.Zero;
            Position = resetPosition;
            LastValidPosition = resetPosition;
            ResetOriginPosition = resetPosition;
            Velocity = float2.zero;
            ModeEndTick = SimulationTick.Zero;
            PlayerZeroPickupLockEndTick = SimulationTick.Zero;
            PlayerOnePickupLockEndTick = SimulationTick.Zero;
            PossessionGraceEndTick = SimulationTick.Zero;
            InvalidTickCount = 0u;
            RestTickCount = 0u;
            IsResting = true;
            ResetCompletionPending = false;
            LastDropReason = CoreDropReason.None;
            LastDropActionId = 0u;
        }

        public CoreMode Mode { get; internal set; }

        public bool HasOwner { get; internal set; }

        public PlayerSlot Owner { get; internal set; }

        public float2 Position { get; internal set; }

        public float2 LastValidPosition { get; internal set; }

        public float2 ResetOriginPosition { get; internal set; }

        public float2 Velocity { get; internal set; }

        public SimulationTick ModeEndTick { get; internal set; }

        public SimulationTick PlayerZeroPickupLockEndTick { get; internal set; }

        public SimulationTick PlayerOnePickupLockEndTick { get; internal set; }

        public SimulationTick PossessionGraceEndTick { get; internal set; }

        public uint InvalidTickCount { get; internal set; }

        public uint RestTickCount { get; internal set; }

        public bool IsResting { get; internal set; }

        internal bool ResetCompletionPending { get; set; }

        public CoreDropReason LastDropReason { get; internal set; }

        public uint LastDropActionId { get; internal set; }

        public bool IsCarriedBy(PlayerSlot slot)
        {
            return Mode == CoreMode.Carried && HasOwner && Owner == slot;
        }

        public SimulationTick GetPickupLockEndTick(PlayerSlot slot)
        {
            return slot == PlayerSlot.Zero ? PlayerZeroPickupLockEndTick : PlayerOnePickupLockEndTick;
        }

        public uint GetPickupLockRemainingTicks(PlayerSlot slot, SimulationTick currentTick)
        {
            SimulationTick endTick = GetPickupLockEndTick(slot);
            return endTick.IsNewerThan(currentTick) ? endTick.DistanceSince(currentTick) : 0u;
        }

        public bool IsPickupEligible(PlayerSlot slot, SimulationTick currentTick)
        {
            if (Mode != CoreMode.Loose && Mode != CoreMode.DropLock)
            {
                return false;
            }

            if (Mode == CoreMode.DropLock && ModeEndTick.IsNewerThan(currentTick))
            {
                return false;
            }

            return !GetPickupLockEndTick(slot).IsNewerThan(currentTick);
        }

        public uint GetPossessionGraceRemainingTicks(SimulationTick currentTick)
        {
            return Mode == CoreMode.Carried && PossessionGraceEndTick.IsNewerThan(currentTick)
                ? PossessionGraceEndTick.DistanceSince(currentTick)
                : 0u;
        }
    }
}
