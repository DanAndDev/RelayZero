using System;
using System.Collections.Generic;
using System.Linq;
using RelayZero.Arena;
using RelayZero.Arena.Authoring;
using RelayZero.Arena.Baking;
using Unity.Mathematics;
using UnityEngine;

namespace RelayZero.Editor.Arena
{
    public static class ArenaValidator
    {
        private const float PositionTolerance = 0.01f;
        private const float DistanceParityTolerance = 0.02f;
        private const float GridStep = 0.5f;

        public static ArenaValidationResult Validate(ArenaAuthoringSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            ArenaValidationReport report = new ArenaValidationReport();
            ValidateStableIds(snapshot, report);
            ValidateRequiredAuthoring(snapshot, report);
            ValidateAuthoringGeometry(snapshot, report);
            if (report.FailedCount > 0)
            {
                return new ArenaValidationResult(report, null, null, string.Empty);
            }

            ArenaBakePayload payload;
            try
            {
                payload = ArenaBakeCompiler.Compile(snapshot);
                report.Add("ARENA-004", "Authoring compiles", true, "Canonical geometry and precomputed data created.");
            }
            catch (Exception exception)
            {
                report.Add("ARENA-004", "Authoring compiles", false, exception.Message);
                return new ArenaValidationResult(report, null, null, string.Empty);
            }

            ArenaBakeData draft = payload.ToRuntimeData(string.Empty);
            string hash = ArenaBakeHasher.Compute(draft);
            ArenaBakeData data = payload.ToRuntimeData(hash);

            ValidateSymmetry(snapshot, report);
            ValidateStrategicDistanceParity(data, report);
            ValidateNavigation(data, report);
            ValidateCameraContainment(data, report);
            ValidateDisplacementTargets(data, report);
            ValidateOverlapsAndForbiddenVolumes(data, report);
            ValidateBarrierSafety(data, report);
            ValidateCoreReachability(data, report);
            report.Add(
                "ARENA-013",
                "Stable bake hash",
                hash.Length == 64 && hash.All(IsLowerHex),
                hash);

            return new ArenaValidationResult(report, payload, data, hash);
        }

