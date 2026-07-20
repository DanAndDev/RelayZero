using RelayZero.Arena;
using RelayZero.Foundation;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    internal static class PlayerPairCollisionSolver
    {
        public const int MaximumPairStaticCleanupIterations = 2;

        private const float SeparationEpsilon = 0.00001f;

        public static bool Resolve(
            ref PlayerState playerZero,
            ref PlayerState playerOne,
            float2 playerZeroSweepStart,
            float2 playerOneSweepStart,
            float radius,
            ArenaBakeData arena,
            SimulationTick tick,
            CollisionDiagnosticBuffer diagnostics,
            PlayerCollisionMobility zeroMobility = PlayerCollisionMobility.Movable,
            PlayerCollisionMobility oneMobility = PlayerCollisionMobility.Movable)
        {
            if (!ResolveSweptContact(
                    ref playerZero,
                    ref playerOne,
                    playerZeroSweepStart,
                    playerOneSweepStart,
                    radius,
                    arena,
                    tick,
                    diagnostics,
                    zeroMobility,
                    oneMobility))
            {
                return false;
            }

            PlayerCollisionMobility effectiveZeroMobility = zeroMobility;
            PlayerCollisionMobility effectiveOneMobility = oneMobility;
            for (int iteration = 0; iteration < MaximumPairStaticCleanupIterations; iteration++)
            {
                float2 zeroTarget = playerZero.Position;
                float2 oneTarget = playerOne.Position;
                float2 fallback = playerOne.Velocity - playerZero.Velocity;
                if (!PlayerPairCollision.TryResolveOverlap(
                        ref zeroTarget,
                        ref oneTarget,
                        radius,
                        effectiveZeroMobility,
                        effectiveOneMobility,
                        fallback,
                        out float2 zeroCorrection,
                        out float2 oneCorrection))
                {
                    if (!AreOverlapping(playerZero.Position, playerOne.Position, radius))
                    {
                        return true;
                    }

                    break;
                }

                bool zeroCorrectionBlocked = false;
                if (effectiveZeroMobility == PlayerCollisionMobility.Movable)
                {
                    float2 start = playerZero.Position;
                    if (!ApplyPairCorrection(
                            ref playerZero,
                            zeroCorrection,
                            radius,
                            arena,
                            tick,
                            diagnostics))
                    {
                        return false;
                    }

                    zeroCorrectionBlocked = WasCorrectionBlocked(start, playerZero.Position, zeroCorrection);
                    AddPairDiagnostic(playerZero.Slot, start, zeroCorrection, playerZero.Position, -SafePairNormal(playerZero, playerOne), iteration, diagnostics);
                }

                bool oneCorrectionBlocked = false;
                if (effectiveOneMobility == PlayerCollisionMobility.Movable)
                {
                    float2 start = playerOne.Position;
                    if (!ApplyPairCorrection(
                            ref playerOne,
                            oneCorrection,
                            radius,
                            arena,
                            tick,
                            diagnostics))
                    {
                        return false;
                    }

                    oneCorrectionBlocked = WasCorrectionBlocked(start, playerOne.Position, oneCorrection);
                    AddPairDiagnostic(playerOne.Slot, start, oneCorrection, playerOne.Position, SafePairNormal(playerZero, playerOne), iteration, diagnostics);
                }

                if (!AreOverlapping(playerZero.Position, playerOne.Position, radius))
                {
                    ProjectClosingVelocity(
                        ref playerZero,
                        ref playerOne,
                        effectiveZeroMobility,
                        effectiveOneMobility);
                    return true;
                }

                if (zeroCorrectionBlocked && !oneCorrectionBlocked &&
                    effectiveOneMobility == PlayerCollisionMobility.Movable)
                {
                    effectiveZeroMobility = PlayerCollisionMobility.Immovable;
                }
                else if (oneCorrectionBlocked && !zeroCorrectionBlocked &&
                         effectiveZeroMobility == PlayerCollisionMobility.Movable)
                {
                    effectiveOneMobility = PlayerCollisionMobility.Immovable;
                }
            }

            if (!StaticPlayerCollisionQueries.IsValidPosition(arena, playerZero.LastValidPosition, radius) ||
                !StaticPlayerCollisionQueries.IsValidPosition(arena, playerOne.LastValidPosition, radius) ||
                AreOverlapping(playerZero.LastValidPosition, playerOne.LastValidPosition, radius))
            {
                return false;
            }

            playerZero.Position = playerZero.LastValidPosition;
            playerOne.Position = playerOne.LastValidPosition;
            playerZero.Velocity = float2.zero;
            playerOne.Velocity = float2.zero;
            diagnostics.Add(
                playerZero.Slot,
                CollisionDiagnosticKind.Recovery,
                ArenaElementId.None,
                0f,
                playerZero.Position,
                float2.zero,
                playerZero.Position,
                float2.zero,
                float2.zero,
                MaximumPairStaticCleanupIterations);
            diagnostics.Add(
                playerOne.Slot,
                CollisionDiagnosticKind.Recovery,
                ArenaElementId.None,
                0f,
                playerOne.Position,
                float2.zero,
                playerOne.Position,
                float2.zero,
                float2.zero,
                MaximumPairStaticCleanupIterations);
            return true;
        }

        private static bool ResolveSweptContact(
            ref PlayerState playerZero,
            ref PlayerState playerOne,
            float2 playerZeroSweepStart,
            float2 playerOneSweepStart,
            float radius,
            ArenaBakeData arena,
            SimulationTick tick,
            CollisionDiagnosticBuffer diagnostics,
            PlayerCollisionMobility zeroMobility,
            PlayerCollisionMobility oneMobility)
        {
            float2 zeroDisplacement = playerZero.Position - playerZeroSweepStart;
            float2 oneDisplacement = playerOne.Position - playerOneSweepStart;
            if (!PlayerPairCollision.TrySweep(
                    playerZeroSweepStart,
                    zeroDisplacement,
                    playerOneSweepStart,
                    oneDisplacement,
                    radius,
                    out PlayerPairSweepHit hit) ||
                hit.StartedOverlapping)
            {
                return true;
            }

            float2 relativeDisplacement = oneDisplacement - zeroDisplacement;
            if (math.dot(relativeDisplacement, hit.Normal) >= -SeparationEpsilon)
            {
                return true;
            }

            float relativeLength = math.length(relativeDisplacement);
            float skinFraction = relativeLength > SeparationEpsilon
                ? SeparationEpsilon / relativeLength
                : 0f;
            float safeTime = math.max(0f, hit.Time - skinFraction);
            float2 zeroProvisional = playerZero.Position;
            float2 oneProvisional = playerOne.Position;
            playerZero.Position = playerZeroSweepStart;
            playerOne.Position = playerOneSweepStart;

            if (!StaticPlayerCollisionSolver.Resolve(
                    ref playerZero,
                    zeroDisplacement * safeTime,
                    radius,
                    arena,
                    tick,
                    diagnostics) ||
                !StaticPlayerCollisionSolver.Resolve(
                    ref playerOne,
                    oneDisplacement * safeTime,
                    radius,
                    arena,
                    tick,
                    diagnostics))
            {
                return false;
            }

            AddPairDiagnostic(
                playerZero.Slot,
                zeroProvisional,
                playerZero.Position - zeroProvisional,
                playerZero.Position,
                -hit.Normal,
                0,
                diagnostics);
            AddPairDiagnostic(
                playerOne.Slot,
                oneProvisional,
                playerOne.Position - oneProvisional,
                playerOne.Position,
                hit.Normal,
                0,
                diagnostics);
            ProjectClosingVelocity(
                ref playerZero,
                ref playerOne,
                hit.Normal,
                zeroMobility,
                oneMobility);
            return true;
        }

        private static bool ApplyPairCorrection(
            ref PlayerState player,
            float2 correction,
            float radius,
            ArenaBakeData arena,
            SimulationTick tick,
            CollisionDiagnosticBuffer diagnostics)
        {
            if (math.lengthsq(correction) <=
                StaticPlayerCollisionSolver.MinimumRemainingDisplacementMeters *
                StaticPlayerCollisionSolver.MinimumRemainingDisplacementMeters)
            {
                float2 target = player.Position + correction;
                if (StaticPlayerCollisionQueries.IsValidPosition(arena, target, radius))
                {
                    player.Position = target;
                }

                return true;
            }

            return StaticPlayerCollisionSolver.Resolve(
                ref player,
                correction,
                radius,
                arena,
                tick,
                diagnostics);
        }

        private static bool WasCorrectionBlocked(float2 start, float2 position, float2 correction)
        {
            float requestedLengthSquared = math.lengthsq(correction);
            if (requestedLengthSquared <= SeparationEpsilon * SeparationEpsilon)
            {
                return false;
            }

            float requestedLength = math.sqrt(requestedLengthSquared);
            float achievedAlongCorrection = math.dot(
                position - start,
                correction / requestedLength);
            return achievedAlongCorrection < requestedLength - SeparationEpsilon;
        }

        private static void AddPairDiagnostic(
            PlayerSlot slot,
            float2 start,
            float2 correction,
            float2 position,
            float2 normal,
            int iteration,
            CollisionDiagnosticBuffer diagnostics)
        {
            diagnostics.Add(
                slot,
                CollisionDiagnosticKind.PlayerPairCorrection,
                ArenaElementId.None,
                0f,
                start,
                correction,
                position,
                normal,
                float2.zero,
                iteration);
        }

        private static bool AreOverlapping(float2 zero, float2 one, float radius)
        {
            float minimumDistance = radius * 2f - SeparationEpsilon;
            return math.lengthsq(one - zero) < minimumDistance * minimumDistance;
        }

        private static float2 SafePairNormal(in PlayerState zero, in PlayerState one)
        {
            float2 delta = one.Position - zero.Position;
            return math.normalizesafe(delta, new float2(1f, 0f));
        }

        private static void ProjectClosingVelocity(
            ref PlayerState zero,
            ref PlayerState one,
            PlayerCollisionMobility zeroMobility,
            PlayerCollisionMobility oneMobility)
        {
            float2 normal = SafePairNormal(in zero, in one);
            ProjectClosingVelocity(ref zero, ref one, normal, zeroMobility, oneMobility);
        }

        private static void ProjectClosingVelocity(
            ref PlayerState zero,
            ref PlayerState one,
            float2 normal,
            PlayerCollisionMobility zeroMobility,
            PlayerCollisionMobility oneMobility)
        {
            normal = math.normalizesafe(normal, new float2(1f, 0f));
            float closingSpeed = math.dot(one.Velocity - zero.Velocity, normal);
            if (closingSpeed >= 0f)
            {
                return;
            }

            bool zeroMovable = zeroMobility == PlayerCollisionMobility.Movable;
            bool oneMovable = oneMobility == PlayerCollisionMobility.Movable;
            if (zeroMovable && oneMovable)
            {
                float impulse = -closingSpeed * 0.5f;
                zero.Velocity -= normal * impulse;
                one.Velocity += normal * impulse;
            }
            else if (zeroMovable)
            {
                zero.Velocity += normal * closingSpeed;
            }
            else if (oneMovable)
            {
                one.Velocity -= normal * closingSpeed;
            }
        }
    }
}
