using System;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    public readonly struct CoreForcedDropRequest
    {
        public CoreForcedDropRequest(CoreDropReason reason, float2 initialVelocity, uint actionId)
        {
            if ((uint)reason < (uint)CoreDropReason.Pulse ||
                (uint)reason > (uint)CoreDropReason.Recovery)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reason),
                    reason,
                    "A forced core drop requires a concrete non-manual reason.");
            }

            if (!math.all(math.isfinite(initialVelocity)))
            {
                throw new ArgumentOutOfRangeException(nameof(initialVelocity), "Core drop velocity must be finite.");
            }

            Reason = reason;
            InitialVelocity = initialVelocity;
            ActionId = actionId;
        }

        public CoreDropReason Reason { get; }

        public float2 InitialVelocity { get; }

        public uint ActionId { get; }

        internal bool IsValid => (uint)Reason >= (uint)CoreDropReason.Pulse &&
            (uint)Reason <= (uint)CoreDropReason.Recovery &&
            math.all(math.isfinite(InitialVelocity));
    }
}
