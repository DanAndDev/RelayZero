using RelayZero.Foundation;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    public struct PlayerState
    {
        internal PlayerState(PlayerId playerId, PlayerSlot slot, float2 position, float2 facingDirection)
        {
            PlayerId = playerId;
            Slot = slot;
            Position = position;
            LastValidPosition = position;
            Velocity = float2.zero;
            FacingDirection = facingDirection;
            IsCarrying = false;
            LocomotionMode = PlayerLocomotionMode.Normal;
            ActionMode = PlayerActionMode.None;
            ConnectionMode = PlayerConnectionMode.Connected;
            HasConsumedInteractSequence = false;
            LastConsumedInteractSequence = CommandSequence.Zero;
        }

        public PlayerId PlayerId { get; }

        public PlayerSlot Slot { get; }

        public float2 Position { get; internal set; }

        public float2 LastValidPosition { get; internal set; }

        public float2 Velocity { get; internal set; }

        public float2 FacingDirection { get; internal set; }

        public bool IsCarrying { get; internal set; }

        public PlayerLocomotionMode LocomotionMode { get; internal set; }

        public PlayerActionMode ActionMode { get; internal set; }

        public PlayerConnectionMode ConnectionMode { get; internal set; }

        internal bool HasConsumedInteractSequence { get; set; }

        internal CommandSequence LastConsumedInteractSequence { get; set; }

        internal bool CanInteract => LocomotionMode == PlayerLocomotionMode.Normal &&
            ActionMode == PlayerActionMode.None &&
            ConnectionMode == PlayerConnectionMode.Connected;
    }
}
