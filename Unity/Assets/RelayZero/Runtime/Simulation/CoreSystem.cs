using RelayZero.Arena;
using RelayZero.Foundation;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    internal static class CoreSystem
    {
        public static void ConsumeBlockedInteractionEdges(
            ref PlayerState playerZero,
            ref PlayerState playerOne,
            in InputCommand playerZeroInput,
            in InputCommand playerOneInput)
        {
            TryConsumeInteract(ref playerZero, in playerZeroInput);
            TryConsumeInteract(ref playerOne, in playerOneInput);
        }

        public static void Step(
            ref CoreRuntimeState core,
            ref PlayerState playerZero,
            ref PlayerState playerOne,
            in InputCommand playerZeroInput,
            in InputCommand playerOneInput,
            bool hasForcedDrop,
            in CoreForcedDropRequest forcedDrop,
            in CoreConfig coreConfig,
            in PlayerMovementConfig playerConfig,
            uint possessionGraceTicks,
            ArenaBakeData arena,
            CoreReachability reachability,
            SimulationTick tick,
            MatchEventBuffer events,
            CoreDiagnosticBuffer diagnostics)
        {
            AdvanceTimedState(ref core, tick, in coreConfig, arena, events, diagnostics);

            if (hasForcedDrop && forcedDrop.IsValid && core.Mode == CoreMode.Carried && core.HasOwner)
            {
                PlayerSlot previousOwner = core.Owner;
                PlayerState owner = previousOwner == PlayerSlot.Zero ? playerZero : playerOne;
                SimulationTick lockEndTick = tick + coreConfig.ForcedDropPickupLockTicks;
                CoreDropOperation operation = new CoreDropOperation(
                    forcedDrop.Reason,
                    previousOwner,
                    owner.Position,
                    forcedDrop.InitialVelocity,
                    lockEndTick,
                    lockEndTick,
                    lockEndTick,
                    forcedDrop.ActionId);
                ApplyDrop(ref core, ref playerZero, ref playerOne, in operation, events, diagnostics);
            }

            bool playerZeroInteracted = TryConsumeInteract(ref playerZero, in playerZeroInput);
            bool playerOneInteracted = TryConsumeInteract(ref playerOne, in playerOneInput);

            if (core.Mode == CoreMode.Carried && core.HasOwner)
            {
                PlayerSlot ownerSlot = core.Owner;
                PlayerState owner = ownerSlot == PlayerSlot.Zero ? playerZero : playerOne;
                core.Position = owner.Position;
                core.LastValidPosition = owner.Position;
                core.Velocity = float2.zero;
                core.InvalidTickCount = 0u;
                core.RestTickCount = 0u;
                core.IsResting = false;

                bool ownerInteracted = ownerSlot == PlayerSlot.Zero
                    ? playerZeroInteracted
                    : playerOneInteracted;
                if (ownerInteracted && owner.CanInteract)
                {
                    ApplyManualDrop(
                        ref core,
                        ref playerZero,
                        ref playerOne,
                        in owner,
                        ownerSlot == PlayerSlot.Zero ? playerZeroInput.Sequence.Value : playerOneInput.Sequence.Value,
                        tick,
                        in coreConfig,
                        events,
                        diagnostics);
                }
            }

            bool canSimulateLoose = core.Mode == CoreMode.Loose || core.Mode == CoreMode.DropLock;
            if (!canSimulateLoose)
            {
                return;
            }

            StaticCoreCollisionSolver.Step(ref core, in coreConfig, arena, diagnostics);
            UpdateRest(ref core, in coreConfig, diagnostics);

            bool validGeometry = CoreInteractionQueries.IsCorePositionValid(
                arena,
                core.Position,
                coreConfig.RadiusMeters);
            bool reachable = validGeometry && reachability.IsReachableByEitherPlayer(
                core.Position,
                in playerZero,
                in playerOne,
                in playerConfig,
                in coreConfig);
            if (!validGeometry || !reachable)
            {
                core.InvalidTickCount++;
                diagnostics.Add(
                    CoreDiagnosticKind.InvalidOrUnreachable,
                    core.Mode,
                    core.Position,
                    core.Velocity,
                    float2.zero,
                    ArenaElementId.None);
                if (core.InvalidTickCount >= coreConfig.InvalidResetDelayTicks)
                {
                    BeginReset(ref core, ref playerZero, ref playerOne, tick, in coreConfig, events, diagnostics);
                    return;
                }
            }
            else
            {
                core.InvalidTickCount = 0u;
                core.LastValidPosition = core.Position;
            }

            PickupCandidate zeroCandidate = EvaluatePickup(
                in core,
                in playerZero,
                in playerZeroInput,
                playerZeroInteracted,
                tick,
                in coreConfig,
                arena,
                diagnostics);
            PickupCandidate oneCandidate = EvaluatePickup(
                in core,
                in playerOne,
                in playerOneInput,
                playerOneInteracted,
                tick,
                in coreConfig,
                arena,
                diagnostics);

            if (!zeroCandidate.IsValid && !oneCandidate.IsValid)
            {
                return;
            }

            PickupCandidate winner = ResolvePickup(in zeroCandidate, in oneCandidate, in coreConfig);
            ApplyPickup(
                ref core,
                ref playerZero,
                ref playerOne,
                in winner,
                in playerConfig,
                possessionGraceTicks,
                tick,
                events,
                diagnostics);
        }

        private static void AdvanceTimedState(
            ref CoreRuntimeState core,
            SimulationTick tick,
            in CoreConfig config,
            ArenaBakeData arena,
            MatchEventBuffer events,
            CoreDiagnosticBuffer diagnostics)
        {
            if (core.Mode == CoreMode.Resetting && !core.ModeEndTick.IsNewerThan(tick))
            {
                core.Mode = CoreMode.Locked;
                core.Position = arena.CoreReset.Position;
                core.LastValidPosition = arena.CoreReset.Position;
                core.Velocity = float2.zero;
                core.InvalidTickCount = 0u;
                core.RestTickCount = 0u;
                core.IsResting = true;
                core.ModeEndTick = tick + config.CenterLockDurationTicks;
                core.PlayerZeroPickupLockEndTick = core.ModeEndTick;
                core.PlayerOnePickupLockEndTick = core.ModeEndTick;
                diagnostics.Add(
                    CoreDiagnosticKind.StateChanged,
                    core.Mode,
                    core.Position,
                    core.Velocity,
                    float2.zero,
                    arena.CoreReset.Id);
                return;
            }

            if (core.Mode == CoreMode.Locked && !core.ModeEndTick.IsNewerThan(tick))
            {
                core.Mode = CoreMode.Loose;
                core.ModeEndTick = SimulationTick.Zero;
                core.PlayerZeroPickupLockEndTick = SimulationTick.Zero;
                core.PlayerOnePickupLockEndTick = SimulationTick.Zero;
                core.IsResting = true;
                diagnostics.Add(
                    CoreDiagnosticKind.StateChanged,
                    core.Mode,
                    core.Position,
                    core.Velocity,
                    float2.zero,
                    arena.CoreReset.Id);
                if (core.ResetCompletionPending)
                {
                    core.ResetCompletionPending = false;
                    events.Add(MatchEventType.CoreResetCompleted);
                }
            }

            if (core.Mode == CoreMode.DropLock && !core.ModeEndTick.IsNewerThan(tick))
            {
                core.Mode = CoreMode.Loose;
                diagnostics.Add(
                    CoreDiagnosticKind.StateChanged,
                    core.Mode,
                    core.Position,
                    core.Velocity,
                    float2.zero,
                    ArenaElementId.None);
            }
        }

        private static bool TryConsumeInteract(ref PlayerState player, in InputCommand command)
        {
            if ((command.PressedButtons & InputButtons.Interact) == 0)
            {
                return false;
            }

            if (player.HasConsumedInteractSequence &&
                !command.Sequence.IsNewerThan(player.LastConsumedInteractSequence))
            {
                return false;
            }

            player.HasConsumedInteractSequence = true;
            player.LastConsumedInteractSequence = command.Sequence;
            return true;
        }

        private static void ApplyManualDrop(
            ref CoreRuntimeState core,
            ref PlayerState playerZero,
            ref PlayerState playerOne,
            in PlayerState owner,
            uint actionId,
            SimulationTick tick,
            in CoreConfig config,
            MatchEventBuffer events,
            CoreDiagnosticBuffer diagnostics)
        {
            SimulationTick opponentLockEnd = tick + config.ManualDropOpponentPickupLockTicks;
            SimulationTick previousOwnerLockEnd = tick + config.ManualDropPreviousOwnerPickupLockTicks;
            SimulationTick zeroLockEnd = owner.Slot == PlayerSlot.Zero
                ? previousOwnerLockEnd
                : opponentLockEnd;
            SimulationTick oneLockEnd = owner.Slot == PlayerSlot.One
                ? previousOwnerLockEnd
                : opponentLockEnd;
            CoreDropOperation operation = new CoreDropOperation(
                CoreDropReason.Manual,
                owner.Slot,
                owner.Position,
                owner.FacingDirection * config.ManualDropSpeedMetersPerSecond,
                opponentLockEnd,
                zeroLockEnd,
                oneLockEnd,
                actionId);
            ApplyDrop(ref core, ref playerZero, ref playerOne, in operation, events, diagnostics);
        }

        private static void ApplyDrop(
            ref CoreRuntimeState core,
            ref PlayerState playerZero,
            ref PlayerState playerOne,
            in CoreDropOperation operation,
            MatchEventBuffer events,
            CoreDiagnosticBuffer diagnostics)
        {
            core.Mode = CoreMode.DropLock;
            core.HasOwner = false;
            core.Owner = PlayerSlot.Zero;
            core.Position = operation.InitialPosition;
            core.LastValidPosition = operation.InitialPosition;
            core.Velocity = operation.InitialVelocity;
            core.ModeEndTick = operation.ModeLockEndTick;
            core.PlayerZeroPickupLockEndTick = operation.PlayerZeroPickupLockEndTick;
            core.PlayerOnePickupLockEndTick = operation.PlayerOnePickupLockEndTick;
            core.PossessionGraceEndTick = SimulationTick.Zero;
            core.InvalidTickCount = 0u;
            core.RestTickCount = 0u;
            core.IsResting = false;
            core.LastDropReason = operation.Reason;
            core.LastDropActionId = operation.ActionId;
            playerZero.IsCarrying = false;
            playerOne.IsCarrying = false;
            events.Add(MatchEventType.CoreDropped, operation.PreviousOwner, (int)operation.Reason);
            diagnostics.Add(
                CoreDiagnosticKind.StateChanged,
                core.Mode,
                core.Position,
                core.Velocity,
                float2.zero,
                ArenaElementId.None,
                true,
                operation.PreviousOwner);
        }

        private static void UpdateRest(
            ref CoreRuntimeState core,
            in CoreConfig config,
            CoreDiagnosticBuffer diagnostics)
        {
            if (math.lengthsq(core.Velocity) >= config.RestSpeedThresholdSquared)
            {
                core.RestTickCount = 0u;
                core.IsResting = false;
                return;
            }

            if (core.RestTickCount < config.RestQualificationTicks)
            {
                core.RestTickCount++;
            }

            if (core.RestTickCount < config.RestQualificationTicks)
            {
                return;
            }

            bool newlyResting = !core.IsResting;
            core.Velocity = float2.zero;
            core.IsResting = true;
            if (newlyResting)
            {
                diagnostics.Add(
                    CoreDiagnosticKind.Rested,
                    core.Mode,
                    core.Position,
                    core.Velocity,
                    float2.zero,
                    ArenaElementId.None);
            }
        }

        private static PickupCandidate EvaluatePickup(
            in CoreRuntimeState core,
            in PlayerState player,
            in InputCommand command,
            bool interacted,
            SimulationTick tick,
            in CoreConfig config,
            ArenaBakeData arena,
            CoreDiagnosticBuffer diagnostics)
        {
            if (!interacted || !player.CanInteract)
            {
                return default;
            }

            if (!core.IsPickupEligible(player.Slot, tick))
            {
                diagnostics.Add(
                    CoreDiagnosticKind.PickupRejectedLocked,
                    core.Mode,
                    core.Position,
                    core.Velocity,
                    float2.zero,
                    ArenaElementId.None,
                    true,
                    player.Slot);
                return default;
            }

            float distanceSquared = math.distancesq(player.Position, core.Position);
            if (distanceSquared > config.InteractionRangeSquared)
            {
                diagnostics.Add(
                    CoreDiagnosticKind.PickupRejectedRange,
                    core.Mode,
                    core.Position,
                    core.Velocity,
                    float2.zero,
                    ArenaElementId.None,
                    true,
                    player.Slot);
                return default;
            }

            if (!CoreInteractionQueries.IsPickupLineClear(arena, player.Position, core.Position))
            {
                diagnostics.Add(
                    CoreDiagnosticKind.PickupRejectedOccluded,
                    core.Mode,
                    core.Position,
                    core.Velocity,
                    float2.zero,
                    ArenaElementId.None,
                    true,
                    player.Slot);
                return default;
            }

            return new PickupCandidate(
                player.Slot,
                math.sqrt(distanceSquared),
                GetMappedCommandTime(in command));
        }

        private static SimulationTick GetMappedCommandTime(in InputCommand command)
        {
            // The local command boundary uses simulation-epoch client ticks.
            // Session adapters validate and map remote clock domains before commands enter simulation.
            return command.ClientTick;
        }

        private static PickupCandidate ResolvePickup(
            in PickupCandidate zero,
            in PickupCandidate one,
            in CoreConfig config)
        {
            if (!zero.IsValid)
            {
                return one;
            }

            if (!one.IsValid)
            {
                return zero;
            }

            float difference = math.abs(zero.DistanceMeters - one.DistanceMeters);
            if (difference >= config.PickupDistanceTieThresholdMeters)
            {
                return zero.DistanceMeters < one.DistanceMeters ? zero : one;
            }

            if (zero.MappedCommandTick.IsNewerThan(one.MappedCommandTick))
            {
                return one;
            }

            if (one.MappedCommandTick.IsNewerThan(zero.MappedCommandTick))
            {
                return zero;
            }

            return zero.Slot.CompareTo(one.Slot) <= 0 ? zero : one;
        }

        private static void ApplyPickup(
            ref CoreRuntimeState core,
            ref PlayerState playerZero,
            ref PlayerState playerOne,
            in PickupCandidate winner,
            in PlayerMovementConfig playerConfig,
            uint possessionGraceTicks,
            SimulationTick tick,
            MatchEventBuffer events,
            CoreDiagnosticBuffer diagnostics)
        {
            core.Mode = CoreMode.Carried;
            core.HasOwner = true;
            core.Owner = winner.Slot;
            core.Position = winner.Slot == PlayerSlot.Zero ? playerZero.Position : playerOne.Position;
            core.LastValidPosition = core.Position;
            core.Velocity = float2.zero;
            core.ModeEndTick = SimulationTick.Zero;
            core.PlayerZeroPickupLockEndTick = SimulationTick.Zero;
            core.PlayerOnePickupLockEndTick = SimulationTick.Zero;
            core.PossessionGraceEndTick = tick + possessionGraceTicks;
            core.InvalidTickCount = 0u;
            core.RestTickCount = 0u;
            core.IsResting = false;
            playerZero.IsCarrying = winner.Slot == PlayerSlot.Zero;
            playerOne.IsCarrying = winner.Slot == PlayerSlot.One;
            if (winner.Slot == PlayerSlot.Zero)
            {
                ClampCarrierVelocity(ref playerZero, in playerConfig);
            }
            else
            {
                ClampCarrierVelocity(ref playerOne, in playerConfig);
            }

            events.Add(MatchEventType.CorePickedUp, winner.Slot);
            diagnostics.Add(
                CoreDiagnosticKind.StateChanged,
                core.Mode,
                core.Position,
                core.Velocity,
                float2.zero,
                ArenaElementId.None,
                true,
                winner.Slot);
        }

        private static void ClampCarrierVelocity(
            ref PlayerState carrier,
            in PlayerMovementConfig config)
        {
            float speedSquared = math.lengthsq(carrier.Velocity);
            if (speedSquared > config.CarrierMaximumSpeedSquared)
            {
                carrier.Velocity *= config.CarrierMaximumSpeedMetersPerSecond * math.rsqrt(speedSquared);
            }
        }

        private static void BeginReset(
            ref CoreRuntimeState core,
            ref PlayerState playerZero,
            ref PlayerState playerOne,
            SimulationTick tick,
            in CoreConfig config,
            MatchEventBuffer events,
            CoreDiagnosticBuffer diagnostics)
        {
            core.Mode = CoreMode.Resetting;
            core.HasOwner = false;
            core.Owner = PlayerSlot.Zero;
            core.ResetOriginPosition = core.Position;
            core.Velocity = float2.zero;
            core.ModeEndTick = tick + config.ResettingDurationTicks;
            core.PlayerZeroPickupLockEndTick = SimulationTick.Zero;
            core.PlayerOnePickupLockEndTick = SimulationTick.Zero;
            core.PossessionGraceEndTick = SimulationTick.Zero;
            core.InvalidTickCount = 0u;
            core.RestTickCount = 0u;
            core.IsResting = true;
            core.ResetCompletionPending = true;
            playerZero.IsCarrying = false;
            playerOne.IsCarrying = false;
            events.Add(MatchEventType.CoreResetStarted);
            diagnostics.Add(
                CoreDiagnosticKind.StateChanged,
                core.Mode,
                core.Position,
                core.Velocity,
                float2.zero,
                ArenaElementId.None);
        }

        private readonly struct PickupCandidate
        {
            public PickupCandidate(PlayerSlot slot, float distanceMeters, SimulationTick mappedCommandTick)
            {
                Slot = slot;
                DistanceMeters = distanceMeters;
                MappedCommandTick = mappedCommandTick;
                IsValid = true;
            }

            public bool IsValid { get; }
            public PlayerSlot Slot { get; }
            public float DistanceMeters { get; }
            public SimulationTick MappedCommandTick { get; }
        }
    }
}
