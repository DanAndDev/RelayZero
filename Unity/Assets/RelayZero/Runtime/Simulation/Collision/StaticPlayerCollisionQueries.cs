using System;
using System.Collections.Generic;
using RelayZero.Arena;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    public static class StaticPlayerCollisionQueries
    {
        private const float ValidityEpsilon = PlanarCircleCollision.GeometryEpsilon;
        private const float PerimeterJoinEpsilon = 0.01f;
        private const float PerimeterJoinEpsilonSquared = PerimeterJoinEpsilon * PerimeterJoinEpsilon;

        public static bool IsValidPosition(ArenaBakeData arena, float2 position, float radius)
        {
            if (arena == null)
            {
                throw new ArgumentNullException(nameof(arena));
            }

            if (!math.all(math.isfinite(position)) || !math.isfinite(radius) || radius <= 0f)
            {
                return false;
            }

            ArenaAabb bounds = arena.Bounds.Bounds;
            if (position.x < bounds.Minimum.x + radius - ValidityEpsilon ||
                position.y < bounds.Minimum.y + radius - ValidityEpsilon ||
                position.x > bounds.Maximum.x - radius + ValidityEpsilon ||
                position.y > bounds.Maximum.y - radius + ValidityEpsilon)
            {
                return false;
            }

            if (WallsFormSingleClosedPerimeter(arena.Walls) &&
                !IsInsideWallPerimeter(arena.Walls, position))
            {
                return false;
            }

            for (int wallIndex = 0; wallIndex < arena.Walls.Count; wallIndex++)
            {
                BakedWall wall = arena.Walls[wallIndex];
                float combinedRadius = radius + wall.Thickness * 0.5f;
                float minimumDistance = math.max(0f, combinedRadius - ValidityEpsilon);
                if (ArenaGeometry.DistanceSquaredToSegment(position, wall.Start, wall.End) <
                    minimumDistance * minimumDistance)
                {
                    return false;
                }
            }

            for (int obstacleIndex = 0; obstacleIndex < arena.Obstacles.Count; obstacleIndex++)
            {
                BakedConvexObstacle obstacle = arena.Obstacles[obstacleIndex];
                if (ArenaGeometry.PointInConvexPolygon(position, obstacle.Vertices, ValidityEpsilon))
                {
                    return false;
                }

                float minimumDistance = math.max(0f, radius - ValidityEpsilon);
                float minimumDistanceSquared = minimumDistance * minimumDistance;
                for (int edgeIndex = 0; edgeIndex < obstacle.Vertices.Count; edgeIndex++)
                {
                    float2 start = obstacle.Vertices[edgeIndex];
                    float2 end = obstacle.Vertices[(edgeIndex + 1) % obstacle.Vertices.Count];
                    if (ArenaGeometry.DistanceSquaredToSegment(position, start, end) < minimumDistanceSquared)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool WallsFormSingleClosedPerimeter(IReadOnlyList<BakedWall> walls)
        {
            if (walls.Count < 3)
            {
                return false;
            }

            for (int wallIndex = 0; wallIndex < walls.Count; wallIndex++)
            {
                BakedWall wall = walls[wallIndex];
                if (CountEndpointConnections(walls, wallIndex, wall.Start) != 1 ||
                    CountEndpointConnections(walls, wallIndex, wall.End) != 1)
                {
                    return false;
                }
            }

            float2 perimeterStart = walls[0].Start;
            float2 currentEndpoint = walls[0].End;
            int currentWallIndex = 0;
            for (int traversed = 1; traversed < walls.Count; traversed++)
            {
                int nextWallIndex = -1;
                bool matchedNextStart = false;
                for (int wallIndex = 0; wallIndex < walls.Count; wallIndex++)
                {
                    if (wallIndex == currentWallIndex)
                    {
                        continue;
                    }

                    BakedWall candidate = walls[wallIndex];
                    bool matchesStart = EndpointsMatch(currentEndpoint, candidate.Start);
                    bool matchesEnd = EndpointsMatch(currentEndpoint, candidate.End);
                    if (!matchesStart && !matchesEnd)
                    {
                        continue;
                    }

                    if (nextWallIndex >= 0)
                    {
                        return false;
                    }

                    nextWallIndex = wallIndex;
                    matchedNextStart = matchesStart;
                }

                if (nextWallIndex < 0)
                {
                    return false;
                }

                BakedWall nextWall = walls[nextWallIndex];
                currentEndpoint = matchedNextStart ? nextWall.End : nextWall.Start;
                currentWallIndex = nextWallIndex;
                if (EndpointsMatch(currentEndpoint, perimeterStart))
                {
                    return traversed == walls.Count - 1;
                }
            }

            return false;
        }

        private static int CountEndpointConnections(
            IReadOnlyList<BakedWall> walls,
            int excludedWallIndex,
            float2 endpoint)
        {
            int count = 0;
            for (int wallIndex = 0; wallIndex < walls.Count; wallIndex++)
            {
                if (wallIndex == excludedWallIndex)
                {
                    continue;
                }

                BakedWall wall = walls[wallIndex];
                if (EndpointsMatch(endpoint, wall.Start))
                {
                    count++;
                }

                if (EndpointsMatch(endpoint, wall.End))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool EndpointsMatch(float2 left, float2 right)
        {
            return math.distancesq(left, right) <= PerimeterJoinEpsilonSquared;
        }

        private static bool IsInsideWallPerimeter(IReadOnlyList<BakedWall> walls, float2 position)
        {
            bool inside = false;
            for (int wallIndex = 0; wallIndex < walls.Count; wallIndex++)
            {
                BakedWall wall = walls[wallIndex];
                double startY = wall.Start.y;
                double endY = wall.End.y;
                double pointY = position.y;
                if ((startY > pointY) == (endY > pointY))
                {
                    continue;
                }

                double intersectionX = wall.Start.x +
                    (pointY - startY) * (wall.End.x - wall.Start.x) / (endY - startY);
                if (position.x < intersectionX)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        public static bool TryRecoverPosition(
            ArenaBakeData arena,
            float2 candidate,
            float2 lastValidPosition,
            float2 spawnFallback,
            float radius,
            out float2 recoveredPosition)
        {
            if (IsValidPosition(arena, candidate, radius))
            {
                recoveredPosition = candidate;
                return true;
            }

            if (IsValidPosition(arena, lastValidPosition, radius))
            {
                recoveredPosition = lastValidPosition;
                return true;
            }

            if (IsValidPosition(arena, spawnFallback, radius))
            {
                recoveredPosition = spawnFallback;
                return true;
            }

            recoveredPosition = float2.zero;
            return false;
        }
    }
}
