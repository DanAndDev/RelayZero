using System;
using System.Collections.Generic;
using System.Linq;
using RelayZero.Arena;
using RelayZero.Arena.Authoring;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RelayZero.Editor.Arena
{
    public sealed class ArenaAuthoringSnapshot
    {
        private readonly Dictionary<string, ArenaElementId> idsByStableId;

        private ArenaAuthoringSnapshot(Scene scene, ArenaElementAuthoring[] elements)
        {
            Scene = scene;
            Elements = elements;
            idsByStableId = elements
                .Where(element => !string.IsNullOrWhiteSpace(element.StableId))
                .GroupBy(element => element.StableId, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => ArenaElementId.FromStableId(group.Key),
                    StringComparer.Ordinal);

            Arena = Find<SwitchyardArenaAuthoring>();
            Bounds = Find<ArenaBoundsAuthoring>();
            Walls = FindAll<ArenaWallAuthoring>();
            Obstacles = FindAll<ConvexPylonAuthoring>();
            Relays = FindAll<RelayZoneAuthoring>();
            Terminals = FindAll<TerminalAuthoring>();
            Gates = FindAll<ShockGateAuthoring>();
            Boosts = FindAll<BoostPadAuthoring>();
            Spawns = FindAll<SpawnPointAuthoring>();
            CoreReset = Find<CoreResetPointAuthoring>();
            BarrierForbiddenVolumes = FindAll<BarrierForbiddenVolumeAuthoring>();
            NavigationHints = FindAll<NavigationHintAuthoring>();
            CameraBounds = Find<CameraBoundsAuthoring>();
        }

        public Scene Scene { get; }
        public ArenaElementAuthoring[] Elements { get; }
        public SwitchyardArenaAuthoring Arena { get; }
        public ArenaBoundsAuthoring Bounds { get; }
        public ArenaWallAuthoring[] Walls { get; }
        public ConvexPylonAuthoring[] Obstacles { get; }
        public RelayZoneAuthoring[] Relays { get; }
        public TerminalAuthoring[] Terminals { get; }
        public ShockGateAuthoring[] Gates { get; }
        public BoostPadAuthoring[] Boosts { get; }
        public SpawnPointAuthoring[] Spawns { get; }
        public CoreResetPointAuthoring CoreReset { get; }
        public BarrierForbiddenVolumeAuthoring[] BarrierForbiddenVolumes { get; }
        public NavigationHintAuthoring[] NavigationHints { get; }
        public CameraBoundsAuthoring CameraBounds { get; }

        public ArenaElementId GetId(string stableId)
        {
            return idsByStableId.TryGetValue(stableId, out ArenaElementId id)
                ? id
                : ArenaElementId.None;
        }

        public static ArenaAuthoringSnapshot Capture(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new ArgumentException("A valid loaded scene is required.", nameof(scene));
            }

            List<ArenaElementAuthoring> elements = new List<ArenaElementAuthoring>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                elements.AddRange(root.GetComponentsInChildren<ArenaElementAuthoring>(true));
            }

            return new ArenaAuthoringSnapshot(
                scene,
                elements.OrderBy(element => element.StableId, StringComparer.Ordinal).ToArray());
        }

        public static Vector2 PositionOf(Component component)
        {
            Vector3 position = component.transform.position;
            return new Vector2(position.x, position.z);
        }

        private T Find<T>()
            where T : ArenaElementAuthoring
        {
            return Elements.OfType<T>().SingleOrDefault();
        }

        private T[] FindAll<T>()
            where T : ArenaElementAuthoring
        {
            return Elements.OfType<T>()
                .OrderBy(element => element.StableId, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
