using System;
using RelayZero.Arena;
using RelayZero.Foundation;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    public sealed class MatchSimulation
    {
        private const ulong ChecksumOffset = 14695981039346656037ul;
        private const ulong ChecksumPrime = 1099511628211ul;

        private MatchConfig config = null!;
        private ArenaBakeData arena = null!;
        private MatchState state = null!;
        private readonly CollisionDiagnosticBuffer collisionDiagnostics = new CollisionDiagnosticBuffer();

        public bool IsInitialized { get; private set; }

        public MatchConfig Config
        {
            get
            {
                EnsureInitialized();
                return config;
            }
        }

        public MatchState State
        {
            get
            {
                EnsureInitialized();
                return state;
            }
        }

        public ArenaBakeData Arena
        {
            get
            {
                EnsureInitialized();
                return arena;
            }
        }

        public CollisionDiagnosticBuffer CollisionDiagnostics
        {
            get
            {
                EnsureInitialized();
                return collisionDiagnostics;
            }
        }

        public void Initialize(
            MatchConfig validatedConfig,
            ArenaBakeData bakedArena,
            in MatchInitialization initialization)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("MatchSimulation is already initialized.");
            }

            config = validatedConfig ?? throw new ArgumentNullException(nameof(validatedConfig));
            arena = bakedArena ?? throw new ArgumentNullException(nameof(bakedArena));
            if (!validatedConfig.Version.IsValid)
            {
                throw new ArgumentException("A compiled configuration version is required.", nameof(validatedConfig));
            }

            initialization.Validate();
            state = new MatchState(in initialization);
            collisionDiagnostics.Reset(SimulationTick.Zero);

            float radius = validatedConfig.Player.RadiusMeters;
            float2 zeroSpawn = GetValidSpawn(PlayerSlot.Zero, radius);
            float2 oneSpawn = GetValidSpawn(PlayerSlot.One, radius);
            PlayerState playerZero = state.GetPlayer(PlayerSlot.Zero);
            PlayerState playerOne = state.GetPlayer(PlayerSlot.One);
            RecoverInitialPosition(ref playerZero, zeroSpawn, radius);
            RecoverInitialPosition(ref playerOne, oneSpawn, radius);

            if (ArePlayersOverlapping(in playerZero, in playerOne, radius))
            {
                playerZero.Position = zeroSpawn;
                playerZero.LastValidPosition = zeroSpawn;
                playerZero.Velocity = float2.zero;
                playerOne.Position = oneSpawn;
                playerOne.LastValidPosition = oneSpawn;
                playerOne.Velocity = float2.zero;
                AddInitializationRecovery(in playerZero);
                AddInitializationRecovery(in playerOne);
            }

            if (ArePlayersOverlapping(in playerZero, in playerOne, radius))
            {
                throw new InvalidOperationException("Baked player spawns overlap for the authored player radius.");
            }

            state.SetPlayer(PlayerSlot.Zero, in playerZero);
            state.SetPlayer(PlayerSlot.One, in playerOne);
            IsInitialized = true;
        }

        public void Step(TickInputs inputs, MatchEventBuffer eventBuffer)
        {
            EnsureInitialized();
            if (inputs == null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }

            if (eventBuffer == null)
            {
                throw new ArgumentNullException(nameof(eventBuffer));
            }

            SimulationTick nextTick = state.Tick.Next();
            PlayerState playerZero = state.GetPlayer(PlayerSlot.Zero);
            PlayerState playerOne = state.GetPlayer(PlayerSlot.One);
            InputCommand playerZeroInput = inputs.Get(PlayerSlot.Zero);
            InputCommand playerOneInput = inputs.Get(PlayerSlot.One);
            PlayerMovementConfig playerConfig = config.Player;
            float2 playerZeroSweepStart = playerZero.Position;
            float2 playerOneSweepStart = playerOne.Position;

            collisionDiagnostics.Reset(nextTick);
            float2 playerZeroDisplacement = PlayerMovementSystem.Step(ref playerZero, in playerZeroInput, in playerConfig);
            float2 playerOneDisplacement = PlayerMovementSystem.Step(ref playerOne, in playerOneInput, in playerConfig);
            if (!StaticPlayerCollisionSolver.Resolve(
                    ref playerZero,
                    playerZeroDisplacement,
                    playerConfig.RadiusMeters,
                    arena,
                    nextTick,
                    collisionDiagnostics) ||
                !StaticPlayerCollisionSolver.Resolve(
                    ref playerOne,
                    playerOneDisplacement,
                    playerConfig.RadiusMeters,
                    arena,
                    nextTick,
                    collisionDiagnostics) ||
                !PlayerPairCollisionSolver.Resolve(
                    ref playerZero,
                    ref playerOne,
                    playerZeroSweepStart,
                    playerOneSweepStart,
                    playerConfig.RadiusMeters,
                    arena,
                    nextTick,
                    collisionDiagnostics))
            {
                throw new InvalidOperationException("Player collision could not recover a valid simulation position.");
            }

            EnsureValidPlayer(in playerZero, playerConfig.RadiusMeters);
            EnsureValidPlayer(in playerOne, playerConfig.RadiusMeters);
            if (ArePlayersOverlapping(in playerZero, in playerOne, playerConfig.RadiusMeters))
            {
                throw new InvalidOperationException("Player collision left the pair overlapping.");
            }

            playerZero.LastValidPosition = playerZero.Position;
            playerOne.LastValidPosition = playerOne.Position;

            eventBuffer.Reset(nextTick);
            state.SetPlayer(PlayerSlot.Zero, in playerZero);
            state.SetPlayer(PlayerSlot.One, in playerOne);
            state.Tick = nextTick;
        }

        public ulong ComputeChecksum()
        {
            EnsureInitialized();
            ulong hash = ChecksumOffset;
            hash = Add(hash, state.MatchId.Value);
            hash = Add(hash, state.Tick.Value);
            hash = Add(hash, config.Version.Upper);
            hash = Add(hash, config.Version.Lower);

            for (byte index = 0; index < MatchState.PlayerCount; index++)
            {
                PlayerState player = state.GetPlayer(new PlayerSlot(index));
                hash = Add(hash, player.PlayerId.Value);
                hash = Add(hash, math.asuint(player.Position.x));
                hash = Add(hash, math.asuint(player.Position.y));
                hash = Add(hash, math.asuint(player.Velocity.x));
                hash = Add(hash, math.asuint(player.Velocity.y));
                hash = Add(hash, math.asuint(player.FacingDirection.x));
                hash = Add(hash, math.asuint(player.FacingDirection.y));
                hash = Add(hash, player.IsCarrying ? 1u : 0u);
            }

            return hash;
        }

        private static ulong Add(ulong hash, Guid value)
        {
            Span<byte> bytes = stackalloc byte[16];
            value.TryWriteBytes(bytes);
            for (int index = 0; index < bytes.Length; index++)
            {
                hash ^= bytes[index];
                hash *= ChecksumPrime;
            }

            return hash;
        }

        private void RecoverInitialPosition(ref PlayerState player, float2 spawnFallback, float radius)
        {
            float2 authoredPosition = player.Position;
            if (!StaticPlayerCollisionQueries.TryRecoverPosition(
                    arena,
                    authoredPosition,
                    authoredPosition,
                    spawnFallback,
                    radius,
                    out float2 recovered))
            {
                throw new InvalidOperationException(
                    "Player slot " + player.Slot.Index + " has no valid authored or spawn position.");
            }

            player.Position = recovered;
            player.LastValidPosition = recovered;
            player.Velocity = float2.zero;
            if (!math.all(recovered == authoredPosition))
            {
                AddInitializationRecovery(in player);
            }
        }

        private float2 GetValidSpawn(PlayerSlot slot, float radius)
        {
            for (int index = 0; index < arena.Spawns.Count; index++)
            {
                BakedSpawn spawn = arena.Spawns[index];
                if (spawn.PlayerSlot != slot.Index)
                {
                    continue;
                }

                if (!StaticPlayerCollisionQueries.IsValidPosition(arena, spawn.Position, radius))
                {
                    throw new InvalidOperationException(
                        "Baked spawn for player slot " + slot.Index + " is not valid for the authored player radius.");
                }

                return spawn.Position;
            }

            throw new InvalidOperationException("Arena bake is missing player slot " + slot.Index + " spawn data.");
        }

        private void AddInitializationRecovery(in PlayerState player)
        {
            collisionDiagnostics.Add(
                player.Slot,
                CollisionDiagnosticKind.Recovery,
                ArenaElementId.None,
                0f,
                player.Position,
                float2.zero,
                player.Position,
                float2.zero,
                float2.zero,
                0);
        }

        private static bool ArePlayersOverlapping(in PlayerState zero, in PlayerState one, float radius)
        {
            float minimumDistance = radius * 2f - 0.00001f;
            return math.lengthsq(one.Position - zero.Position) < minimumDistance * minimumDistance;
        }

        private void EnsureValidPlayer(in PlayerState player, float radius)
        {
            if (!math.all(math.isfinite(player.Position)) ||
                !math.all(math.isfinite(player.LastValidPosition)) ||
                !math.all(math.isfinite(player.Velocity)) ||
                !math.all(math.isfinite(player.FacingDirection)) ||
                !StaticPlayerCollisionQueries.IsValidPosition(arena, player.Position, radius))
            {
                throw new InvalidOperationException("Simulation produced an invalid player state.");
            }
        }

        private static ulong Add(ulong hash, uint value)
        {
            for (int shift = 0; shift < 32; shift += 8)
            {
                hash ^= (byte)(value >> shift);
                hash *= ChecksumPrime;
            }

            return hash;
        }

        private static ulong Add(ulong hash, ulong value)
        {
            for (int shift = 0; shift < 64; shift += 8)
            {
                hash ^= (byte)(value >> shift);
                hash *= ChecksumPrime;
            }

            return hash;
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("MatchSimulation must be initialized before use.");
            }
        }
    }
}