        private static void ValidateStableIds(ArenaAuthoringSnapshot snapshot, ArenaValidationReport report)
        {
            string[] empty = snapshot.Elements
                .Where(element => string.IsNullOrWhiteSpace(element.StableId))
                .Select(element => element.name)
                .ToArray();
            string[] duplicates = snapshot.Elements
                .Where(element => !string.IsNullOrWhiteSpace(element.StableId))
                .GroupBy(element => element.StableId, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();
            string[] collisions = snapshot.Elements
                .Where(element => !string.IsNullOrWhiteSpace(element.StableId))
                .GroupBy(element => ArenaElementId.FromStableId(element.StableId).Value)
                .Where(group => group.Select(element => element.StableId).Distinct(StringComparer.Ordinal).Count() > 1)
                .Select(group => group.Key.ToString())
                .ToArray();
            bool passed = empty.Length == 0 && duplicates.Length == 0 && collisions.Length == 0;
            report.Add(
                "ARENA-001",
                "Unique stable IDs",
                passed,
                passed
                    ? snapshot.Elements.Length + " unique stable IDs and deterministic ushort IDs."
                    : "Empty: " + Join(empty) + "; duplicates: " + Join(duplicates) + "; ID collisions: " + Join(collisions));
        }

        private static void ValidateRequiredAuthoring(ArenaAuthoringSnapshot snapshot, ArenaValidationReport report)
        {
            List<string> failures = new List<string>();
            Expect("arena", snapshot.Arena == null ? 0 : 1, 1, failures);
            Expect("bounds", snapshot.Bounds == null ? 0 : 1, 1, failures);
            Expect("walls", snapshot.Walls.Length, 8, failures);
            Expect("obstacles", snapshot.Obstacles.Length, 4, failures);
            Expect("relays", snapshot.Relays.Length, 2, failures);
            Expect("terminals", snapshot.Terminals.Length, 2, failures);
            Expect("gates", snapshot.Gates.Length, 2, failures);
            Expect("boosts", snapshot.Boosts.Length, 4, failures);
            Expect("spawns", snapshot.Spawns.Length, 2, failures);
            Expect("core reset", snapshot.CoreReset == null ? 0 : 1, 1, failures);
            Expect("forbidden volumes", snapshot.BarrierForbiddenVolumes.Length, 7, failures);
            Expect("navigation hints", snapshot.NavigationHints.Length, 17, failures);
            Expect("camera bounds", snapshot.CameraBounds == null ? 0 : 1, 1, failures);
            Expect("elements", snapshot.Elements.Length, 52, failures);
            report.Add(
                "ARENA-002",
                "Required authored fixtures",
                failures.Count == 0,
                failures.Count == 0 ? "All 52 expected authoring records are present." : string.Join("; ", failures));
        }

        private static void ValidateAuthoringGeometry(ArenaAuthoringSnapshot snapshot, ArenaValidationReport report)
        {
            List<string> failures = new List<string>();
            foreach (ArenaElementAuthoring element in snapshot.Elements)
            {
                Transform transform = element.transform;
                if (element.gameObject.layer != 0)
                {
                    failures.Add(element.StableId + " is not on Default layer");
                }

                if (!Finite(transform.position) || !Finite(transform.localScale) || !Finite(transform.eulerAngles))
                {
                    failures.Add(element.StableId + " has a non-finite transform");
                }
            }

            foreach (ConvexPylonAuthoring obstacle in snapshot.Obstacles)
            {
                if (!ArenaGeometry.IsConvex(WorldVertices(obstacle, obstacle.Vertices)))
                {
                    failures.Add(obstacle.StableId + " is not a finite convex polygon");
                }
            }

            foreach (BoostPadAuthoring boost in snapshot.Boosts)
            {
                if (!ArenaGeometry.IsConvex(WorldVertices(boost, boost.Vertices)))
                {
                    failures.Add(boost.StableId + " is not a finite convex polygon");
                }
            }

            foreach (ShockGateAuthoring gate in snapshot.Gates)
            {
                if (gate.TriggerSize.x <= 0f || gate.TriggerSize.y <= 0f || gate.SafeSideDirection.sqrMagnitude < 0.99f)
                {
                    failures.Add(gate.StableId + " has invalid trigger or safe-side geometry");
                }
            }

            foreach (BarrierForbiddenVolumeAuthoring volume in snapshot.BarrierForbiddenVolumes)
            {
                bool valid = volume.Shape == BarrierForbiddenShape.Circle
                    ? volume.Radius > 0f
                    : volume.Size.x > 0f && volume.Size.y > 0f;
                if (!valid)
                {
                    failures.Add(volume.StableId + " has invalid dimensions");
                }
            }

            report.Add(
                "ARENA-003",
                "Finite convex authoring geometry",
                failures.Count == 0,
                failures.Count == 0 ? "Transforms, layers, polygons, triggers, and volumes are valid." : string.Join("; ", failures));
        }

        private static void ValidateSymmetry(ArenaAuthoringSnapshot snapshot, ArenaValidationReport report)
        {
            List<string> failures = new List<string>();
            foreach (ArenaElementAuthoring element in snapshot.Elements)
            {
                float2 position = ToFloat2(ArenaAuthoringSnapshot.PositionOf(element));
                bool paired = snapshot.Elements.Any(candidate =>
                    candidate.GetType() == element.GetType() &&
                    math.distancesq(ToFloat2(ArenaAuthoringSnapshot.PositionOf(candidate)), -position) <=
                    PositionTolerance * PositionTolerance);
                if (!paired)
                {
                    failures.Add(element.StableId);
                }
            }

            report.Add(
                "ARENA-005",
                "Rotational arena symmetry",
                failures.Count == 0,
                failures.Count == 0 ? "Every authored fixture has a 180-degree counterpart." : "Unpaired: " + Join(failures));
        }

        private static void ValidateStrategicDistanceParity(ArenaBakeData data, ArenaValidationReport report)
        {
            List<float2> targets = data.Circles.Select(circle => circle.Center)
                .Concat(data.Gates.Select(gate => gate.Center))
                .Append(data.CoreReset.Position)
                .ToList();
            bool passed = data.Spawns.Count == 2;
            float maximumDelta = 0f;
            if (passed)
            {
                float[] first = targets.Select(target => math.distancesq(data.Spawns[0].Position, target)).OrderBy(value => value).ToArray();
                float[] second = targets.Select(target => math.distancesq(data.Spawns[1].Position, target)).OrderBy(value => value).ToArray();
                for (int i = 0; i < first.Length; i++)
                {
                    maximumDelta = math.max(maximumDelta, math.abs(first[i] - second[i]));
                }

                passed = maximumDelta <= DistanceParityTolerance;
            }

            report.Add(
                "ARENA-006",
                "Strategic distance parity",
                passed,
                passed ? "Spawn-to-strategic squared-distance sets match." : "Maximum squared-distance delta: " + maximumDelta);
        }

        private static void ValidateNavigation(ArenaBakeData data, ArenaValidationReport report)
        {
            HashSet<ArenaElementId> visited = TraverseNavigation(data, default(ArenaElementId), default(ArenaElementId));
            bool allKinds = Enum.GetValues(typeof(BakedNavigationKind))
                .Cast<BakedNavigationKind>()
                .All(kind => data.NavigationNodes.Any(node => node.Kind == kind));
            bool passed = data.NavigationNodes.Count > 0 && visited.Count == data.NavigationNodes.Count && allKinds;
            report.Add(
                "ARENA-007",
                "Navigation adjacency and reachability",
                passed,
                visited.Count + "/" + data.NavigationNodes.Count + " nodes reachable across " + data.NavigationEdges.Count + " precomputed edges.");
        }

        private static void ValidateCameraContainment(ArenaBakeData data, ArenaValidationReport report)
        {
            ArenaAabb playable = data.Bounds.Bounds;
            ArenaAabb camera = new ArenaAabb(
                data.CameraBounds.Center - data.CameraBounds.HalfExtents,
                data.CameraBounds.Center + data.CameraBounds.HalfExtents);
            float[] ratios = { 16f / 9f, 16f / 10f, 21f / 9f };
            bool authoredBoundsContain = camera.Contains(playable.Minimum, PositionTolerance) && camera.Contains(playable.Maximum, PositionTolerance);
            bool projectionContains = ratios.All(ratio =>
                data.CameraBounds.OrthographicSize + PositionTolerance >= data.Bounds.HalfExtents.y &&
                data.CameraBounds.OrthographicSize * ratio + PositionTolerance >= data.Bounds.HalfExtents.x);
            bool passed = authoredBoundsContain && projectionContains;
            report.Add(
                "ARENA-008",
                "Camera containment at target ratios",
                passed,
                passed ? "Playable bounds fit at 16:9, 16:10, and 21:9." : "Camera bounds or orthographic framing clip playable geometry.");
        }

        private static void ValidateDisplacementTargets(ArenaBakeData data, ArenaValidationReport report)
        {
            List<string> failures = new List<string>();
            foreach (BakedShockGate gate in data.Gates)
            {
                float directionalExtent = math.abs(gate.SafeSideDirection.x) * gate.HalfExtents.x +
                                          math.abs(gate.SafeSideDirection.y) * gate.HalfExtents.y;
                float2 target = gate.Center + gate.SafeSideDirection * (directionalExtent + 1.2f + ArenaBakeCompiler.PlayerRadius);
                bool inside = data.Bounds.Bounds.Contains(target, -ArenaBakeCompiler.PlayerRadius);
                bool blocked = data.Obstacles.Any(obstacle => obstacle.PlayerExpandedBounds.Contains(target));
                if (!inside || blocked)
                {
                    failures.Add(gate.Id + " -> " + target);
                }
            }

            report.Add(
                "ARENA-009",
                "Safe shock displacement targets",
                failures.Count == 0,
                failures.Count == 0 ? "Every gate safe-side landing is in bounds and outside expanded obstacles." : Join(failures));
        }

        private static void ValidateOverlapsAndForbiddenVolumes(ArenaBakeData data, ArenaValidationReport report)
        {
            List<string> failures = new List<string>();
            for (int i = 0; i < data.Obstacles.Count; i++)
            {
                for (int j = i + 1; j < data.Obstacles.Count; j++)
                {
                    if (Overlaps(data.Obstacles[i].Bounds, data.Obstacles[j].Bounds, PositionTolerance))
                    {
                        failures.Add(data.Obstacles[i].Id + " overlaps " + data.Obstacles[j].Id);
                    }
                }
            }

            IEnumerable<float2> strategicPoints = data.Circles.Select(circle => circle.Center)
                .Concat(data.Spawns.Select(spawn => spawn.Position))
                .Append(data.CoreReset.Position);
            if (strategicPoints.Any(point => data.Obstacles.Any(obstacle => ArenaGeometry.PointInConvexPolygon(point, obstacle.Vertices))))
            {
                failures.Add("strategic fixture intersects an obstacle");
            }

            foreach (BakedNavigationNode node in data.NavigationNodes)
            {
                if (data.Obstacles.Any(obstacle => obstacle.PlayerExpandedBounds.Contains(node.Position)))
                {
                    failures.Add(node.Id + " lies inside player-expanded obstacle bounds");
                }
            }

            foreach (BakedBarrierForbiddenVolume volume in data.BarrierForbiddenVolumes)
            {
                float2 extent = volume.Shape == BakedForbiddenShape.Circle
                    ? new float2(volume.Radius, volume.Radius)
                    : volume.HalfExtents;
                if (!data.Bounds.Bounds.Contains(volume.Center - extent) || !data.Bounds.Bounds.Contains(volume.Center + extent))
                {
                    failures.Add(volume.Id + " extends outside playable bounds");
                }
            }

            foreach (BakedShockGate gate in data.Gates)
            {
                bool covered = data.BarrierForbiddenVolumes.Any(volume =>
                    volume.Shape == BakedForbiddenShape.Rectangle &&
                    math.distancesq(volume.Center, gate.Center) <= PositionTolerance * PositionTolerance &&
                    math.all(volume.HalfExtents + new float2(PositionTolerance) >= gate.HalfExtents));
                if (!covered)
                {
                    failures.Add(gate.Id + " lacks a covering Barrier-forbidden volume");
                }
            }

            report.Add(
                "ARENA-010",
                "Overlap and forbidden-volume safety",
                failures.Count == 0,
                failures.Count == 0 ? "No invalid overlaps; every forbidden volume and gate footprint is safe." : string.Join("; ", failures));
        }

        private static void ValidateBarrierSafety(ArenaBakeData data, ArenaValidationReport report)
        {
            bool noSingleEdgeCut = data.NavigationEdges.All(edge =>
                TraverseNavigation(data, edge.NodeA, edge.NodeB).Count == data.NavigationNodes.Count);
            float minimumArenaSpan = math.cmin(data.Bounds.HalfExtents * 2f);
            bool placementEnvelope = data.BarrierForbiddenVolumes.Count >= 7 && minimumArenaSpan > 2.6f + ArenaBakeCompiler.PlayerRadius * 2f;
            bool passed = noSingleEdgeCut && placementEnvelope;
            report.Add(
                "ARENA-011",
                "Barrier safety and route redundancy",
                passed,
                noSingleEdgeCut
                    ? "Navigation stays connected after any single edge loss; seven placement exclusions are baked."
                    : "A single navigation edge can isolate part of the arena.");
        }

        private static void ValidateCoreReachability(ArenaBakeData data, ArenaValidationReport report)
        {
            ArenaAabb bounds = data.Bounds.Bounds;
            float2[] perimeter = BuildPerimeter(data.Walls);
            if (perimeter.Length != data.Walls.Count)
            {
                report.Add("ARENA-012", "Core and free-space reachability", false, "Perimeter walls do not form one closed loop.");
                return;
            }

            int width = (int)math.floor((bounds.Maximum.x - bounds.Minimum.x) / GridStep) + 1;
            int height = (int)math.floor((bounds.Maximum.y - bounds.Minimum.y) / GridStep) + 1;
            bool[,] walkable = new bool[width, height];
            int walkableCount = 0;
            int startX = 0;
            int startY = 0;
            float nearest = float.MaxValue;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float2 point = new float2(bounds.Minimum.x + x * GridStep, bounds.Minimum.y + y * GridStep);
                    bool clearOfWalls = data.Walls.All(wall =>
                    {
                        float clearance = wall.Thickness * 0.5f + ArenaBakeCompiler.PlayerRadius;
                        return ArenaGeometry.DistanceSquaredToSegment(point, wall.Start, wall.End) > clearance * clearance;
                    });
                    bool valid = ArenaGeometry.PointInPolygon(point, perimeter) && clearOfWalls &&
                                 !data.Obstacles.Any(obstacle => obstacle.PlayerExpandedBounds.Contains(point));
                    walkable[x, y] = valid;
                    if (!valid)
                    {
                        continue;
                    }

                    walkableCount++;
                    float distance = math.distancesq(point, data.CoreReset.Position);
                    if (distance < nearest)
                    {
                        nearest = distance;
                        startX = x;
                        startY = y;
                    }
                }
            }

