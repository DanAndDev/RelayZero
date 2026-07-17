using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.Mathematics;

namespace RelayZero.Arena
{
    public enum BakedElementKind : byte
    {
        Arena,
        Bounds,
        Wall,
        ConvexObstacle,
        Relay,
        Terminal,
        ShockGate,
        BoostPad,
        Spawn,
        CoreReset,
        BarrierForbidden,
        NavigationNode,
        CameraBounds,
    }

    public enum BakedPowerSide : byte
    {
        Alpha,
        Beta,
    }

    public enum BakedForbiddenShape : byte
    {
        Circle,
        Rectangle,
    }

    public enum BakedNavigationKind : byte
    {
        Spawn,
        Core,
        Relay,
        Terminal,
        LaneJunction,
        PylonCorner,
    }

    public sealed class ArenaElementDescriptor
    {
        public ArenaElementDescriptor(ArenaElementId id, string stableId, BakedElementKind kind)
        {
            Id = id;
            StableId = stableId ?? throw new ArgumentNullException(nameof(stableId));
            Kind = kind;
        }

        public ArenaElementId Id { get; }

        public string StableId { get; }

        public BakedElementKind Kind { get; }
    }

    public sealed class BakedArenaBounds
    {
        public BakedArenaBounds(ArenaElementId id, float2 center, float2 halfExtents)
        {
            Id = id;
            Center = center;
            HalfExtents = halfExtents;
        }

        public ArenaElementId Id { get; }

        public float2 Center { get; }

        public float2 HalfExtents { get; }

        public ArenaAabb Bounds
        {
            get { return new ArenaAabb(Center - HalfExtents, Center + HalfExtents); }
        }
    }

    public sealed class BakedWall
    {
        public BakedWall(
            ArenaElementId id,
            float2 start,
            float2 end,
            float2 normal,
            float thickness,
            ArenaAabb bounds,
            ArenaAabb playerExpandedBounds)
        {
            Id = id;
            Start = start;
            End = end;
            Normal = normal;
            Thickness = thickness;
            Bounds = bounds;
            PlayerExpandedBounds = playerExpandedBounds;
        }

        public ArenaElementId Id { get; }
        public float2 Start { get; }
        public float2 End { get; }
        public float2 Normal { get; }
        public float Thickness { get; }
        public ArenaAabb Bounds { get; }
        public ArenaAabb PlayerExpandedBounds { get; }
    }

    public sealed class BakedConvexObstacle
    {
        public BakedConvexObstacle(
            ArenaElementId id,
            IEnumerable<float2> vertices,
            IEnumerable<float2> normals,
            ArenaAabb bounds,
            ArenaAabb playerExpandedBounds)
        {
            Id = id;
            Vertices = Freeze(vertices);
            Normals = Freeze(normals);
            Bounds = bounds;
            PlayerExpandedBounds = playerExpandedBounds;
        }

        public ArenaElementId Id { get; }
        public IReadOnlyList<float2> Vertices { get; }
        public IReadOnlyList<float2> Normals { get; }
        public ArenaAabb Bounds { get; }
        public ArenaAabb PlayerExpandedBounds { get; }

        private static ReadOnlyCollection<float2> Freeze(IEnumerable<float2> values)
        {
            return Array.AsReadOnly(values == null ? Array.Empty<float2>() : new List<float2>(values).ToArray());
        }
    }

    public sealed class BakedCircle
    {
        public BakedCircle(ArenaElementId id, BakedElementKind kind, float2 center, float radius)
        {
            Id = id;
            Kind = kind;
            Center = center;
            Radius = radius;
            RadiusSquared = radius * radius;
        }

        public ArenaElementId Id { get; }
        public BakedElementKind Kind { get; }
        public float2 Center { get; }
        public float Radius { get; }
        public float RadiusSquared { get; }
    }

    public sealed class BakedShockGate
    {
        public BakedShockGate(
            ArenaElementId id,
            BakedPowerSide safeWhenPoweredSide,
            float2 center,
            float2 halfExtents,
            float2 safeSideDirection,
            ArenaAabb bounds)
        {
            Id = id;
            SafeWhenPoweredSide = safeWhenPoweredSide;
            Center = center;
            HalfExtents = halfExtents;
            SafeSideDirection = safeSideDirection;
            Bounds = bounds;
        }

        public ArenaElementId Id { get; }
        public BakedPowerSide SafeWhenPoweredSide { get; }
        public float2 Center { get; }
        public float2 HalfExtents { get; }
        public float2 SafeSideDirection { get; }
        public ArenaAabb Bounds { get; }
    }

    public sealed class BakedBoostPad
    {
        public BakedBoostPad(
            ArenaElementId id,
            BakedPowerSide side,
            IEnumerable<float2> vertices,
            IEnumerable<float2> normals,
            ArenaAabb bounds)
        {
            Id = id;
            Side = side;
            Vertices = Array.AsReadOnly(new List<float2>(vertices).ToArray());
            Normals = Array.AsReadOnly(new List<float2>(normals).ToArray());
            Bounds = bounds;
        }

        public ArenaElementId Id { get; }
        public BakedPowerSide Side { get; }
        public IReadOnlyList<float2> Vertices { get; }
        public IReadOnlyList<float2> Normals { get; }
        public ArenaAabb Bounds { get; }
    }

    public sealed class BakedSpawn
    {
        public BakedSpawn(ArenaElementId id, byte playerSlot, float2 position, float2 facingDirection)
        {
            Id = id;
            PlayerSlot = playerSlot;
            Position = position;
            FacingDirection = facingDirection;
        }

        public ArenaElementId Id { get; }
        public byte PlayerSlot { get; }
        public float2 Position { get; }
        public float2 FacingDirection { get; }
    }

