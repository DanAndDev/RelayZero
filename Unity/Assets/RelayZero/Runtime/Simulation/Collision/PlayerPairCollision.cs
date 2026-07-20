using System;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    public enum PlayerCollisionMobility : byte
    {
        Movable,
        Immovable,
    }

    public readonly struct PlayerPairSweepHit
    {
        internal PlayerPairSweepHit(float time, float2 normal, bool startedOverlapping)
        {
            Time = time;
            Normal = normal;
            StartedOverlapping = startedOverlapping;
        }

        public float Time { get; }

        public float2 Normal { get; }

        public bool StartedOverlapping { get; }
    }

    public static class PlayerPairCollision
    {
        private const float SeparationEpsilon = 0.00001f;

        public static bool TrySweep(
            float2 playerZeroStart,
            float2 playerZeroDisplacement,
            float2 playerOneStart,
            float2 playerOneDisplacement,
            float radius,
            out PlayerPairSweepHit hit)
        {
            ValidateSweepInputs(
                playerZeroStart,
                playerZeroDisplacement,
                playerOneStart,
                playerOneDisplacement,
                radius);

            float2 relativeStart = playerOneStart - playerZeroStart;
            float2 relativeDisplacement = playerOneDisplacement - playerZeroDisplacement;
            float combinedRadius = radius * 2f;
            if (!math.all(math.isfinite(relativeStart)) ||
                !math.all(math.isfinite(relativeDisplacement)) ||
                !math.isfinite(combinedRadius))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerZeroDisplacement),
                    "Pair sweep inputs exceed the supported finite geometry range.");
            }

            if (!PlanarCircleCollision.TrySweepSegmentCapsule(
                    relativeStart,
                    relativeDisplacement,
                    float2.zero,
                    float2.zero,
                    combinedRadius,
                    out PlanarCircleSweepHit relativeHit))
            {
                hit = default;
                return false;
            }

            hit = new PlayerPairSweepHit(
                relativeHit.Time,
                relativeHit.Normal,
                relativeHit.StartedOverlapping);
            return true;
        }

        public static bool TryResolveOverlap(
            ref float2 playerZeroPosition,
            ref float2 playerOnePosition,
            float radius,
            PlayerCollisionMobility playerZeroMobility,
            PlayerCollisionMobility playerOneMobility,
            float2 fallbackDirection,
            out float2 playerZeroCorrection,
            out float2 playerOneCorrection)
        {
            ValidateInputs(playerZeroPosition, playerOnePosition, radius, fallbackDirection);
            playerZeroCorrection = float2.zero;
            playerOneCorrection = float2.zero;

            float2 delta = playerOnePosition - playerZeroPosition;
            float distanceSquared = math.lengthsq(delta);
            float diameter = radius * 2f;
            if (distanceSquared >= (diameter - SeparationEpsilon) * (diameter - SeparationEpsilon))
            {
                return false;
            }

            bool zeroMovable = playerZeroMobility == PlayerCollisionMobility.Movable;
            bool oneMovable = playerOneMobility == PlayerCollisionMobility.Movable;
            if (!zeroMovable && !oneMovable)
            {
                return false;
            }

            float distance = math.sqrt(math.max(0f, distanceSquared));
            float2 normal;
            if (distance > SeparationEpsilon)
            {
                normal = delta / distance;
            }
            else
            {
                float fallbackLengthSquared = math.lengthsq(fallbackDirection);
                normal = fallbackLengthSquared > SeparationEpsilon * SeparationEpsilon
                    ? fallbackDirection * math.rsqrt(fallbackLengthSquared)
                    : new float2(1f, 0f);
            }

            float overlap = diameter - distance + SeparationEpsilon;
            if (zeroMovable && oneMovable)
            {
                playerZeroCorrection = -normal * (overlap * 0.5f);
                playerOneCorrection = normal * (overlap * 0.5f);
            }
            else if (zeroMovable)
            {
                playerZeroCorrection = -normal * overlap;
            }
            else
            {
                playerOneCorrection = normal * overlap;
            }

            playerZeroPosition += playerZeroCorrection;
            playerOnePosition += playerOneCorrection;
            return true;
        }

        private static void ValidateInputs(
            float2 playerZeroPosition,
            float2 playerOnePosition,
            float radius,
            float2 fallbackDirection)
        {
            if (!math.all(math.isfinite(playerZeroPosition)) ||
                !math.all(math.isfinite(playerOnePosition)) ||
                !math.all(math.isfinite(fallbackDirection)) ||
                !math.isfinite(radius) || radius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(playerZeroPosition), "Pair collision inputs must be finite and valid.");
            }
        }

        private static void ValidateSweepInputs(
            float2 playerZeroStart,
            float2 playerZeroDisplacement,
            float2 playerOneStart,
            float2 playerOneDisplacement,
            float radius)
        {
            if (!math.all(math.isfinite(playerZeroStart)))
            {
                throw new ArgumentOutOfRangeException(nameof(playerZeroStart), "Pair sweep start must be finite.");
            }

            if (!math.all(math.isfinite(playerZeroDisplacement)))
            {
                throw new ArgumentOutOfRangeException(nameof(playerZeroDisplacement), "Pair sweep displacement must be finite.");
            }

            if (!math.all(math.isfinite(playerOneStart)))
            {
                throw new ArgumentOutOfRangeException(nameof(playerOneStart), "Pair sweep start must be finite.");
            }

            if (!math.all(math.isfinite(playerOneDisplacement)))
            {
                throw new ArgumentOutOfRangeException(nameof(playerOneDisplacement), "Pair sweep displacement must be finite.");
            }

            if (!math.isfinite(radius) || radius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(radius), radius, "Player radius must be finite and positive.");
            }

            if (!math.all(math.isfinite(playerZeroStart + playerZeroDisplacement)) ||
                !math.all(math.isfinite(playerOneStart + playerOneDisplacement)))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerZeroDisplacement),
                    "Pair sweep inputs exceed the supported finite geometry range.");
            }
        }
    }
}