            bool[,] visited = new bool[width, height];
            Queue<int2> queue = new Queue<int2>();
            if (walkableCount > 0)
            {
                visited[startX, startY] = true;
                queue.Enqueue(new int2(startX, startY));
            }

            int reached = 0;
            int2[] directions = { new int2(1, 0), new int2(-1, 0), new int2(0, 1), new int2(0, -1) };
            while (queue.Count > 0)
            {
                int2 current = queue.Dequeue();
                reached++;
                foreach (int2 direction in directions)
                {
                    int2 next = current + direction;
                    if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= height ||
                        visited[next.x, next.y] || !walkable[next.x, next.y])
                    {
                        continue;
                    }

                    visited[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }

            bool passed = walkableCount > 0 && reached == walkableCount;
            report.Add(
                "ARENA-012",
                "Core and free-space reachability",
                passed,
                reached + "/" + walkableCount + " collision-expanded grid samples reachable from core.");
        }

        private static float2[] BuildPerimeter(IReadOnlyList<BakedWall> walls)
        {
            if (walls == null || walls.Count < 3)
            {
                return Array.Empty<float2>();
            }

            float2 start = walls.SelectMany(wall => new[] { wall.Start, wall.End })
                .OrderBy(point => point.x)
                .ThenBy(point => point.y)
                .First();
            float2 current = start;
            bool[] used = new bool[walls.Count];
            List<float2> vertices = new List<float2>(walls.Count);
            for (int step = 0; step < walls.Count; step++)
            {
                int match = -1;
                bool matchedStart = false;
                for (int index = 0; index < walls.Count; index++)
                {
                    if (used[index])
                    {
                        continue;
                    }

                    if (math.distancesq(walls[index].Start, current) <= PositionTolerance * PositionTolerance)
                    {
                        match = index;
                        matchedStart = true;
                        break;
                    }

                    if (math.distancesq(walls[index].End, current) <= PositionTolerance * PositionTolerance)
                    {
                        match = index;
                        matchedStart = false;
                        break;
                    }
                }

                if (match < 0)
                {
                    return Array.Empty<float2>();
                }

                vertices.Add(current);
                used[match] = true;
                current = matchedStart ? walls[match].End : walls[match].Start;
            }

            return math.distancesq(current, start) <= PositionTolerance * PositionTolerance
                ? vertices.ToArray()
                : Array.Empty<float2>();
        }

