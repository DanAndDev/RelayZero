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
        private CoreReachability coreReachability = null!;
        private readonly CollisionDiagnosticBuffer collisionDiagnostics = new CollisionDiagnosticBuffer();
        private readonly CoreDiagnosticBuffer coreDiagnostics = new CoreDiagnosticBuffer();
        private CoreForcedDropRequest pendingForcedCoreDrop;
        private bool hasPendingForcedCoreDrop;

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

        public CoreDiagnosticBuffer CoreDiagnostics
        {
            get
            {
                EnsureInitialized();
                return coreDiagnostics;
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
            RegulationConfig regulationConfig = validatedConfig.Regulation;
            state = new MatchState(in initialization, bakedArena.CoreReset.Position, in regulationConfig);
            coreReachability = new CoreReachability(bakedArena);
            collisionDiagnostics.Reset(SimulationTick.Zero);
            coreDiagnostics.Reset(SimulationTick.Zero);

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
            ScoreRuntimeState initialScore = state.Score;
            MatchClockState initialClock = state.Clock;
            EnsureValidRuleState(in initialScore, in initialClock, in regulationConfig, state.Tick);
            if (!CoreInteractionQueries.IsCorePositionValid(
                    bakedArena,
                    bakedArena.CoreReset.Position,
                    validatedConfig.Core.RadiusMeters))
            {
                throw new InvalidOperationException("Baked core reset position is invalid for the configured core radius.");
            }

            IsInitialized = true;
        }

        public bool TryQueueForcedCoreDrop(in CoreForcedDropRequest request)
        {
            EnsureInitialized();
            if (!request.IsValid)
            {
                throw new ArgumentException("A queued forced core drop must be valid.", nameof(request));
            }

            if (hasPendingForcedCoreDrop || state.Clock.Phase != MatchPhase.Regulation ||
                state.Core.Mode != CoreMode.Carried || !state.Core.HasOwner)
            {
                return false;
            }

            pendingForcedCoreDrop = request;
            hasPendingForcedCoreDrop = true;
            return true;
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

            if (state.Clock.Phase == MatchPhase.Results || state.Clock.Phase == MatchPhase.OvertimeReset)
            {
                eventBuffer.Reset(state.Tick);
                collisionDiagnostics.Reset(state.Tick);
                coreDiagnostics.Reset(state.Tick);
                hasPendingForcedCoreDrop = false;
                pendingForcedCoreDrop = default;
                return;
            }

            SimulationTick nextTick = state.Tick.Next();
            PlayerState playerZero = state.GetPlayer(PlayerSlot.Zero);
            PlayerState playerOne = state.GetPlayer(PlayerSlot.One);
            InputCommand playerZeroInput = inputs.Get(PlayerSlot.Zero);
            InputCommand playerOneInput = inputs.Get(PlayerSlot.One);
            PlayerMovementConfig playerConfig = config.Player;
            CoreConfig coreConfig = config.Core;
            RegulationConfig regulationConfig = config.Regulation;
            MatchClockState clock = state.Clock;
            ScoreRuntimeState score = state.Score;
            MatchClockState previousClock = clock;
            ScoreRuntimeState previousScore = score;
            float2 playerZeroSweepStart = playerZero.Position;
            float2 playerOneSweepStart = playerOne.Position;

            eventBuffer.Reset(nextTick);
            collisionDiagnostics.Reset(nextTick);
            coreDiagnostics.Reset(nextTick);
            bool gameplayEnabled = MatchClockSystem.BeginTick(ref clock, nextTick, eventBuffer);
            if (!gameplayEnabled)
            {
                if (clock.Phase == MatchPhase.Countdown)
                {
                    CoreSystem.ConsumeBlockedInteractionEdges(
                        ref playerZero,
                        ref playerOne,
                        in playerZeroInput,
                        in playerOneInput);
                }

                playerZero.Velocity = float2.zero;
                playerOne.Velocity = float2.zero;
                EnsureValidRuleTransition(
                    in previousScore,
                    in previousClock,
                    in score,
                    in clock,
                    in regulationConfig,
                    nextTick);
                state.SetPlayer(PlayerSlot.Zero, in playerZero);
                state.SetPlayer(PlayerSlot.One, in playerOne);
                state.Clock = clock;
                state.Tick = nextTick;
                hasPendingForcedCoreDrop = false;
                pendingForcedCoreDrop = default;
                return;
            }

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

            CoreRuntimeState core = state.Core;
            bool processForcedDrop = hasPendingForcedCoreDrop;
            CoreForcedDropRequest forcedDrop = pendingForcedCoreDrop;
            hasPendingForcedCoreDrop = false;
            pendingForcedCoreDrop = default;
            CoreSystem.Step(
                ref core,
                ref playerZero,
                ref playerOne,
                in playerZeroInput,
                in playerOneInput,
                processForcedDrop,
                in forcedDrop,
                in coreConfig,
                in playerConfig,
                regulationConfig.PossessionGraceTicks,
                arena,
                coreReachability,
                nextTick,
                eventBuffer,
                coreDiagnostics);
            EnsureValidCore(in core, in playerZero, in playerOne);

            RelayScoringSystem.Step(
                ref score,
                in core,
                in playerZero,
                in playerOne,
                false,
                in regulationConfig,
                nextTick,
                eventBuffer);
            MatchClockSystem.EndRegulationTick(
                ref clock,
                in score,
                in regulationConfig,
                nextTick,
                eventBuffer);
            if (clock.Phase != MatchPhase.Regulation)
            {
                playerZero.Velocity = float2.zero;
                playerOne.Velocity = float2.zero;
                core.Velocity = float2.zero;
            }

            EnsureValidRuleTransition(
                in previousScore,
                in previousClock,
                in score,
                in clock,
                in regulationConfig,
                nextTick);

            state.SetPlayer(PlayerSlot.Zero, in playerZero);
            state.SetPlayer(PlayerSlot.One, in playerOne);
            state.Core = core;
            state.Score = score;
            state.Clock = clock;
            state.Tick = nextTick;
        }

        public ulong ComputeChecksum()
        {
            EnsureInitialized();
            ulong hash = ChecksumOffset;
            hash = Add(hash, state.MatchId.Value);
            hash = Add(hash, state.MatchSeed);
            hash = Add(hash, state.Tick.Value);
            hash = Add(hash, config.Version.Upper);
            hash = Add(hash, config.Version.Lower);
            hash = Add(hash, unchecked((uint)arena.BakeVersion));
            hash = Add(hash, arena.ContentHash);

            for (byte index = 0; index < MatchState.PlayerCount; index++)
            {
                PlayerState player = state.GetPlayer(new PlayerSlot(index));
                hash = Add(hash, player.PlayerId.Value);
                hash = Add(hash, math.asuint(player.Position.x));
                hash = Add(hash, math.asuint(player.Position.y));
                hash = Add(hash, math.asuint(player.LastValidPosition.x));
                hash = Add(hash, math.asuint(player.LastValidPosition.y));
                hash = Add(hash, math.asuint(player.Velocity.x));
                hash = Add(hash, math.asuint(player.Velocity.y));
                hash = Add(hash, math.asuint(player.FacingDirection.x));
                hash = Add(hash, math.asuint(player.FacingDirection.y));
                hash = Add(hash, player.IsCarrying ? 1u : 0u);
                hash = Add(hash, (uint)player.LocomotionMode);
                hash = Add(hash, (uint)player.ActionMode);
                hash = Add(hash, (uint)player.ConnectionMode);
                hash = Add(hash, player.HasConsumedInteractSequence ? 1u : 0u);
                hash = Add(hash, player.LastConsumedInteractSequence.Value);
            }

            CoreRuntimeState core = state.Core;
            hash = Add(hash, (uint)core.Mode);
            hash = Add(hash, core.HasOwner ? 1u : 0u);
            hash = Add(hash, core.Owner.Value);
            hash = Add(hash, math.asuint(core.Position.x));
            hash = Add(hash, math.asuint(core.Position.y));
            hash = Add(hash, math.asuint(core.LastValidPosition.x));
            hash = Add(hash, math.asuint(core.LastValidPosition.y));
            hash = Add(hash, math.asuint(core.ResetOriginPosition.x));
            hash = Add(hash, math.asuint(core.ResetOriginPosition.y));
            hash = Add(hash, math.asuint(core.Velocity.x));
            hash = Add(hash, math.asuint(core.Velocity.y));
            hash = Add(hash, core.ModeEndTick.Value);
            hash = Add(hash, core.PlayerZeroPickupLockEndTick.Value);
            hash = Add(hash, core.PlayerOnePickupLockEndTick.Value);
            hash = Add(hash, core.PossessionGraceEndTick.Value);
            hash = Add(hash, core.InvalidTickCount);
            hash = Add(hash, core.RestTickCount);
            hash = Add(hash, core.IsResting ? 1u : 0u);
            hash = Add(hash, core.ResetCompletionPending ? 1u : 0u);
            hash = Add(hash, (uint)core.LastDropReason);
            hash = Add(hash, core.LastDropActionId);

            ScoreRuntimeState score = state.Score;
            hash = Add(hash, unchecked((uint)score.PlayerZeroMilliPoints));
            hash = Add(hash, unchecked((uint)score.PlayerOneMilliPoints));
            hash = Add(hash, unchecked((uint)score.PlayerZeroRateRemainder));
            hash = Add(hash, unchecked((uint)score.PlayerOneRateRemainder));

            MatchClockState clock = state.Clock;
            hash = Add(hash, (uint)clock.Phase);
            hash = Add(hash, clock.PhaseStartTick.Value);
            hash = Add(hash, clock.PhaseEndTick.Value);
            hash = Add(hash, clock.RegulationTicksRemaining);
            hash = Add(hash, clock.HasResult ? 1u : 0u);
            if (clock.HasResult)
            {
                MatchResult result = clock.Result;
                hash = Add(hash, (uint)result.Outcome);
                hash = Add(hash, (uint)result.Reason);
                hash = Add(hash, result.HasWinner ? 1u : 0u);
                hash = Add(hash, result.Winner.Value);
                hash = Add(hash, result.FinalizedTick.Value);
                hash = Add(hash, unchecked((uint)result.PlayerZeroScoreMilliPoints));
                hash = Add(hash, unchecked((uint)result.PlayerOneScoreMilliPoints));
                hash = Add(hash, result.RegulationTicksRemaining);
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

        private static ulong Add(ulong hash, string value)
        {
            hash = Add(hash, unchecked((uint)value.Length));
            for (int index = 0; index < value.Length; index++)
            {
                hash = Add(hash, (uint)value[index]);
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

        private static void EnsureValidCore(
            in CoreRuntimeState core,
            in PlayerState playerZero,
            in PlayerState playerOne)
        {
            if (!math.all(math.isfinite(core.Position)) ||
                !math.all(math.isfinite(core.LastValidPosition)) ||
                !math.all(math.isfinite(core.ResetOriginPosition)) ||
                !math.all(math.isfinite(core.Velocity)) ||
                (uint)core.Mode > (uint)CoreMode.Resetting)
            {
                throw new InvalidOperationException("Simulation produced an invalid core state.");
            }

            bool carried = core.Mode == CoreMode.Carried;
            if (carried != core.HasOwner ||
                playerZero.IsCarrying != core.IsCarriedBy(PlayerSlot.Zero) ||
                playerOne.IsCarrying != core.IsCarriedBy(PlayerSlot.One) ||
                (playerZero.IsCarrying && playerOne.IsCarrying))
            {
                throw new InvalidOperationException("Core ownership and carrier state diverged.");
            }

            if (carried)
            {
                float2 ownerPosition = core.Owner == PlayerSlot.Zero
                    ? playerZero.Position
                    : playerOne.Position;
                if (!math.all(core.Position == ownerPosition) || math.lengthsq(core.Velocity) != 0f)
                {
                    throw new InvalidOperationException("Carried core pose must match its authoritative owner.");
                }
            }
        }

        private static void EnsureValidRuleState(
            in ScoreRuntimeState score,
            in MatchClockState clock,
            in RegulationConfig config,
            SimulationTick tick)
        {
            if (score.PlayerZeroMilliPoints < 0 ||
                score.PlayerOneMilliPoints < 0 ||
                score.PlayerZeroMilliPoints > config.ScoreTargetMilliPoints ||
                score.PlayerOneMilliPoints > config.ScoreTargetMilliPoints ||
                score.PlayerZeroRateRemainder < 0 ||
                score.PlayerOneRateRemainder < 0 ||
                score.PlayerZeroRateRemainder >= SimulationTime.TicksPerSecond ||
                score.PlayerOneRateRemainder >= SimulationTime.TicksPerSecond)
            {
                throw new InvalidOperationException("Simulation produced an invalid score accumulator.");
            }

            if ((uint)clock.Phase > (uint)MatchPhase.Results ||
                clock.RegulationTicksRemaining > config.RegulationDurationTicks ||
                (clock.Phase == MatchPhase.Regulation && clock.RegulationTicksRemaining == 0u) ||
                (clock.Phase == MatchPhase.OvertimeReset && clock.RegulationTicksRemaining != 0u))
            {
                throw new InvalidOperationException("Simulation produced an invalid match clock state.");
            }

            bool resultPhase = clock.Phase == MatchPhase.Finalizing || clock.Phase == MatchPhase.Results;
            if (clock.HasResult != resultPhase)
            {
                throw new InvalidOperationException("Finalizing and Results require exactly one immutable result.");
            }

            if (!clock.HasResult)
            {
                if (clock.Result.IsFinal)
                {
                    throw new InvalidOperationException("A non-final match cannot contain a finalized result.");
                }

                return;
            }

            MatchResult result = clock.Result;
            if (!result.IsFinal || !result.HasWinner ||
                result.Outcome != MatchResultOutcome.PlayerVictory ||
                result.FinalizedTick.IsNewerThan(tick) ||
                result.PlayerZeroScoreMilliPoints != score.PlayerZeroMilliPoints ||
                result.PlayerOneScoreMilliPoints != score.PlayerOneMilliPoints ||
                result.RegulationTicksRemaining != clock.RegulationTicksRemaining)
            {
                throw new InvalidOperationException("The immutable match result diverged from finalized authority.");
            }
        }

        private static void EnsureValidRuleTransition(
            in ScoreRuntimeState previousScore,
            in MatchClockState previousClock,
            in ScoreRuntimeState score,
            in MatchClockState clock,
            in RegulationConfig config,
            SimulationTick tick)
        {
            EnsureValidRuleState(in score, in clock, in config, tick);
            if (score.PlayerZeroMilliPoints < previousScore.PlayerZeroMilliPoints ||
                score.PlayerOneMilliPoints < previousScore.PlayerOneMilliPoints)
            {
                throw new InvalidOperationException("Authoritative score cannot decrease.");
            }

            if (previousClock.HasResult &&
                (!clock.HasResult || !ResultsEqual(previousClock.Result, clock.Result)))
            {
                throw new InvalidOperationException("A finalized result cannot change.");
            }
        }

        private static bool ResultsEqual(MatchResult left, MatchResult right)
        {
            return left.Outcome == right.Outcome &&
                left.Reason == right.Reason &&
                left.HasWinner == right.HasWinner &&
                left.Winner == right.Winner &&
                left.FinalizedTick == right.FinalizedTick &&
                left.PlayerZeroScoreMilliPoints == right.PlayerZeroScoreMilliPoints &&
                left.PlayerOneScoreMilliPoints == right.PlayerOneScoreMilliPoints &&
                left.RegulationTicksRemaining == right.RegulationTicksRemaining;
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
