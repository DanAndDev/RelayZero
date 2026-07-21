using RelayZero.Arena;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    internal static class StaticCoreCollisionSolver
    {
        public const float CollisionSkinMeters = 0.001f;
        public const int MaximumSweepIterations = 4;

        private const float HitTimeEpsilon = 0.000001f;
        private const float InwardEpsilon = 0.0000001f;
        private const float MinimumTimeSeconds = 0.000001f;

        public static void Step(
            ref CoreRuntimeState core,
            in CoreConfig config,
            ArenaBakeData arena,
            CoreDiagnosticBuffer diagnostics)
        {
            float speedSquared = math.lengthsq(core.Velocity);
            if (speedSquared > config.MaximumLooseSpeedSquared)
            {
                core.Velocity *= config.MaximumLooseSpeedMetersPerSecond * math.rsqrt(speedSquared);
            }

            core.Velocity *= config.PerTickDragMultiplier;
            float2 position = core.Position;
            float remainingSeconds = (float)Foundation.SimulationTime.TickDurationSeconds;
            for (int iteration = 0;
                 iteration < MaximumSweepIterations && remainingSeconds > MinimumTimeSeconds;
                 iteration++)
            {
                float2 displacement = core.Velocity * remainingSeconds;
                if (math.lengthsq(displacement) <= PlanarCircleCollision.GeometryEpsilon *
                    PlanarCircleCollision.GeometryEpsilon)
                {
                    break;
                }

                if (!TryFindEarliestHit(arena, position, displacement, config.RadiusMeters, out StaticHit hit))
                {
                    position += displacement;
                    remainingSeconds = 0f;
                    break;
                }

                if (hit.SweepHit.StartedOverlapping)
                {
                    position += hit.SweepHit.Normal *
                        (hit.SweepHit.PenetrationDepth + CollisionSkinMeters);
                }
                else
                {
                    float displacementLength = math.length(displacement);
                    float skinFraction = displacementLength > 0f
                        ? CollisionSkinMeters / displacementLength
                        : 0f;
                    float safeTime = math.max(0f, hit.SweepHit.Time - skinFraction);
                    position += displacement * safeTime;
                    remainingSeconds *= 1f - hit.SweepHit.Time;
                }

                float2 bouncedVelocity = core.Velocity;
                ApplyBounce(ref bouncedVelocity, hit.SweepHit.Normal, in config);
                core.Velocity = bouncedVelocity;
                diagnostics.Add(
                    CoreDiagnosticKind.Bounce,
                    core.Mode,
                    position,
                    core.Velocity,
                    hit.SweepHit.Normal,
                    hit.ElementId);
            }

            core.Position = position;
        }

        private static void ApplyBounce(ref float2 velocity, float2 normal, in CoreConfig config)
        {
            float normalSpeed = math.dot(velocity, normal);
            if (normalSpeed >= 0f)
            {
                return;
            }

            float2 normalVelocity = normal * normalSpeed;
            float2 tangentVelocity = velocity - normalVelocity;
            velocity = tangentVelocity * config.TangentialVelocityRetention -
                normalVelocity * config.Restitution;
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
                if (!SweepBoundsOverlap(start, end, wall.Bounds.Expanded(radius)))
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
