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
    internal static class ArenaBakeCompiler
    {
        public const int CurrentBakeVersion = 1;
        public const float PlayerRadius = 0.45f;

        public static ArenaBakePayload Compile(ArenaAuthoringSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            ArenaElementRecord[] elements = snapshot.Elements
                .Select(element => new ArenaElementRecord
                {
                    id = snapshot.GetId(element.StableId).Value,
                    stableId = element.StableId,
                    kind = GetElementKind(element),
                })
                .OrderBy(record => record.id)
                .ToArray();

            ArenaBoundsRecord bounds = CompileBounds(snapshot);
            ArenaWallRecord[] walls = snapshot.Walls.Select(CompileWall).OrderBy(record => record.id).ToArray();
            ArenaPolygonRecord[] obstacles = snapshot.Obstacles
                .Select(obstacle => CompilePolygon(snapshot, obstacle, BakedPowerSide.Alpha, true))
                .OrderBy(record => record.id)
                .ToArray();
            ArenaCircleRecord[] circles = snapshot.Relays
                .Select(relay => CompileCircle(snapshot, relay, BakedElementKind.Relay, relay.Radius))
                .Concat(snapshot.Terminals.Select(terminal =>
                    CompileCircle(snapshot, terminal, BakedElementKind.Terminal, terminal.InteractionRadius)))
                .OrderBy(record => record.id)
                .ToArray();
            ArenaGateRecord[] gates = snapshot.Gates.Select(gate => CompileGate(snapshot, gate)).OrderBy(record => record.id).ToArray();
            ArenaPolygonRecord[] boosts = snapshot.Boosts
                .Select(boost => CompilePolygon(
                    snapshot,
                    boost,
                    boost.Side == ArenaPowerSide.Alpha ? BakedPowerSide.Alpha : BakedPowerSide.Beta,
                    false))
                .OrderBy(record => record.id)
                .ToArray();
            ArenaSpawnRecord[] spawns = snapshot.Spawns.Select(spawn => CompileSpawn(snapshot, spawn)).OrderBy(record => record.id).ToArray();
            ArenaCoreResetRecord coreReset = CompileCoreReset(snapshot);
            ArenaForbiddenRecord[] forbidden = snapshot.BarrierForbiddenVolumes
                .Select(volume => CompileForbidden(snapshot, volume))
                .OrderBy(record => record.id)
                .ToArray();
            ArenaCameraBoundsRecord cameraBounds = CompileCameraBounds(snapshot);

            ArenaNavigationNodeRecord[] navigationNodes;
            ArenaNavigationEdgeRecord[] navigationEdges;
            CompileNavigation(snapshot, obstacles, gates, out navigationNodes, out navigationEdges);

            return new ArenaBakePayload
            {
                BakeVersion = CurrentBakeVersion,
                SourceScene = snapshot.Scene.path,
                Elements = elements,
                Bounds = bounds,
                Walls = walls,
                Obstacles = obstacles,
                Circles = circles,
                Gates = gates,
                Boosts = boosts,
                Spawns = spawns,
                CoreReset = coreReset,
                BarrierForbiddenVolumes = forbidden,
                NavigationNodes = navigationNodes,
                NavigationEdges = navigationEdges,
                CameraBounds = cameraBounds,
            };
        }

        private static ArenaBoundsRecord CompileBounds(ArenaAuthoringSnapshot snapshot)
        {
            Vector2 center = Canonical(ArenaAuthoringSnapshot.PositionOf(snapshot.Bounds));
            return new ArenaBoundsRecord
            {
                id = snapshot.GetId(snapshot.Bounds.StableId).Value,
                center = center,
                halfExtents = Canonical(snapshot.Bounds.Size * 0.5f),
            };
        }

        private static ArenaWallRecord CompileWall(ArenaWallAuthoring wall)
        {
            float2 start = ToFloat2(Canonical(wall.Start));
            float2 end = ToFloat2(Canonical(wall.End));
            float2 direction = math.normalize(end - start);
            float2 inwardNormal = new float2(direction.y, -direction.x);
            float halfThickness = wall.Thickness * 0.5f;
            ArenaAabb bounds = new ArenaAabb(math.min(start, end), math.max(start, end)).Expanded(halfThickness);
            ArenaAabb expanded = bounds.Expanded(PlayerRadius);
            return new ArenaWallRecord
            {
                id = ArenaElementId.FromStableId(wall.StableId).Value,
                start = ToVector2(start),
                end = ToVector2(end),
                normal = ToVector2(ArenaGeometry.Canonicalize(inwardNormal)),
                thickness = ArenaGeometry.Canonicalize(wall.Thickness),
                minimum = ToVector2(ArenaGeometry.Canonicalize(bounds.Minimum)),
                maximum = ToVector2(ArenaGeometry.Canonicalize(bounds.Maximum)),
                playerExpandedMinimum = ToVector2(ArenaGeometry.Canonicalize(expanded.Minimum)),
                playerExpandedMaximum = ToVector2(ArenaGeometry.Canonicalize(expanded.Maximum)),
            };
        }

        private static ArenaPolygonRecord CompilePolygon(
            ArenaAuthoringSnapshot snapshot,
            ArenaElementAuthoring element,
            BakedPowerSide side,
            bool includePlayerExpansion)
        {
            Vector2[] localVertices;
            if (element is ConvexPylonAuthoring obstacle)
            {
                localVertices = obstacle.Vertices;
            }
            else if (element is BoostPadAuthoring boost)
            {
                localVertices = boost.Vertices;
            }
            else
            {
                throw new ArgumentException("Unsupported polygon authoring component.", nameof(element));
            }

            float2[] worldVertices = localVertices
                .Select(vertex =>
                {
                    Vector3 world = element.transform.TransformPoint(new Vector3(vertex.x, 0f, vertex.y));
                    return new float2(world.x, world.z);
                })
                .ToArray();
            float2[] canonical = ArenaGeometry.CanonicalizePolygon(worldVertices);
            float2[] normals = ArenaGeometry.ComputeOutwardNormals(canonical)
                .Select(ArenaGeometry.Canonicalize)
                .ToArray();
            ArenaAabb bounds = ArenaGeometry.ComputeBounds(canonical);
            ArenaAabb expanded = includePlayerExpansion ? bounds.Expanded(PlayerRadius) : bounds;
            return new ArenaPolygonRecord
            {
                id = snapshot.GetId(element.StableId).Value,
                side = side,
                vertices = canonical.Select(ToVector2).ToArray(),
                normals = normals.Select(ToVector2).ToArray(),
                minimum = ToVector2(bounds.Minimum),
                maximum = ToVector2(bounds.Maximum),
                playerExpandedMinimum = ToVector2(ArenaGeometry.Canonicalize(expanded.Minimum)),
                playerExpandedMaximum = ToVector2(ArenaGeometry.Canonicalize(expanded.Maximum)),
            };
        }

        private static ArenaCircleRecord CompileCircle(
            ArenaAuthoringSnapshot snapshot,
            ArenaElementAuthoring element,
            BakedElementKind kind,
            float radius)
        {
            return new ArenaCircleRecord
            {
                id = snapshot.GetId(element.StableId).Value,
                kind = kind,
                center = Canonical(ArenaAuthoringSnapshot.PositionOf(element)),
                radius = ArenaGeometry.Canonicalize(radius),
            };
        }

        private static ArenaGateRecord CompileGate(ArenaAuthoringSnapshot snapshot, ShockGateAuthoring gate)
        {
            Vector2 center = Canonical(ArenaAuthoringSnapshot.PositionOf(gate));
            Vector2 halfExtents = Canonical(gate.TriggerSize * 0.5f);
            return new ArenaGateRecord
            {
                id = snapshot.GetId(gate.StableId).Value,
                safeWhenPoweredSide = gate.SafeWhenPoweredSide == ArenaPowerSide.Alpha
                    ? BakedPowerSide.Alpha
                    : BakedPowerSide.Beta,
                center = center,
                halfExtents = halfExtents,
                safeSideDirection = Canonical(gate.SafeSideDirection.normalized),
                minimum = Canonical(center - halfExtents),
                maximum = Canonical(center + halfExtents),
            };
        }

        private static ArenaSpawnRecord CompileSpawn(ArenaAuthoringSnapshot snapshot, SpawnPointAuthoring spawn)
        {
            return new ArenaSpawnRecord
            {
                id = snapshot.GetId(spawn.StableId).Value,
                playerSlot = checked((byte)spawn.PlayerSlot),
                position = Canonical(ArenaAuthoringSnapshot.PositionOf(spawn)),
                facingDirection = Canonical(spawn.FacingDirection.normalized),
            };
        }

        private static ArenaCoreResetRecord CompileCoreReset(ArenaAuthoringSnapshot snapshot)
        {
            return new ArenaCoreResetRecord
            {
                id = snapshot.GetId(snapshot.CoreReset.StableId).Value,
                position = Canonical(ArenaAuthoringSnapshot.PositionOf(snapshot.CoreReset)),
                pedestalRadius = ArenaGeometry.Canonicalize(snapshot.CoreReset.PedestalRadius),
            };
        }

        private static ArenaForbiddenRecord CompileForbidden(
            ArenaAuthoringSnapshot snapshot,
            BarrierForbiddenVolumeAuthoring volume)
        {
            return new ArenaForbiddenRecord
            {
                id = snapshot.GetId(volume.StableId).Value,
                shape = volume.Shape == BarrierForbiddenShape.Circle
                    ? BakedForbiddenShape.Circle
                    : BakedForbiddenShape.Rectangle,
                center = Canonical(ArenaAuthoringSnapshot.PositionOf(volume)),
                radius = ArenaGeometry.Canonicalize(volume.Radius),
                halfExtents = Canonical(volume.Size * 0.5f),
                reason = volume.Reason,
            };
        }

        private static ArenaCameraBoundsRecord CompileCameraBounds(ArenaAuthoringSnapshot snapshot)
        {
            return new ArenaCameraBoundsRecord
            {
                id = snapshot.GetId(snapshot.CameraBounds.StableId).Value,
                center = Canonical(ArenaAuthoringSnapshot.PositionOf(snapshot.CameraBounds)),
                halfExtents = Canonical(snapshot.CameraBounds.Size * 0.5f),
                orthographicSize = ArenaGeometry.Canonicalize(snapshot.CameraBounds.OrthographicSize),
            };
        }

        private static void CompileNavigation(
            ArenaAuthoringSnapshot snapshot,
            ArenaPolygonRecord[] obstacles,
            ArenaGateRecord[] gates,
            out ArenaNavigationNodeRecord[] nodes,
            out ArenaNavigationEdgeRecord[] edges)
        {
            nodes = snapshot.NavigationHints
                .Select(hint => new ArenaNavigationNodeRecord
                {
                    id = snapshot.GetId(hint.StableId).Value,
                    kind = (BakedNavigationKind)hint.Kind,
                    position = Canonical(ArenaAuthoringSnapshot.PositionOf(hint)),
                    connectionRadius = ArenaGeometry.Canonicalize(hint.ConnectionRadius),
                    neighborIds = Array.Empty<ushort>(),
                })
                .OrderBy(node => node.id)
                .ToArray();

            List<ArenaNavigationEdgeRecord> compiledEdges = new List<ArenaNavigationEdgeRecord>();
            Dictionary<ushort, List<ushort>> neighbors = nodes.ToDictionary(
                node => node.id,
                node => new List<ushort>());

            for (int i = 0; i < nodes.Length; i++)
            {
                for (int j = i + 1; j < nodes.Length; j++)
                {
                    float2 start = ToFloat2(nodes[i].position);
                    float2 end = ToFloat2(nodes[j].position);
                    float distanceSquared = math.lengthsq(end - start);
                    float maximum = math.min(nodes[i].connectionRadius, nodes[j].connectionRadius);
                    if (distanceSquared > maximum * maximum || !IsNavigationSegmentClear(start, end, obstacles))
                    {
                        continue;
                    }

                    ushort crossedGate = FindCrossedGate(start, end, gates);
                    compiledEdges.Add(new ArenaNavigationEdgeRecord
                    {
                        nodeA = nodes[i].id,
                        nodeB = nodes[j].id,
                        lengthSquared = ArenaGeometry.Canonicalize(distanceSquared),
                        crossedGate = crossedGate,
                    });
                    neighbors[nodes[i].id].Add(nodes[j].id);
                    neighbors[nodes[j].id].Add(nodes[i].id);
                }
            }

            for (int i = 0; i < nodes.Length; i++)
            {
                ArenaNavigationNodeRecord node = nodes[i];
                node.neighborIds = neighbors[node.id].OrderBy(value => value).ToArray();
                nodes[i] = node;
            }

            edges = compiledEdges
                .OrderBy(edge => edge.nodeA)
                .ThenBy(edge => edge.nodeB)
                .ToArray();
        }

        private static bool IsNavigationSegmentClear(
            float2 start,
            float2 end,
            IEnumerable<ArenaPolygonRecord> obstacles)
        {
            foreach (ArenaPolygonRecord obstacle in obstacles)
            {
                float2 minimum = ToFloat2(obstacle.playerExpandedMinimum);
                float2 maximum = ToFloat2(obstacle.playerExpandedMaximum);
                float2[] playerExpandedBounds =
                {
                    new float2(minimum.x, minimum.y),
                    new float2(maximum.x, minimum.y),
                    new float2(maximum.x, maximum.y),
                    new float2(minimum.x, maximum.y),
                };
                if (ArenaGeometry.SegmentIntersectsConvexPolygon(start, end, playerExpandedBounds))
                {
                    return false;
                }
            }

            return true;
        }

        private static ushort FindCrossedGate(float2 start, float2 end, IEnumerable<ArenaGateRecord> gates)
        {
            foreach (ArenaGateRecord gate in gates.OrderBy(value => value.id))
            {
                float2 minimum = ToFloat2(gate.minimum);
                float2 maximum = ToFloat2(gate.maximum);
                float2[] rectangle =
                {
                    new float2(minimum.x, minimum.y),
                    new float2(maximum.x, minimum.y),
                    new float2(maximum.x, maximum.y),
                    new float2(minimum.x, maximum.y),
                };
                if (ArenaGeometry.SegmentIntersectsConvexPolygon(start, end, rectangle))
                {
                    return gate.id;
                }
            }

            return 0;
        }

        private static BakedElementKind GetElementKind(ArenaElementAuthoring element)
        {
            if (element is SwitchyardArenaAuthoring) return BakedElementKind.Arena;
            if (element is ArenaBoundsAuthoring) return BakedElementKind.Bounds;
            if (element is ArenaWallAuthoring) return BakedElementKind.Wall;
            if (element is ConvexPylonAuthoring) return BakedElementKind.ConvexObstacle;
            if (element is RelayZoneAuthoring) return BakedElementKind.Relay;
            if (element is TerminalAuthoring) return BakedElementKind.Terminal;
            if (element is ShockGateAuthoring) return BakedElementKind.ShockGate;
            if (element is BoostPadAuthoring) return BakedElementKind.BoostPad;
            if (element is SpawnPointAuthoring) return BakedElementKind.Spawn;
            if (element is CoreResetPointAuthoring) return BakedElementKind.CoreReset;
            if (element is BarrierForbiddenVolumeAuthoring) return BakedElementKind.BarrierForbidden;
            if (element is NavigationHintAuthoring) return BakedElementKind.NavigationNode;
            if (element is CameraBoundsAuthoring) return BakedElementKind.CameraBounds;
            throw new ArgumentOutOfRangeException(nameof(element), element.GetType().FullName, "Unknown arena authoring type.");
        }

        private static Vector2 Canonical(Vector2 value)
        {
            float2 canonical = ArenaGeometry.Canonicalize(ToFloat2(value));
            return ToVector2(canonical);
        }

        private static float2 ToFloat2(Vector2 value)
        {
            return new float2(value.x, value.y);
        }

        private static Vector2 ToVector2(float2 value)
        {
            return new Vector2(value.x, value.y);
        }
    }
}
