using NUnit.Framework;
using RelayZero.Foundation;
using RelayZero.Simulation;
using Unity.Mathematics;

namespace RelayZero.Tests.EditMode.Simulation
{
    public sealed class RegulationMatchTests
    {
        [Test]
        public void CountdownEndsOnGoTickAndDiscardedInteractDoesNotLeakIntoRegulation()
        {
            MatchSimulation simulation = SimulationTestFactory.CreateCountdown(
                float2.zero,
                new float2(8f, 0f));
            InputCommand countdownInteract = Interact(1u, 1u);
            float2 initialPosition = simulation.State.GetPlayer(PlayerSlot.Zero).Position;

            Assert.That(simulation.State.Clock.Phase, Is.EqualTo(MatchPhase.Countdown));
            Assert.That(
                simulation.State.Clock.GetCountdownTicksRemaining(simulation.State.Tick),
                Is.EqualTo(180u));
            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Locked));

            MatchEventBuffer blockedEvents = Step(simulation, in countdownInteract);

            Assert.That(simulation.State.Tick.Value, Is.EqualTo(1u));
            Assert.That(simulation.State.Clock.Phase, Is.EqualTo(MatchPhase.Countdown));
            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Locked));
            Assert.That(simulation.State.Core.HasOwner, Is.False);
            Assert.That(simulation.State.GetPlayer(PlayerSlot.Zero).Position, Is.EqualTo(initialPosition));
            Assert.That(blockedEvents.Count, Is.Zero);

            Advance(simulation, 178);

            Assert.That(simulation.State.Tick.Value, Is.EqualTo(179u));
            Assert.That(simulation.State.Clock.Phase, Is.EqualTo(MatchPhase.Countdown));
            Assert.That(
                simulation.State.Clock.GetCountdownTicksRemaining(simulation.State.Tick),
                Is.EqualTo(1u));
            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Locked));

            MatchEventBuffer goEvents = Step(simulation, in countdownInteract);

            Assert.That(simulation.State.Tick.Value, Is.EqualTo(180u));
            Assert.That(simulation.State.Clock.Phase, Is.EqualTo(MatchPhase.Regulation));
            Assert.That(simulation.State.Clock.RegulationTicksRemaining, Is.EqualTo(10799u));
            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Loose));
            Assert.That(simulation.State.Core.HasOwner, Is.False);
            Assert.That(goEvents.Count, Is.EqualTo(1));
            AssertEvent(goEvents[0], MatchEventType.MatchPhaseChanged, 0, (int)MatchPhase.Regulation);

            InputCommand freshInteract = Interact(181u, 2u);
            MatchEventBuffer pickupEvents = Step(simulation, in freshInteract);

            Assert.That(simulation.State.Core.IsCarriedBy(PlayerSlot.Zero), Is.True);
            Assert.That(pickupEvents.Count, Is.EqualTo(1));
            Assert.That(pickupEvents[0].Type, Is.EqualTo(MatchEventType.CorePickedUp));

            MatchSimulation freshGoSimulation = SimulationTestFactory.CreateCountdown(
                float2.zero,
                new float2(8f, 0f));
            Advance(freshGoSimulation, 179);
            InputCommand goInteract = Interact(180u, 1u);
            MatchEventBuffer goPickupEvents = Step(freshGoSimulation, in goInteract);

            Assert.That(freshGoSimulation.State.Tick.Value, Is.EqualTo(180u));
            Assert.That(freshGoSimulation.State.Clock.Phase, Is.EqualTo(MatchPhase.Regulation));
            Assert.That(freshGoSimulation.State.Core.IsCarriedBy(PlayerSlot.Zero), Is.True);
            Assert.That(goPickupEvents.Count, Is.EqualTo(2));
            AssertEvent(
                goPickupEvents[0],
                MatchEventType.MatchPhaseChanged,
                0,
                (int)MatchPhase.Regulation);
            AssertEvent(
                goPickupEvents[1],
                MatchEventType.CorePickedUp,
                1,
                0,
                PlayerSlot.Zero);
        }

        [Test]
        public void PossessionGraceAndBaseScoringUseExactTickAndMilliPointBoundaries()
        {
            MatchSimulation simulation = CreateRegulation();
            InputCommand pickup = Interact(1u, 1u);

            Step(simulation, in pickup);

            Assert.That(simulation.State.Core.IsCarriedBy(PlayerSlot.Zero), Is.True);
            Assert.That(
                simulation.State.Core.GetPossessionGraceRemainingTicks(simulation.State.Tick),
                Is.EqualTo(30u));
            Assert.That(simulation.State.Score.PlayerZeroMilliPoints, Is.Zero);
            Assert.That(simulation.State.Score.PlayerZeroRateRemainder, Is.Zero);

            Advance(simulation, 29);

            Assert.That(simulation.State.Tick.Value, Is.EqualTo(30u));
            Assert.That(
                simulation.State.Core.GetPossessionGraceRemainingTicks(simulation.State.Tick),
                Is.EqualTo(1u));
            Assert.That(simulation.State.Score.PlayerZeroMilliPoints, Is.Zero);

            MatchEventBuffer firstEligibleEvents = Step(simulation);

            Assert.That(simulation.State.Tick.Value, Is.EqualTo(31u));
            Assert.That(
                simulation.State.Core.GetPossessionGraceRemainingTicks(simulation.State.Tick),
                Is.Zero);
            Assert.That(simulation.State.Score.PlayerZeroMilliPoints, Is.EqualTo(16));
            Assert.That(simulation.State.Score.PlayerZeroRateRemainder, Is.EqualTo(40));
            Assert.That(firstEligibleEvents.Count, Is.Zero);

            Advance(simulation, 58);

            Assert.That(simulation.State.Tick.Value, Is.EqualTo(89u));
            Assert.That(simulation.State.Score.PlayerZeroMilliPoints, Is.EqualTo(983));
            Assert.That(simulation.State.Score.PlayerZeroRateRemainder, Is.EqualTo(20));
            Assert.That(simulation.State.Score.GetDisplayedPoints(PlayerSlot.Zero), Is.Zero);

            MatchEventBuffer wholePointEvents = Step(simulation);

            Assert.That(simulation.State.Tick.Value, Is.EqualTo(90u));
            Assert.That(simulation.State.Score.PlayerZeroMilliPoints, Is.EqualTo(1000));
            Assert.That(simulation.State.Score.PlayerZeroRateRemainder, Is.Zero);
            Assert.That(simulation.State.Score.GetDisplayedPoints(PlayerSlot.Zero), Is.EqualTo(1));
            Assert.That(wholePointEvents.Count, Is.EqualTo(1));
            AssertEvent(wholePointEvents[0], MatchEventType.ScoreChanged, 0, 1000, PlayerSlot.Zero);
        }

        [Test]
        public void ManualDropOnFirstEligibleScoringTickDeniesScore()
        {
            MatchSimulation simulation = CreateRegulation();
            InputCommand pickup = Interact(1u, 1u);
            Step(simulation, in pickup);
            Advance(simulation, 29);
            InputCommand drop = Interact(31u, 2u);

            MatchEventBuffer dropEvents = Step(simulation, in drop);

            Assert.That(simulation.State.Tick.Value, Is.EqualTo(31u));
            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.DropLock));
            Assert.That(simulation.State.Core.HasOwner, Is.False);
            Assert.That(simulation.State.Score.PlayerZeroMilliPoints, Is.Zero);
            Assert.That(simulation.State.Score.PlayerZeroRateRemainder, Is.Zero);
            Assert.That(dropEvents.Count, Is.EqualTo(1));
            AssertEvent(
                dropEvents[0],
                MatchEventType.CoreDropped,
                0,
                (int)CoreDropReason.Manual,
                PlayerSlot.Zero);
        }

        [Test]
        public void PartialScoreAndRateRemainderSurviveDropAndReacquisition()
        {
            MatchSimulation simulation = CreateRegulation(possessionGraceSeconds: 0.016f);
            InputCommand pickup = Interact(1u, 1u);
            Step(simulation, in pickup);
            Step(simulation);

            Assert.That(simulation.State.Tick.Value, Is.EqualTo(2u));
            Assert.That(simulation.State.Score.PlayerZeroMilliPoints, Is.EqualTo(16));
            Assert.That(simulation.State.Score.PlayerZeroRateRemainder, Is.EqualTo(40));

            CoreForcedDropRequest forcedDrop = new CoreForcedDropRequest(
                CoreDropReason.Pulse,
                float2.zero,
                17u);
            Assert.That(simulation.TryQueueForcedCoreDrop(in forcedDrop), Is.True);
            Step(simulation);
            Advance(simulation, 20);

            Assert.That(simulation.State.Tick.Value, Is.EqualTo(23u));
            Assert.That(simulation.State.Score.PlayerZeroMilliPoints, Is.EqualTo(16));
            Assert.That(simulation.State.Score.PlayerZeroRateRemainder, Is.EqualTo(40));

            InputCommand reacquire = Interact(24u, 2u);
            Step(simulation, in reacquire);
            Assert.That(simulation.State.Core.IsCarriedBy(PlayerSlot.Zero), Is.True);
            Assert.That(
                simulation.State.Core.GetPossessionGraceRemainingTicks(simulation.State.Tick),
                Is.EqualTo(1u));
            Assert.That(simulation.State.Score.PlayerZeroMilliPoints, Is.EqualTo(16));
            Assert.That(simulation.State.Score.PlayerZeroRateRemainder, Is.EqualTo(40));

            Step(simulation);
            Assert.That(simulation.State.Score.PlayerZeroMilliPoints, Is.EqualTo(33));
            Assert.That(simulation.State.Score.PlayerZeroRateRemainder, Is.EqualTo(20));
            Advance(simulation, 58);

            Assert.That(simulation.State.Score.PlayerZeroMilliPoints, Is.EqualTo(1000));
            Assert.That(simulation.State.Score.PlayerZeroRateRemainder, Is.Zero);
        }

        [Test]
        public void TargetVictoryOrdersEventsAndResultRemainsImmutableThroughResults()
        {
            MatchSimulation simulation = CreateRegulation(
                regulationSeconds: 10f,
                scoreTargetPoints: 1,
                possessionGraceSeconds: 0.5f,
                finalizingSeconds: 1.5f);
            InputCommand pickup = Interact(1u, 1u);
            Step(simulation, in pickup);
            Advance(simulation, 88);

            Assert.That(simulation.State.Tick.Value, Is.EqualTo(89u));
            Assert.That(simulation.State.Score.PlayerZeroMilliPoints, Is.EqualTo(983));

            MatchEventBuffer finalEvents = Step(simulation);

            Assert.That(simulation.State.Tick.Value, Is.EqualTo(90u));
            Assert.That(simulation.State.Clock.Phase, Is.EqualTo(MatchPhase.Finalizing));
            Assert.That(simulation.State.Clock.HasResult, Is.True);
            Assert.That(simulation.State.Clock.PhaseEndTick.Value, Is.EqualTo(180u));
            Assert.That(finalEvents.Count, Is.EqualTo(3));
            AssertEvent(finalEvents[0], MatchEventType.ScoreChanged, 0, 1000, PlayerSlot.Zero);
            AssertEvent(finalEvents[1], MatchEventType.MatchPhaseChanged, 1, (int)MatchPhase.Finalizing);
            AssertEvent(
                finalEvents[2],
                MatchEventType.MatchFinalized,
                2,
                (int)MatchResultReason.TargetScore,
                PlayerSlot.Zero);

            MatchResult result = simulation.State.Clock.Result;
            AssertFinalResult(
                in result,
                MatchResultReason.TargetScore,
                PlayerSlot.Zero,
                finalizedTick: 90u,
                zeroScore: 1000,
                oneScore: 0,
                regulationTicksRemaining: 510u);
            float2 frozenZeroPosition = simulation.State.GetPlayer(PlayerSlot.Zero).Position;
            float2 frozenOnePosition = simulation.State.GetPlayer(PlayerSlot.One).Position;
            CoreRuntimeState frozenCore = simulation.State.Core;
            ScoreRuntimeState frozenScore = simulation.State.Score;
            uint frozenRegulationTicks = simulation.State.Clock.RegulationTicksRemaining;
            InputCommand blockedMutation = SimulationTestFactory.Command(
                new float2(1f, 0f),
                InputButtons.Interact,
                91u,
                2u);

            MatchEventBuffer frozenEvents = Step(simulation, in blockedMutation);

            Assert.That(frozenEvents.Count, Is.Zero);
            AssertFrozenState(
                simulation,
                frozenZeroPosition,
                frozenOnePosition,
                in frozenCore,
                in frozenScore,
                frozenRegulationTicks,
                in result);

            Advance(simulation, 88);

            Assert.That(simulation.State.Tick.Value, Is.EqualTo(179u));
            Assert.That(simulation.State.Clock.Phase, Is.EqualTo(MatchPhase.Finalizing));
            AssertFrozenState(
                simulation,
                frozenZeroPosition,
                frozenOnePosition,
                in frozenCore,
                in frozenScore,
                frozenRegulationTicks,
                in result);

            MatchEventBuffer resultsEvents = Step(simulation);

            Assert.That(simulation.State.Tick.Value, Is.EqualTo(180u));
            Assert.That(simulation.State.Clock.Phase, Is.EqualTo(MatchPhase.Results));
            Assert.That(resultsEvents.Count, Is.EqualTo(1));
            AssertEvent(resultsEvents[0], MatchEventType.MatchPhaseChanged, 0, (int)MatchPhase.Results);
            AssertFrozenState(
                simulation,
                frozenZeroPosition,
                frozenOnePosition,
                in frozenCore,
                in frozenScore,
                frozenRegulationTicks,
                in result);

            SimulationTick resultsTick = simulation.State.Tick;
            MatchEventBuffer afterResultsEvents = Step(simulation, in blockedMutation);

            Assert.That(simulation.State.Tick, Is.EqualTo(resultsTick));
            Assert.That(afterResultsEvents.Count, Is.Zero);
            AssertFrozenState(
                simulation,
                frozenZeroPosition,
                frozenOnePosition,
                in frozenCore,
                in frozenScore,
                frozenRegulationTicks,
                in result);
        }

        [Test]
        public void FinalizingIgnoresGameplayInputsWithoutMutatingAuthoritativeState()
        {
            MatchSimulation quiet = CreateRegulation(
                regulationSeconds: 10f,
                scoreTargetPoints: 1,
                possessionGraceSeconds: 0.5f);
            MatchSimulation noisy = CreateRegulation(
                regulationSeconds: 10f,
                scoreTargetPoints: 1,
                possessionGraceSeconds: 0.5f);
            InputCommand pickup = Interact(1u, 1u);
            Step(quiet, in pickup);
            Step(noisy, in pickup);
            Advance(quiet, 89);
            Advance(noisy, 89);

            Assert.That(quiet.State.Clock.Phase, Is.EqualTo(MatchPhase.Finalizing));
            Assert.That(noisy.ComputeChecksum(), Is.EqualTo(quiet.ComputeChecksum()));
            InputCommand noisyInput = SimulationTestFactory.Command(
                new float2(1f, 0f),
                InputButtons.Interact,
                91u,
                2u,
                new float2(1f, 0f));

            Step(quiet);
            Step(noisy, in noisyInput);

            Assert.That(noisy.State.Tick, Is.EqualTo(quiet.State.Tick));
            Assert.That(noisy.State.Clock.Phase, Is.EqualTo(MatchPhase.Finalizing));
            Assert.That(noisy.ComputeChecksum(), Is.EqualTo(quiet.ComputeChecksum()));
        }

        [Test]
        public void RegulationTimerAwardsHigherFixedPointScore()
        {
            MatchSimulation simulation = CreateRegulation(
                regulationSeconds: 1f,
                scoreTargetPoints: 100,
                possessionGraceSeconds: 0.016f,
                finalizingSeconds: 1.5f);
            Assert.That(simulation.Config.Regulation.RegulationDurationTicks, Is.EqualTo(60u));
            Assert.That(simulation.Config.Regulation.PossessionGraceTicks, Is.EqualTo(1u));
            InputCommand pickup = Interact(1u, 1u);
            Step(simulation, in pickup);
            Advance(simulation, 58);

            MatchEventBuffer timerEvents = Step(simulation);

            Assert.That(simulation.State.Tick.Value, Is.EqualTo(60u));
            Assert.That(simulation.State.Score.PlayerZeroMilliPoints, Is.EqualTo(983));
            Assert.That(simulation.State.Score.PlayerZeroRateRemainder, Is.EqualTo(20));
            Assert.That(simulation.State.Score.PlayerOneMilliPoints, Is.Zero);
            Assert.That(simulation.State.Clock.Phase, Is.EqualTo(MatchPhase.Finalizing));
            Assert.That(timerEvents.Count, Is.EqualTo(2));
            AssertEvent(timerEvents[0], MatchEventType.MatchPhaseChanged, 0, (int)MatchPhase.Finalizing);
            AssertEvent(
                timerEvents[1],
                MatchEventType.MatchFinalized,
                1,
                (int)MatchResultReason.RegulationTimer,
                PlayerSlot.Zero);
            MatchResult result = simulation.State.Clock.Result;
            AssertFinalResult(
                in result,
                MatchResultReason.RegulationTimer,
                PlayerSlot.Zero,
                finalizedTick: 60u,
                zeroScore: 983,
                oneScore: 0,
                regulationTicksRemaining: 0u);
        }

        [Test]
        public void TargetVictoryTakesPrecedenceWhenTimerAlsoExpires()
        {
            MatchSimulation simulation = CreateRegulation(
                regulationSeconds: 1.016f,
                scoreTargetPoints: 1,
                possessionGraceSeconds: 0.016f,
                finalizingSeconds: 1.5f);
            Assert.That(simulation.Config.Regulation.RegulationDurationTicks, Is.EqualTo(61u));
            Assert.That(simulation.Config.Regulation.PossessionGraceTicks, Is.EqualTo(1u));
            InputCommand pickup = Interact(1u, 1u);
            Step(simulation, in pickup);
            Advance(simulation, 59);

            Assert.That(simulation.State.Tick.Value, Is.EqualTo(60u));
            Assert.That(simulation.State.Clock.RegulationTicksRemaining, Is.EqualTo(1u));
            Assert.That(simulation.State.Score.PlayerZeroMilliPoints, Is.EqualTo(983));

            MatchEventBuffer finalEvents = Step(simulation);

            Assert.That(simulation.State.Tick.Value, Is.EqualTo(61u));
            Assert.That(simulation.State.Clock.RegulationTicksRemaining, Is.Zero);
            Assert.That(finalEvents.Count, Is.EqualTo(3));
            AssertEvent(finalEvents[0], MatchEventType.ScoreChanged, 0, 1000, PlayerSlot.Zero);
            AssertEvent(finalEvents[1], MatchEventType.MatchPhaseChanged, 1, (int)MatchPhase.Finalizing);
            AssertEvent(
                finalEvents[2],
                MatchEventType.MatchFinalized,
                2,
                (int)MatchResultReason.TargetScore,
                PlayerSlot.Zero);
            MatchResult result = simulation.State.Clock.Result;
            AssertFinalResult(
                in result,
                MatchResultReason.TargetScore,
                PlayerSlot.Zero,
                finalizedTick: 61u,
                zeroScore: 1000,
                oneScore: 0,
                regulationTicksRemaining: 0u);
        }

        [Test]
        public void RegulationTieFreezesInOvertimeResetWithoutResult()
        {
            MatchSimulation simulation = CreateRegulation(
                regulationSeconds: 1f,
                scoreTargetPoints: 100,
                possessionGraceSeconds: 0.5f,
                finalizingSeconds: 1.5f,
                playerZeroPosition: new float2(-2f, 0f),
                playerOnePosition: new float2(2f, 0f));
            Advance(simulation, 59);

            Assert.That(simulation.State.Clock.Phase, Is.EqualTo(MatchPhase.Regulation));
            Assert.That(simulation.State.Clock.RegulationTicksRemaining, Is.EqualTo(1u));
            Assert.That(simulation.State.Clock.HasResult, Is.False);

            MatchEventBuffer tieEvents = Step(simulation);

            Assert.That(simulation.State.Tick.Value, Is.EqualTo(60u));
            Assert.That(simulation.State.Clock.Phase, Is.EqualTo(MatchPhase.OvertimeReset));
            Assert.That(simulation.State.Clock.RegulationTicksRemaining, Is.Zero);
            Assert.That(simulation.State.Clock.HasResult, Is.False);
            Assert.That(simulation.State.Clock.Result.IsFinal, Is.False);
            Assert.That(tieEvents.Count, Is.EqualTo(2));
            AssertEvent(tieEvents[0], MatchEventType.MatchPhaseChanged, 0, (int)MatchPhase.OvertimeReset);
            AssertEvent(tieEvents[1], MatchEventType.RegulationTieDetected, 1, 0);

            SimulationTick frozenTick = simulation.State.Tick;
            float2 frozenZeroPosition = simulation.State.GetPlayer(PlayerSlot.Zero).Position;
            float2 frozenOnePosition = simulation.State.GetPlayer(PlayerSlot.One).Position;
            CoreRuntimeState frozenCore = simulation.State.Core;
            InputCommand blockedMutation = SimulationTestFactory.Command(
                new float2(1f, 0f),
                InputButtons.Interact,
                61u,
                1u);

            MatchEventBuffer frozenEvents = Step(simulation, in blockedMutation);

            Assert.That(simulation.State.Tick, Is.EqualTo(frozenTick));
            Assert.That(simulation.State.Clock.Phase, Is.EqualTo(MatchPhase.OvertimeReset));
            Assert.That(simulation.State.Clock.HasResult, Is.False);
            Assert.That(simulation.State.GetPlayer(PlayerSlot.Zero).Position, Is.EqualTo(frozenZeroPosition));
            Assert.That(simulation.State.GetPlayer(PlayerSlot.One).Position, Is.EqualTo(frozenOnePosition));
            AssertCoreEqual(simulation.State.Core, frozenCore);
            Assert.That(frozenEvents.Count, Is.Zero);
        }

        private static MatchSimulation CreateRegulation(
            float regulationSeconds = 180f,
            int scoreTargetPoints = 100,
            float possessionGraceSeconds = 0.5f,
            float finalizingSeconds = 1.5f,
            float2 playerZeroPosition = default,
            float2 playerOnePosition = default)
        {
            if (math.all(playerOnePosition == float2.zero))
            {
                playerOnePosition = new float2(8f, 0f);
            }

            PlayerConfigValues playerValues = PlayerConfigValues.GddDefaults;
            CoreConfigValues coreValues = CoreConfigValues.GddDefaults;
            RegulationConfigValues regulationValues = new RegulationConfigValues(
                3f,
                regulationSeconds,
                scoreTargetPoints,
                1000,
                2000,
                possessionGraceSeconds,
                finalizingSeconds);
            return SimulationTestFactory.Create(
                SimulationTestFactory.CreateOpenArena(),
                playerZeroPosition,
                playerOnePosition,
                in playerValues,
                in coreValues,
                in regulationValues,
                MatchStartMode.Regulation);
        }

        private static InputCommand Interact(uint clientTick, uint sequence)
        {
            return SimulationTestFactory.Command(
                float2.zero,
                InputButtons.Interact,
                clientTick,
                sequence);
        }

        private static MatchEventBuffer Step(MatchSimulation simulation)
        {
            InputCommand empty = default;
            return Step(simulation, in empty, in empty);
        }

        private static MatchEventBuffer Step(MatchSimulation simulation, in InputCommand zero)
        {
            InputCommand one = default;
            return Step(simulation, in zero, in one);
        }

        private static MatchEventBuffer Step(
            MatchSimulation simulation,
            in InputCommand zero,
            in InputCommand one)
        {
            TickInputs inputs = new TickInputs();
            inputs.Set(PlayerSlot.Zero, in zero);
            inputs.Set(PlayerSlot.One, in one);
            MatchEventBuffer events = new MatchEventBuffer();
            simulation.Step(inputs, events);
            return events;
        }

        private static void Advance(MatchSimulation simulation, int tickCount)
        {
            TickInputs inputs = new TickInputs();
            MatchEventBuffer events = new MatchEventBuffer();
            for (int index = 0; index < tickCount; index++)
            {
                simulation.Step(inputs, events);
            }
        }

        private static void AssertEvent(
            in MatchEvent matchEvent,
            MatchEventType expectedType,
            byte expectedIndex,
            int expectedValue,
            PlayerSlot? expectedPlayer = null)
        {
            Assert.That(matchEvent.Type, Is.EqualTo(expectedType));
            Assert.That(matchEvent.Id.Index, Is.EqualTo(expectedIndex));
            Assert.That(matchEvent.Value, Is.EqualTo(expectedValue));
            Assert.That(matchEvent.HasPlayerSlot, Is.EqualTo(expectedPlayer.HasValue));
            if (expectedPlayer.HasValue)
            {
                Assert.That(matchEvent.PlayerSlot, Is.EqualTo(expectedPlayer.Value));
            }
        }

        private static void AssertFinalResult(
            in MatchResult result,
            MatchResultReason reason,
            PlayerSlot winner,
            uint finalizedTick,
            int zeroScore,
            int oneScore,
            uint regulationTicksRemaining)
        {
            Assert.That(result.IsFinal, Is.True);
            Assert.That(result.Outcome, Is.EqualTo(MatchResultOutcome.PlayerVictory));
            Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(result.HasWinner, Is.True);
            Assert.That(result.Winner, Is.EqualTo(winner));
            Assert.That(result.FinalizedTick.Value, Is.EqualTo(finalizedTick));
            Assert.That(result.PlayerZeroScoreMilliPoints, Is.EqualTo(zeroScore));
            Assert.That(result.PlayerOneScoreMilliPoints, Is.EqualTo(oneScore));
            Assert.That(result.RegulationTicksRemaining, Is.EqualTo(regulationTicksRemaining));
        }

        private static void AssertFrozenState(
            MatchSimulation simulation,
            float2 expectedZeroPosition,
            float2 expectedOnePosition,
            in CoreRuntimeState expectedCore,
            in ScoreRuntimeState expectedScore,
            uint expectedRegulationTicks,
            in MatchResult expectedResult)
        {
            Assert.That(simulation.State.GetPlayer(PlayerSlot.Zero).Position, Is.EqualTo(expectedZeroPosition));
            Assert.That(simulation.State.GetPlayer(PlayerSlot.One).Position, Is.EqualTo(expectedOnePosition));
            AssertCoreEqual(simulation.State.Core, expectedCore);
            Assert.That(
                simulation.State.Score.PlayerZeroMilliPoints,
                Is.EqualTo(expectedScore.PlayerZeroMilliPoints));
            Assert.That(
                simulation.State.Score.PlayerOneMilliPoints,
                Is.EqualTo(expectedScore.PlayerOneMilliPoints));
            Assert.That(
                simulation.State.Score.PlayerZeroRateRemainder,
                Is.EqualTo(expectedScore.PlayerZeroRateRemainder));
            Assert.That(
                simulation.State.Score.PlayerOneRateRemainder,
                Is.EqualTo(expectedScore.PlayerOneRateRemainder));
            Assert.That(simulation.State.Clock.RegulationTicksRemaining, Is.EqualTo(expectedRegulationTicks));
            MatchResult actualResult = simulation.State.Clock.Result;
            Assert.That(actualResult.Outcome, Is.EqualTo(expectedResult.Outcome));
            Assert.That(actualResult.Reason, Is.EqualTo(expectedResult.Reason));
            Assert.That(actualResult.HasWinner, Is.EqualTo(expectedResult.HasWinner));
            Assert.That(actualResult.Winner, Is.EqualTo(expectedResult.Winner));
            Assert.That(actualResult.FinalizedTick, Is.EqualTo(expectedResult.FinalizedTick));
            Assert.That(
                actualResult.PlayerZeroScoreMilliPoints,
                Is.EqualTo(expectedResult.PlayerZeroScoreMilliPoints));
            Assert.That(
                actualResult.PlayerOneScoreMilliPoints,
                Is.EqualTo(expectedResult.PlayerOneScoreMilliPoints));
            Assert.That(
                actualResult.RegulationTicksRemaining,
                Is.EqualTo(expectedResult.RegulationTicksRemaining));
        }

        private static void AssertCoreEqual(
            in CoreRuntimeState actual,
            in CoreRuntimeState expected)
        {
            Assert.That(actual.Mode, Is.EqualTo(expected.Mode));
            Assert.That(actual.HasOwner, Is.EqualTo(expected.HasOwner));
            Assert.That(actual.Owner, Is.EqualTo(expected.Owner));
            Assert.That(actual.Position, Is.EqualTo(expected.Position));
            Assert.That(actual.LastValidPosition, Is.EqualTo(expected.LastValidPosition));
            Assert.That(actual.ResetOriginPosition, Is.EqualTo(expected.ResetOriginPosition));
            Assert.That(actual.Velocity, Is.EqualTo(expected.Velocity));
            Assert.That(actual.ModeEndTick, Is.EqualTo(expected.ModeEndTick));
            Assert.That(actual.PlayerZeroPickupLockEndTick, Is.EqualTo(expected.PlayerZeroPickupLockEndTick));
            Assert.That(actual.PlayerOnePickupLockEndTick, Is.EqualTo(expected.PlayerOnePickupLockEndTick));
            Assert.That(actual.PossessionGraceEndTick, Is.EqualTo(expected.PossessionGraceEndTick));
            Assert.That(actual.InvalidTickCount, Is.EqualTo(expected.InvalidTickCount));
            Assert.That(actual.RestTickCount, Is.EqualTo(expected.RestTickCount));
            Assert.That(actual.IsResting, Is.EqualTo(expected.IsResting));
            Assert.That(actual.LastDropReason, Is.EqualTo(expected.LastDropReason));
            Assert.That(actual.LastDropActionId, Is.EqualTo(expected.LastDropActionId));
        }
    }
}
