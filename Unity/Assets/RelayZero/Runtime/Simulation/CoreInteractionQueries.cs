using System;
using RelayZero.Arena;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    internal static class CoreInteractionQueries
    {
        private const float InwardEpsilon = 0.0000001f;

        public static bool IsPickupLineClear(ArenaBakeData arena, float2 playerPosition, float2 corePosition)
        {
            return IsPathClear(arena, playerPosition, corePosition, 0f);
        }

        public static bool IsPathClear(ArenaBakeData arena, float2 start, float2 end, float clearanceRadius)
        {
            if (arena == null)
            {
                throw new ArgumentNullException(nameof(arena));
            }

            if (!math.all(math.isfinite(start)) || !math.all(math.isfinite(end)) ||
                !math.isfinite(clearanceRadius) || clearanceRadius < 0f)
            {
                return false;
            }

            float2 displacement = end - start;
            if (math.lengthsq(displacement) <= PlanarCircleCollision.GeometryEpsilon *
                PlanarCircleCollision.GeometryEpsilon)
            {
                return true;
            }

            for (int wallIndex = 0; wallIndex < arena.Walls.Count; wallIndex++)
            {
                BakedWall wall = arena.Walls[wallIndex];
                float radius = clearanceRadius + wall.Thickness * 0.5f;
                if (radius <= 0f)
                {
                    if (ArenaGeometry.SegmentsIntersect(start, end, wall.Start, wall.End))
                    {
                        return false;
                    }

                    continue;
                }

                if (PlanarCircleCollision.TrySweepSegmentCapsule(
                        start,
                        displacement,
                        wall.Start,
                        wall.End,
                        radius,
                        out PlanarCircleSweepHit wallHit) &&
                    (wallHit.StartedOverlapping || math.dot(displacement, wallHit.Normal) < -InwardEpsilon))
                {
                    return false;
                }
            }

            for (int obstacleIndex = 0; obstacleIndex < arena.Obstacles.Count; obstacleIndex++)
            {
                BakedConvexObstacle obstacle = arena.Obstacles[obstacleIndex];
                if (clearanceRadius == 0f &&
                    ArenaGeometry.SegmentIntersectsConvexPolygon(start, end, obstacle.Vertices))
                {
                    return false;
                }

                for (int edgeIndex = 0; edgeIndex < obstacle.Vertices.Count; edgeIndex++)
                {
                    float2 edgeStart = obstacle.Vertices[edgeIndex];
                    float2 edgeEnd = obstacle.Vertices[(edgeIndex + 1) % obstacle.Vertices.Count];
                    if (clearanceRadius > 0f &&
                        PlanarCircleCollision.TrySweepSegmentCapsule(
                            start,
                            displacement,
                            edgeStart,
                            edgeEnd,
                            clearanceRadius,
                            out PlanarCircleSweepHit obstacleHit) &&
                        (obstacleHit.StartedOverlapping ||
                         math.dot(displacement, obstacleHit.Normal) < -InwardEpsilon))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool IsCorePositionValid(ArenaBakeData arena, float2 position, float radius)
        {
            if (!StaticPlayerCollisionQueries.IsValidPosition(arena, position, radius))
            {
                return false;
            }

            for (int gateIndex = 0; gateIndex < arena.Gates.Count; gateIndex++)
            {
                BakedShockGate gate = arena.Gates[gateIndex];
                ArenaAabb expanded = gate.Bounds.Expanded(radius);
                if (expanded.Contains(position))
                {
                    return false;
                }
            }

            for (int circleIndex = 0; circleIndex < arena.Circles.Count; circleIndex++)
            {
                BakedCircle circle = arena.Circles[circleIndex];
                if (circle.Kind != BakedElementKind.Terminal)
                {
                    continue;
                }

                float combinedRadius = radius + circle.Radius;
                if (math.distancesq(position, circle.Center) < combinedRadius * combinedRadius)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
