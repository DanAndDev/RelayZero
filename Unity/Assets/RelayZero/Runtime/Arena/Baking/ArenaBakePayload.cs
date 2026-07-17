using System;
using System.Linq;
using RelayZero.Arena;
using Unity.Mathematics;
using UnityEngine;

namespace RelayZero.Arena.Baking
{
    [Serializable]
    public struct ArenaElementRecord
    {
        public ushort id;
        public string stableId;
        public BakedElementKind kind;
    }

    [Serializable]
    public struct ArenaBoundsRecord
    {
        public ushort id;
        public Vector2 center;
        public Vector2 halfExtents;
    }

    [Serializable]
    public struct ArenaWallRecord
    {
        public ushort id;
        public Vector2 start;
        public Vector2 end;
        public Vector2 normal;
        public float thickness;
        public Vector2 minimum;
        public Vector2 maximum;
        public Vector2 playerExpandedMinimum;
        public Vector2 playerExpandedMaximum;
    }

    [Serializable]
    public struct ArenaPolygonRecord
    {
        public ushort id;
        public BakedPowerSide side;
        public Vector2[] vertices;
        public Vector2[] normals;
        public Vector2 minimum;
        public Vector2 maximum;
        public Vector2 playerExpandedMinimum;
        public Vector2 playerExpandedMaximum;
    }

    [Serializable]
    public struct ArenaCircleRecord
    {
        public ushort id;
        public BakedElementKind kind;
        public Vector2 center;
        public float radius;
    }

    [Serializable]
    public struct ArenaGateRecord
    {
        public ushort id;
        public BakedPowerSide safeWhenPoweredSide;
        public Vector2 center;
        public Vector2 halfExtents;
        public Vector2 safeSideDirection;
        public Vector2 minimum;
        public Vector2 maximum;
    }

    [Serializable]
    public struct ArenaSpawnRecord
    {
        public ushort id;
        public byte playerSlot;
        public Vector2 position;
        public Vector2 facingDirection;
    }

    [Serializable]
    public struct ArenaCoreResetRecord
    {
        public ushort id;
        public Vector2 position;
        public float pedestalRadius;
    }

    [Serializable]
    public struct ArenaForbiddenRecord
    {
        public ushort id;
        public BakedForbiddenShape shape;
        public Vector2 center;
        public float radius;
        public Vector2 halfExtents;
        public string reason;
    }

    [Serializable]
    public struct ArenaNavigationNodeRecord
    {
        public ushort id;
        public BakedNavigationKind kind;
        public Vector2 position;
        public float connectionRadius;
        public ushort[] neighborIds;
    }

    [Serializable]
    public struct ArenaNavigationEdgeRecord
    {
        public ushort nodeA;
        public ushort nodeB;
        public float lengthSquared;
        public ushort crossedGate;
    }

    [Serializable]
    public struct ArenaCameraBoundsRecord
    {
        public ushort id;
        public Vector2 center;
        public Vector2 halfExtents;
        public float orthographicSize;
    }

    public sealed class ArenaBakePayload
    {
        public int BakeVersion { get; set; } = 1;
        public string SourceScene { get; set; } = string.Empty;
        public ArenaElementRecord[] Elements { get; set; } = Array.Empty<ArenaElementRecord>();
        public ArenaBoundsRecord Bounds { get; set; }
        public ArenaWallRecord[] Walls { get; set; } = Array.Empty<ArenaWallRecord>();
        public ArenaPolygonRecord[] Obstacles { get; set; } = Array.Empty<ArenaPolygonRecord>();
        public ArenaCircleRecord[] Circles { get; set; } = Array.Empty<ArenaCircleRecord>();
        public ArenaGateRecord[] Gates { get; set; } = Array.Empty<ArenaGateRecord>();
        public ArenaPolygonRecord[] Boosts { get; set; } = Array.Empty<ArenaPolygonRecord>();
        public ArenaSpawnRecord[] Spawns { get; set; } = Array.Empty<ArenaSpawnRecord>();
        public ArenaCoreResetRecord CoreReset { get; set; }
        public ArenaForbiddenRecord[] BarrierForbiddenVolumes { get; set; } = Array.Empty<ArenaForbiddenRecord>();
        public ArenaNavigationNodeRecord[] NavigationNodes { get; set; } = Array.Empty<ArenaNavigationNodeRecord>();
        public ArenaNavigationEdgeRecord[] NavigationEdges { get; set; } = Array.Empty<ArenaNavigationEdgeRecord>();
        public ArenaCameraBoundsRecord CameraBounds { get; set; }

