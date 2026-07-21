using RelayZero.Arena;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    internal sealed class CoreReachability
    {
        private const int StandSampleCount = 16;
        private const float StandSampleInset = 0.98f;

        private readonly ArenaBakeData arena;
        private readonly bool[] visited;
        private readonly int[] queue;
        private readonly float2[] standDirections;

        public CoreReachability(ArenaBakeData arena)
        {
            this.arena = arena;
            visited = new bool[arena.NavigationNodes.Count];
            queue = new int[arena.NavigationNodes.Count];
            standDirections = new float2[StandSampleCount];
            for (int index = 0; index < standDirections.Length; index++)
            {
                float angle = index * math.PI * 2f / standDirections.Length;
                standDirections[index] = new float2(math.cos(angle), math.sin(angle));
            }
        }

        public bool IsReachableByEitherPlayer(
            float2 corePosition,
            in PlayerState playerZero,
            in PlayerState playerOne,
            in PlayerMovementConfig playerConfig,
            in CoreConfig coreConfig)
        {
            if (CanPickupFrom(playerZero.Position, corePosition, in coreConfig) ||
                CanPickupFrom(playerOne.Position, corePosition, in coreConfig))
            {
                return true;
            }

            if (arena.NavigationNodes.Count == 0)
            {
                return HasReachableStandPoint(
                        playerZero.Position,
                        corePosition,
                        in playerConfig,
                        in coreConfig) ||
                    HasReachableStandPoint(
                        playerOne.Position,
                        corePosition,
                        in playerConfig,
                        in coreConfig);
            }

            ResetScratch();
            int readIndex = 0;
            int writeIndex = 0;
            for (int nodeIndex = 0; nodeIndex < arena.NavigationNodes.Count; nodeIndex++)
            {
                BakedNavigationNode node = arena.NavigationNodes[nodeIndex];
                if (node.Kind != BakedNavigationKind.Spawn ||
                    !StaticPlayerCollisionQueries.IsValidPosition(
                        arena,
                        node.Position,
                        playerConfig.RadiusMeters))
                {
                    continue;
                }

                visited[nodeIndex] = true;
                queue[writeIndex++] = nodeIndex;
            }

            while (readIndex < writeIndex)
            {
                int nodeIndex = queue[readIndex++];
                BakedNavigationNode node = arena.NavigationNodes[nodeIndex];
                if (CanReachCoreFromNode(node, corePosition, in playerConfig, in coreConfig))
                {
                    return true;
                }

                for (int edgeIndex = 0; edgeIndex < arena.NavigationEdges.Count; edgeIndex++)
                {
                    BakedNavigationEdge edge = arena.NavigationEdges[edgeIndex];
                    ArenaElementId neighborId;
                    if (edge.NodeA == node.Id)
                    {
                        neighborId = edge.NodeB;
                    }
                    else if (edge.NodeB == node.Id)
                    {
                        neighborId = edge.NodeA;
                    }
                    else
                    {
                        continue;
                    }

                    int neighborIndex = FindNodeIndex(neighborId);
                    if (neighborIndex < 0 || visited[neighborIndex])
                    {
                        continue;
                    }

                    visited[neighborIndex] = true;
                    queue[writeIndex++] = neighborIndex;
                }
            }

            return false;
        }

        private bool CanReachCoreFromNode(
            BakedNavigationNode node,
            float2 corePosition,
            in PlayerMovementConfig playerConfig,
            in CoreConfig coreConfig)
        {
            float maximumConnection = node.ConnectionRadius + coreConfig.InteractionRangeMeters;
            if (math.distancesq(node.Position, corePosition) > maximumConnection * maximumConnection)
            {
                return false;
            }

            float2 toNode = node.Position - corePosition;
            float distanceSquared = math.lengthsq(toNode);
            if (distanceSquared > 0.00000001f)
            {
                float standDistance = math.min(
                    coreConfig.InteractionRangeMeters * StandSampleInset,
                    math.sqrt(distanceSquared));
                float2 standPosition = corePosition + toNode *
                    (standDistance * math.rsqrt(distanceSquared));
                if (CanStandAndPickup(
                    node.Position,
                    standPosition,
                    corePosition,
                    in playerConfig,
                    in coreConfig))
                {
                    return true;
                }
            }

            return HasReachableStandPoint(
                node.Position,
                corePosition,
                in playerConfig,
                in coreConfig);
        }

        private bool HasReachableStandPoint(
            float2 pathStart,
            float2 corePosition,
            in PlayerMovementConfig playerConfig,
            in CoreConfig coreConfig)
        {
            float standDistance = coreConfig.InteractionRangeMeters * StandSampleInset;
            for (int index = 0; index < standDirections.Length; index++)
            {
                float2 standPosition = corePosition + standDirections[index] * standDistance;
                if (CanStandAndPickup(
                        pathStart,
                        standPosition,
                        corePosition,
                        in playerConfig,
                        in coreConfig))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanStandAndPickup(
            float2 pathStart,
            float2 standPosition,
            float2 corePosition,
            in PlayerMovementConfig playerConfig,
            in CoreConfig coreConfig)
        {
            return math.distancesq(standPosition, corePosition) <= coreConfig.InteractionRangeSquared &&
                StaticPlayerCollisionQueries.IsValidPosition(
                    arena,
                    standPosition,
                    playerConfig.RadiusMeters) &&
                CoreInteractionQueries.IsPathClear(
                    arena,
                    pathStart,
                    standPosition,
                    playerConfig.RadiusMeters) &&
                CoreInteractionQueries.IsPickupLineClear(arena, standPosition, corePosition);
        }

        private bool CanPickupFrom(float2 playerPosition, float2 corePosition, in CoreConfig config)
        {
            return math.distancesq(playerPosition, corePosition) <= config.InteractionRangeSquared &&
                CoreInteractionQueries.IsPickupLineClear(arena, playerPosition, corePosition);
        }

        private void ResetScratch()
        {
            for (int index = 0; index < visited.Length; index++)
            {
                visited[index] = false;
            }
        }

        private int FindNodeIndex(ArenaElementId id)
        {
            for (int index = 0; index < arena.NavigationNodes.Count; index++)
            {
                if (arena.NavigationNodes[index].Id == id)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
