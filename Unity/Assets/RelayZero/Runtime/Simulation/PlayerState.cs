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
        }

        public PlayerId PlayerId { get; }

        public PlayerSlot Slot { get; }

        public float2 Position { get; internal set; }

        public float2 LastValidPosition { get; internal set; }

        public float2 Velocity { get; internal set; }

        public float2 FacingDirection { get; internal set; }

        public bool IsCarrying { get; internal set; }
    }
}