        public ArenaBakeData ToRuntimeData(string contentHash)
        {
            return new ArenaBakeData(
                BakeVersion,
                contentHash,
                SourceScene,
                Elements.Select(record => new ArenaElementDescriptor(
                    new ArenaElementId(record.id),
                    record.stableId,
                    record.kind)),
                new BakedArenaBounds(
                    new ArenaElementId(Bounds.id),
                    ToFloat2(Bounds.center),
                    ToFloat2(Bounds.halfExtents)),
                Walls.Select(record => new BakedWall(
                    new ArenaElementId(record.id),
                    ToFloat2(record.start),
                    ToFloat2(record.end),
                    ToFloat2(record.normal),
                    record.thickness,
                    new ArenaAabb(ToFloat2(record.minimum), ToFloat2(record.maximum)),
                    new ArenaAabb(
                        ToFloat2(record.playerExpandedMinimum),
                        ToFloat2(record.playerExpandedMaximum)))),
                Obstacles.Select(record => new BakedConvexObstacle(
                    new ArenaElementId(record.id),
                    record.vertices.Select(ToFloat2),
                    record.normals.Select(ToFloat2),
                    new ArenaAabb(ToFloat2(record.minimum), ToFloat2(record.maximum)),
                    new ArenaAabb(
                        ToFloat2(record.playerExpandedMinimum),
                        ToFloat2(record.playerExpandedMaximum)))),
                Circles.Select(record => new BakedCircle(
                    new ArenaElementId(record.id),
                    record.kind,
                    ToFloat2(record.center),
                    record.radius)),
                Gates.Select(record => new BakedShockGate(
                    new ArenaElementId(record.id),
                    record.safeWhenPoweredSide,
                    ToFloat2(record.center),
                    ToFloat2(record.halfExtents),
                    ToFloat2(record.safeSideDirection),
                    new ArenaAabb(ToFloat2(record.minimum), ToFloat2(record.maximum)))),
                Boosts.Select(record => new BakedBoostPad(
                    new ArenaElementId(record.id),
                    record.side,
                    record.vertices.Select(ToFloat2),
                    record.normals.Select(ToFloat2),
                    new ArenaAabb(ToFloat2(record.minimum), ToFloat2(record.maximum)))),
                Spawns.Select(record => new BakedSpawn(
                    new ArenaElementId(record.id),
                    record.playerSlot,
                    ToFloat2(record.position),
                    ToFloat2(record.facingDirection))),
                new BakedCoreReset(
                    new ArenaElementId(CoreReset.id),
                    ToFloat2(CoreReset.position),
                    CoreReset.pedestalRadius),
                BarrierForbiddenVolumes.Select(record => new BakedBarrierForbiddenVolume(
                    new ArenaElementId(record.id),
                    record.shape,
                    ToFloat2(record.center),
                    record.radius,
                    ToFloat2(record.halfExtents),
                    record.reason)),
                NavigationNodes.Select(record => new BakedNavigationNode(
                    new ArenaElementId(record.id),
                    record.kind,
                    ToFloat2(record.position),
                    record.connectionRadius,
                    (record.neighborIds ?? Array.Empty<ushort>()).Select(value => new ArenaElementId(value)))),
                NavigationEdges.Select(record => new BakedNavigationEdge(
                    new ArenaElementId(record.nodeA),
                    new ArenaElementId(record.nodeB),
                    record.lengthSquared,
                    new ArenaElementId(record.crossedGate))),
                new BakedCameraBounds(
                    new ArenaElementId(CameraBounds.id),
                    ToFloat2(CameraBounds.center),
                    ToFloat2(CameraBounds.halfExtents),
                    CameraBounds.orthographicSize));
        }

        private static float2 ToFloat2(Vector2 value)
        {
            return new float2(value.x, value.y);
        }
    }
}
