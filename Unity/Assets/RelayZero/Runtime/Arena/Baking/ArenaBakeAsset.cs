using System;
using RelayZero.Arena;
using UnityEngine;

namespace RelayZero.Arena.Baking
{
    [CreateAssetMenu(fileName = "ArenaBakeAsset", menuName = "Relay Zero/Arena Bake Asset")]
    public sealed class ArenaBakeAsset : ScriptableObject
    {
        [SerializeField]
        private int bakeVersion = 1;

        [SerializeField]
        private string contentHash = string.Empty;

        [SerializeField]
        private string sourceScene = string.Empty;

        [SerializeField]
        private ArenaElementRecord[] elements = Array.Empty<ArenaElementRecord>();

        [SerializeField]
        private ArenaBoundsRecord bounds;

        [SerializeField]
        private ArenaWallRecord[] walls = Array.Empty<ArenaWallRecord>();

        [SerializeField]
        private ArenaPolygonRecord[] obstacles = Array.Empty<ArenaPolygonRecord>();

        [SerializeField]
        private ArenaCircleRecord[] circles = Array.Empty<ArenaCircleRecord>();

        [SerializeField]
        private ArenaGateRecord[] gates = Array.Empty<ArenaGateRecord>();

        [SerializeField]
        private ArenaPolygonRecord[] boosts = Array.Empty<ArenaPolygonRecord>();

        [SerializeField]
        private ArenaSpawnRecord[] spawns = Array.Empty<ArenaSpawnRecord>();

        [SerializeField]
        private ArenaCoreResetRecord coreReset;

        [SerializeField]
        private ArenaForbiddenRecord[] barrierForbiddenVolumes = Array.Empty<ArenaForbiddenRecord>();

        [SerializeField]
        private ArenaNavigationNodeRecord[] navigationNodes = Array.Empty<ArenaNavigationNodeRecord>();

        [SerializeField]
        private ArenaNavigationEdgeRecord[] navigationEdges = Array.Empty<ArenaNavigationEdgeRecord>();

        [SerializeField]
        private ArenaCameraBoundsRecord cameraBounds;

        public int BakeVersion => bakeVersion;
        public string ContentHash => contentHash;
        public string SourceScene => sourceScene;
        public int ElementCount => elements.Length;
        public int WallCount => walls.Length;
        public int ObstacleCount => obstacles.Length;
        public int CircleCount => circles.Length;
        public int GateCount => gates.Length;
        public int BoostCount => boosts.Length;
        public int SpawnCount => spawns.Length;
        public int BarrierForbiddenCount => barrierForbiddenVolumes.Length;
        public int NavigationNodeCount => navigationNodes.Length;
        public int NavigationEdgeCount => navigationEdges.Length;
        public int TransformReferenceCount => 0;

        public ArenaBakeData CreateRuntimeData()
        {
            return CreatePayload().ToRuntimeData(contentHash);
        }

#if UNITY_EDITOR
        public void ApplyBake(ArenaBakePayload payload, string hash)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            bakeVersion = payload.BakeVersion;
            contentHash = hash ?? string.Empty;
            sourceScene = payload.SourceScene ?? string.Empty;
            elements = payload.Elements ?? Array.Empty<ArenaElementRecord>();
            bounds = payload.Bounds;
            walls = payload.Walls ?? Array.Empty<ArenaWallRecord>();
            obstacles = payload.Obstacles ?? Array.Empty<ArenaPolygonRecord>();
            circles = payload.Circles ?? Array.Empty<ArenaCircleRecord>();
            gates = payload.Gates ?? Array.Empty<ArenaGateRecord>();
            boosts = payload.Boosts ?? Array.Empty<ArenaPolygonRecord>();
            spawns = payload.Spawns ?? Array.Empty<ArenaSpawnRecord>();
            coreReset = payload.CoreReset;
            barrierForbiddenVolumes = payload.BarrierForbiddenVolumes ?? Array.Empty<ArenaForbiddenRecord>();
            navigationNodes = payload.NavigationNodes ?? Array.Empty<ArenaNavigationNodeRecord>();
            navigationEdges = payload.NavigationEdges ?? Array.Empty<ArenaNavigationEdgeRecord>();
            cameraBounds = payload.CameraBounds;
        }
#endif

        private ArenaBakePayload CreatePayload()
        {
            return new ArenaBakePayload
            {
                BakeVersion = bakeVersion,
                SourceScene = sourceScene,
                Elements = elements,
                Bounds = bounds,
                Walls = walls,
                Obstacles = obstacles,
                Circles = circles,
                Gates = gates,
                Boosts = boosts,
                Spawns = spawns,
                CoreReset = coreReset,
                BarrierForbiddenVolumes = barrierForbiddenVolumes,
                NavigationNodes = navigationNodes,
                NavigationEdges = navigationEdges,
                CameraBounds = cameraBounds,
            };
        }
    }
}