        private static HashSet<ArenaElementId> TraverseNavigation(
            ArenaBakeData data,
            ArenaElementId removedA,
            ArenaElementId removedB)
        {
            Dictionary<ArenaElementId, List<ArenaElementId>> adjacency = data.NavigationNodes.ToDictionary(
                node => node.Id,
                node => new List<ArenaElementId>());
            foreach (BakedNavigationEdge edge in data.NavigationEdges)
            {
                bool removed = (edge.NodeA == removedA && edge.NodeB == removedB) ||
                               (edge.NodeA == removedB && edge.NodeB == removedA);
                if (removed)
                {
                    continue;
                }

                adjacency[edge.NodeA].Add(edge.NodeB);
                adjacency[edge.NodeB].Add(edge.NodeA);
            }

            HashSet<ArenaElementId> visited = new HashSet<ArenaElementId>();
            if (data.NavigationNodes.Count == 0)
            {
                return visited;
            }

            Queue<ArenaElementId> queue = new Queue<ArenaElementId>();
            queue.Enqueue(data.NavigationNodes[0].Id);
            visited.Add(data.NavigationNodes[0].Id);
            while (queue.Count > 0)
            {
                ArenaElementId current = queue.Dequeue();
                foreach (ArenaElementId neighbor in adjacency[current])
                {
                    if (visited.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return visited;
        }

        private static float2[] WorldVertices(ArenaElementAuthoring element, IEnumerable<Vector2> vertices)
        {
            return vertices.Select(vertex =>
            {
                Vector3 world = element.transform.TransformPoint(new Vector3(vertex.x, 0f, vertex.y));
                return new float2(world.x, world.z);
            }).ToArray();
        }

        private static bool Overlaps(ArenaAabb first, ArenaAabb second, float tolerance)
        {
            return first.Minimum.x < second.Maximum.x - tolerance &&
                   first.Maximum.x > second.Minimum.x + tolerance &&
                   first.Minimum.y < second.Maximum.y - tolerance &&
                   first.Maximum.y > second.Minimum.y + tolerance;
        }

        private static bool Finite(Vector3 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        }

        private static float2 ToFloat2(Vector2 value)
        {
            return new float2(value.x, value.y);
        }

        private static bool IsLowerHex(char value)
        {
            return (value >= '0' && value <= '9') || (value >= 'a' && value <= 'f');
        }

        private static void Expect(string name, int actual, int expected, ICollection<string> failures)
        {
            if (actual != expected)
            {
                failures.Add(name + " " + actual + "/" + expected);
            }
        }

        private static string Join(IEnumerable<string> values)
        {
            string result = string.Join(", ", values);
            return string.IsNullOrEmpty(result) ? "none" : result;
        }
    }
}