    public sealed class BakedCoreReset
    {
        public BakedCoreReset(ArenaElementId id, float2 position, float pedestalRadius)
        {
            Id = id;
            Position = position;
            PedestalRadius = pedestalRadius;
            PedestalRadiusSquared = pedestalRadius * pedestalRadius;
        }

        public ArenaElementId Id { get; }
        public float2 Position { get; }
        public float PedestalRadius { get; }
        public float PedestalRadiusSquared { get; }
    }

    public sealed class BakedBarrierForbiddenVolume
    {
        public BakedBarrierForbiddenVolume(
            ArenaElementId id,
            BakedForbiddenShape shape,
            float2 center,
            float radius,
            float2 halfExtents,
            string reason)
        {
            Id = id;
            Shape = shape;
            Center = center;
            Radius = radius;
            RadiusSquared = radius * radius;
            HalfExtents = halfExtents;
            Reason = reason ?? string.Empty;
        }

        public ArenaElementId Id { get; }
        public BakedForbiddenShape Shape { get; }
        public float2 Center { get; }
        public float Radius { get; }
        public float RadiusSquared { get; }
        public float2 HalfExtents { get; }
        public string Reason { get; }
    }

    public sealed class BakedNavigationNode
    {
        public BakedNavigationNode(
            ArenaElementId id,
            BakedNavigationKind kind,
            float2 position,
            float connectionRadius,
            IEnumerable<ArenaElementId> neighborIds)
        {
            Id = id;
            Kind = kind;
            Position = position;
            ConnectionRadius = connectionRadius;
            NeighborIds = Array.AsReadOnly(new List<ArenaElementId>(neighborIds).ToArray());
        }

        public ArenaElementId Id { get; }
        public BakedNavigationKind Kind { get; }
        public float2 Position { get; }
        public float ConnectionRadius { get; }
        public IReadOnlyList<ArenaElementId> NeighborIds { get; }
    }

    public sealed class BakedNavigationEdge
    {
        public BakedNavigationEdge(
            ArenaElementId nodeA,
            ArenaElementId nodeB,
            float lengthSquared,
            ArenaElementId crossedGate)
        {
            NodeA = nodeA;
            NodeB = nodeB;
            LengthSquared = lengthSquared;
            CrossedGate = crossedGate;
        }

        public ArenaElementId NodeA { get; }
        public ArenaElementId NodeB { get; }
        public float LengthSquared { get; }
        public ArenaElementId CrossedGate { get; }
    }

    public sealed class BakedCameraBounds
    {
        public BakedCameraBounds(ArenaElementId id, float2 center, float2 halfExtents, float orthographicSize)
        {
            Id = id;
            Center = center;
            HalfExtents = halfExtents;
            OrthographicSize = orthographicSize;
        }

        public ArenaElementId Id { get; }
        public float2 Center { get; }
        public float2 HalfExtents { get; }
        public float OrthographicSize { get; }
    }

    public sealed class ArenaBakeData
    {
        public ArenaBakeData(
            int bakeVersion,
            string contentHash,
            string sourceScene,
            IEnumerable<ArenaElementDescriptor> elements,
            BakedArenaBounds bounds,
            IEnumerable<BakedWall> walls,
            IEnumerable<BakedConvexObstacle> obstacles,
            IEnumerable<BakedCircle> circles,
            IEnumerable<BakedShockGate> gates,
            IEnumerable<BakedBoostPad> boosts,
            IEnumerable<BakedSpawn> spawns,
            BakedCoreReset coreReset,
            IEnumerable<BakedBarrierForbiddenVolume> barrierForbiddenVolumes,
            IEnumerable<BakedNavigationNode> navigationNodes,
            IEnumerable<BakedNavigationEdge> navigationEdges,
            BakedCameraBounds cameraBounds)
        {
            BakeVersion = bakeVersion;
            ContentHash = contentHash ?? string.Empty;
            SourceScene = sourceScene ?? string.Empty;
            Elements = Freeze(elements);
            Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
            Walls = Freeze(walls);
            Obstacles = Freeze(obstacles);
            Circles = Freeze(circles);
            Gates = Freeze(gates);
            Boosts = Freeze(boosts);
            Spawns = Freeze(spawns);
            CoreReset = coreReset ?? throw new ArgumentNullException(nameof(coreReset));
            BarrierForbiddenVolumes = Freeze(barrierForbiddenVolumes);
            NavigationNodes = Freeze(navigationNodes);
            NavigationEdges = Freeze(navigationEdges);
            CameraBounds = cameraBounds ?? throw new ArgumentNullException(nameof(cameraBounds));
        }

        public int BakeVersion { get; }
        public string ContentHash { get; }
        public string SourceScene { get; }
        public IReadOnlyList<ArenaElementDescriptor> Elements { get; }
        public BakedArenaBounds Bounds { get; }
        public IReadOnlyList<BakedWall> Walls { get; }
        public IReadOnlyList<BakedConvexObstacle> Obstacles { get; }
        public IReadOnlyList<BakedCircle> Circles { get; }
        public IReadOnlyList<BakedShockGate> Gates { get; }
        public IReadOnlyList<BakedBoostPad> Boosts { get; }
        public IReadOnlyList<BakedSpawn> Spawns { get; }
        public BakedCoreReset CoreReset { get; }
        public IReadOnlyList<BakedBarrierForbiddenVolume> BarrierForbiddenVolumes { get; }
        public IReadOnlyList<BakedNavigationNode> NavigationNodes { get; }
        public IReadOnlyList<BakedNavigationEdge> NavigationEdges { get; }
        public BakedCameraBounds CameraBounds { get; }

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> values)
        {
            return Array.AsReadOnly(values == null ? Array.Empty<T>() : new List<T>(values).ToArray());
        }
    }
}
