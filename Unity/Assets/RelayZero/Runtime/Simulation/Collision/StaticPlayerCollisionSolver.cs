using System;
using RelayZero.Arena;
using RelayZero.Foundation;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    public static class StaticPlayerCollisionSolver
    {
        public const float CollisionSkinMeters = 0.002f;
        public const int MaximumSweepIterations = 4;
        public const float MinimumRemainingDisplacementMeters = 0.0001f;

        private const float HitTimeEpsilon = 0.000001f;
        private const float InwardEpsilon = 0.0000001f;

        internal static bool Resolve(
            ref PlayerState player,
            float2 requestedDisplacement,
            float radius,
            ArenaBakeData arena,
            SimulationTick tick,
            CollisionDiagnosticBuffer diagnostics)
        {
            if (!math.all(math.isfinite(player.Position)) ||
                !math.all(math.isfinite(player.Velocity)) ||
                !math.all(math.isfinite(requestedDisplacement)) ||
                !StaticPlayerCollisionQueries.IsValidPosition(arena, player.Position, radius))
            {
                return Recover(
                    ref player,
                    player.Position,
                    requestedDisplacement,
                    radius,
                    arena,
                    tick,
                    diagnostics,
                    0);
            }

            float2 position = player.Position;
            float2 remaining = requestedDisplacement;
            float minimumRemainingSquared = MinimumRemainingDisplacementMeters * MinimumRemainingDisplacementMeters;
            int iteration = 0;
            for (; iteration < MaximumSweepIterations && math.lengthsq(remaining) > minimumRemainingSquared; iteration++)
            {
                if (!TryFindEarliestHit(arena, position, remaining, radius, out StaticHit hit))
                {
                    position += remaining;
                    remaining = float2.zero;
                    break;
                }

                float2 sweepStart = position;
                float2 sweepDisplacement = remaining;
                if (hit.SweepHit.StartedOverlapping)
                {
                    position += hit.SweepHit.Normal *
                        (hit.SweepHit.PenetrationDepth + CollisionSkinMeters);
                }
                else
                {
                    float remainingLength = math.length(remaining);
                    float skinFraction = remainingLength > 0f
                        ? CollisionSkinMeters / remainingLength
                        : 0f;
                    float safeTime = math.max(0f, hit.SweepHit.Time - skinFraction);
                    position += remaining * safeTime;
                    remaining *= 1f - safeTime;
                }

                RemoveInwardComponent(ref remaining, hit.SweepHit.Normal);
                float2 velocity = player.Velocity;
                RemoveInwardComponent(ref velocity, hit.SweepHit.Normal);
                player.Velocity = velocity;
                diagnostics.Add(
                    player.Slot,
                    CollisionDiagnosticKind.StaticImpact,
                    hit.ElementId,
                    hit.SweepHit.Time,
                    sweepStart,
                    sweepDisplacement,
                    hit.SweepHit.Position,
                    hit.SweepHit.Normal,
                    remaining,
                    iteration);
            }

            if (math.lengthsq(remaining) > minimumRemainingSquared)
            {
                diagnostics.Add(
                    player.Slot,
                    CollisionDiagnosticKind.IterationLimit,
                    ArenaElementId.None,
                    1f,
                    position,
                    requestedDisplacement,
                    position,
                    float2.zero,
                    remaining,
                    iteration);
                return Recover(
                    ref player,
                    position,
                    requestedDisplacement,
                    radius,
                    arena,
                    tick,
                    diagnostics,
                    iteration);
            }

            position = ClampToPlayableBounds(position, radius, arena.Bounds.Bounds);
            if (!StaticPlayerCollisionQueries.IsValidPosition(arena, position, radius))
            {
                return Recover(
                    ref player,
                    position,
                    requestedDisplacement,
                    radius,
                    arena,
                    tick,
                    diagnostics,
                    iteration);
            }

            player.Position = position;
            return true;
        }

        private static bool TryFindEarliestHit(
            ArenaBakeData arena,
            float2 start,
            float2 displacement,
            float radius,
            out StaticHit earliest)
        {
            earliest = default;
            bool found = false;
            float2 end = start + displacement;

            for (int wallIndex = 0; wallIndex < arena.Walls.Count; wallIndex++)
            {
                BakedWall wall = arena.Walls[wallIndex];
                ArenaAabb broadPhaseBounds = wall.Bounds.Expanded(radius);
                if (!SweepBoundsOverlap(start, end, broadPhaseBounds))
                {
                    continue;
                }

                float combinedRadius = radius + wall.Thickness * 0.5f;
                if (PlanarCircleCollision.TrySweepSegmentCapsule(
                        start,
                        displacement,
                        wall.Start,
                        wall.End,
                        combinedRadius,
                        out PlanarCircleSweepHit hit) &&
                    IsInwardHit(displacement, in hit))
                {
                    Consider(in hit, wall.Id, 0, ref found, ref earliest);
                }
            }

            for (int obstacleIndex = 0; obstacleIndex < arena.Obstacles.Count; obstacleIndex++)
            {
                BakedConvexObstacle obstacle = arena.Obstacles[obstacleIndex];
                if (!SweepBoundsOverlap(start, end, obstacle.Bounds.Expanded(radius)))
                {
                    continue;
                }

                for (int edgeIndex = 0; edgeIndex < obstacle.Vertices.Count; edgeIndex++)
                {
                    float2 edgeStart = obstacle.Vertices[edgeIndex];
                    float2 edgeEnd = obstacle.Vertices[(edgeIndex + 1) % obstacle.Vertices.Count];
                    if (PlanarCircleCollision.TrySweepSegmentCapsule(
                            start,
                            displacement,
                            edgeStart,
                            edgeEnd,
                            radius,
                            out PlanarCircleSweepHit hit) &&
                        IsInwardHit(displacement, in hit))
                    {
                        Consider(in hit, obstacle.Id, edgeIndex, ref found, ref earliest);
                    }
                }
            }

            return found;
        }

        private static bool IsInwardHit(float2 displacement, in PlanarCircleSweepHit hit)
        {
            return hit.StartedOverlapping || math.dot(displacement, hit.Normal) < -InwardEpsilon;
        }

        private static void Consider(
            in PlanarCircleSweepHit hit,
            ArenaElementId elementId,
            int edgeIndex,
            ref bool found,
            ref StaticHit earliest)
        {
            if (!found || hit.Time < earliest.SweepHit.Time - HitTimeEpsilon ||
                (math.abs(hit.Time - earliest.SweepHit.Time) <= HitTimeEpsilon &&
                 (elementId.Value < earliest.ElementId.Value ||
                  (elementId == earliest.ElementId && edgeIndex < earliest.EdgeIndex))))
            {
                earliest = new StaticHit(in hit, elementId, edgeIndex);
                found = true;
            }
        }

        private static bool Recover(
            ref PlayerState player,
            float2 attemptedPosition,
            float2 requestedDisplacement,
            float radius,
            ArenaBakeData arena,
            SimulationTick tick,
            CollisionDiagnosticBuffer diagnostics,
            int iteration)
        {
            if (!StaticPlayerCollisionQueries.IsValidPosition(arena, player.LastValidPosition, radius))
            {
                return false;
            }

            player.Position = player.LastValidPosition;
            player.Velocity = float2.zero;
            diagnostics.Add(
                player.Slot,
                CollisionDiagnosticKind.Recovery,
                ArenaElementId.None,
                0f,
                attemptedPosition,
                requestedDisplacement,
                player.Position,
                float2.zero,
                float2.zero,
                iteration);
            return true;
        }

        private static void RemoveInwardComponent(ref float2 value, float2 normal)
        {
            float inward = math.dot(value, normal);
            if (inward < 0f)
            {
                value -= normal * inward;
            }
        }

        private static float2 ClampToPlayableBounds(float2 position, float radius, ArenaAabb bounds)
        {
            float2 minimum = bounds.Minimum + new float2(radius);
            float2 maximum = bounds.Maximum - new float2(radius);
            if (math.any(minimum > maximum))
            {
                throw new InvalidOperationException("Arena bounds are smaller than the authored player diameter.");
            }

            return math.clamp(position, minimum, maximum);
        }

        private static bool SweepBoundsOverlap(float2 start, float2 end, ArenaAabb bounds)
        {
            float2 minimum = math.min(start, end);
            float2 maximum = math.max(start, end);
            return maximum.x >= bounds.Minimum.x && maximum.y >= bounds.Minimum.y &&
                   minimum.x <= bounds.Maximum.x && minimum.y <= bounds.Maximum.y;
        }

        private readonly struct StaticHit
        {
            public StaticHit(in PlanarCircleSweepHit sweepHit, ArenaElementId elementId, int edgeIndex)
            {
                SweepHit = sweepHit;
                ElementId = elementId;
                EdgeIndex = edgeIndex;
            }

            public PlanarCircleSweepHit SweepHit { get; }

            public ArenaElementId ElementId { get; }

            public int EdgeIndex { get; }
        }
    }
}
