using System;
using NUnit.Framework;
using RelayZero.Arena;
using RelayZero.Foundation;
using RelayZero.Simulation;
using Unity.Mathematics;

namespace RelayZero.Tests.EditMode.Simulation
{
    public sealed class CoreLifecycleTests
    {
        [Test]
        public void CoreStartsLockedAndReleasesOnFirstRegulationTick()
        {
            MatchSimulation simulation = SimulationTestFactory.Create(
                new float2(2f, 0f),
                new float2(-2f, 0f));

            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Locked));
            Assert.That(simulation.State.Core.HasOwner, Is.False);

            MatchEventBuffer events = Step(simulation);

            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Loose));
            Assert.That(events.Count, Is.Zero);
        }

        [TestCase(1.1f, true)]
        [TestCase(1.1001f, false)]
        public void PickupUsesInclusiveAuthoredRange(float playerDistance, bool shouldPickUp)
        {
            MatchSimulation simulation = SimulationTestFactory.Create(
                new float2(playerDistance, 0f),
                new float2(5f, 0f));
            InputCommand interact = Interact(1u, 1u);

            MatchEventBuffer events = Step(simulation, in interact, default);

            Assert.That(simulation.State.Core.Mode == CoreMode.Carried, Is.EqualTo(shouldPickUp));
            Assert.That(events.Count, Is.EqualTo(shouldPickUp ? 1 : 0));
            if (shouldPickUp)
            {
                Assert.That(simulation.State.Core.Owner, Is.EqualTo(PlayerSlot.Zero));
                Assert.That(events[0].Type, Is.EqualTo(MatchEventType.CorePickedUp));
            }
            else
            {
                AssertDiagnosticKind(simulation, CoreDiagnosticKind.PickupRejectedRange);
            }
        }

        [Test]
        public void StaticWallOccludesPickupWithoutChangingCoreAvailability()
        {
            ArenaBakeData arena = CreateWallArena();
            MatchSimulation simulation = SimulationTestFactory.Create(
                arena,
                new float2(1.1f, 0f),
                new float2(5f, 0f));
            InputCommand interact = Interact(1u, 1u);

            MatchEventBuffer events = Step(simulation, in interact, default);

            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Loose));
            Assert.That(events.Count, Is.Zero);
            AssertDiagnosticKind(simulation, CoreDiagnosticKind.PickupRejectedOccluded);
        }

        [Test]
        public void ClearlyCloserContestedPickupWins()
        {
            MatchSimulation simulation = SimulationTestFactory.Create(
                new float2(-0.9f, 0f),
                new float2(0.7f, 0f));
            InputCommand zero = Interact(1u, 1u);
            InputCommand one = Interact(0u, 1u);

            MatchEventBuffer events = Step(simulation, in zero, in one);

            AssertPickupWinner(simulation, events, PlayerSlot.One);
        }

        [Test]
        public void SubThresholdDistanceContestUsesEarlierMappedCommandTick()
        {
            MatchSimulation simulation = SimulationTestFactory.Create(
                new float2(-0.8f, 0f),
                new float2(0.82f, 0f));
            InputCommand earlierZero = Interact(0u, 1u);
            InputCommand laterOne = Interact(1u, 1u);

            MatchEventBuffer events = Step(simulation, in earlierZero, in laterOne);

            AssertPickupWinner(simulation, events, PlayerSlot.Zero);
        }

        [Test]
        public void ExactTieThresholdUsesDistanceBeforeMappedTime()
        {
            MatchSimulation simulation = SimulationTestFactory.Create(
                new float2(-0.8f, 0f),
                new float2(0.85f, 0f));
            InputCommand laterCloserZero = Interact(1u, 1u);
            InputCommand earlierFartherOne = Interact(0u, 1u);

            MatchEventBuffer events = Step(simulation, in laterCloserZero, in earlierFartherOne);

            AssertPickupWinner(simulation, events, PlayerSlot.Zero);
        }

        [Test]
        public void ExactDistanceAndTimeContestUsesStableSlot()
        {
            MatchSimulation simulation = SimulationTestFactory.Create(
                new float2(-0.8f, 0f),
                new float2(0.8f, 0f));
            InputCommand zero = Interact(1u, 1u);
            InputCommand one = Interact(1u, 1u);

            MatchEventBuffer events = Step(simulation, in zero, in one);

            AssertPickupWinner(simulation, events, PlayerSlot.Zero);
        }

        [Test]
        public void ReplayedInteractEdgeCannotBecomeADeferredPickup()
        {
            MatchSimulation simulation = SimulationTestFactory.Create(
                new float2(1.2f, 0f),
                new float2(8f, 0f));
            InputCommand stale = SimulationTestFactory.Command(
                new float2(-1f, 0f),
                InputButtons.Interact,
                1u,
                1u);

            for (int index = 0; index < 4; index++)
            {
                Step(simulation, in stale, default);
            }

            Assert.That(simulation.State.GetPlayer(PlayerSlot.Zero).Position.x, Is.LessThan(1.1f));
            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Loose));

            InputCommand fresh = SimulationTestFactory.Command(
                float2.zero,
                InputButtons.Interact,
                5u,
                2u);
            Step(simulation, in fresh, default);

            Assert.That(simulation.State.Core.IsCarriedBy(PlayerSlot.Zero), Is.True);
        }

        [Test]
        public void CarriedCoreFollowsOwnerAndMovementNeverExceedsCarrierMaximum()
        {
            MatchSimulation simulation = SimulationTestFactory.Create(
                float2.zero,
                new float2(10f, 0f));
            InputCommand pickup = Interact(1u, 1u);
            Step(simulation, in pickup, default);
            MatchEventBuffer events = new MatchEventBuffer();
            TickInputs inputs = new TickInputs();

            for (uint tick = 2u; tick <= 90u; tick++)
            {
                InputCommand move = SimulationTestFactory.Movement(new float2(1f, 0f), tick, tick);
                inputs.Set(PlayerSlot.Zero, in move);
                simulation.Step(inputs, events);
                PlayerState carrier = simulation.State.GetPlayer(PlayerSlot.Zero);
                Assert.That(math.length(carrier.Velocity), Is.LessThanOrEqualTo(5.3f + 0.00001f));
                Assert.That(simulation.State.Core.Position, Is.EqualTo(carrier.Position));
                Assert.That(simulation.State.Core.Velocity, Is.EqualTo(float2.zero));
            }
        }

        [Test]
        public void PickupImmediatelyClampsExistingVelocityToCarrierMaximum()
        {
            MatchSimulation simulation = SimulationTestFactory.Create(
                new float2(-3f, 0f),
                new float2(8f, 0f));
            uint tick = 1u;
            while (simulation.State.GetPlayer(PlayerSlot.Zero).Position.x < -1.2f && tick < 60u)
            {
                InputCommand approach = SimulationTestFactory.Movement(new float2(1f, 0f), tick, tick);
                Step(simulation, in approach, default);
                tick++;
            }

            Assert.That(tick, Is.LessThan(60u));
            Assert.That(
                math.length(simulation.State.GetPlayer(PlayerSlot.Zero).Velocity),
                Is.GreaterThan(5.3f));
            InputCommand pickup = SimulationTestFactory.Command(
                new float2(1f, 0f),
                InputButtons.Interact,
                tick,
                tick);

            Step(simulation, in pickup, default);

            Assert.That(simulation.State.Core.IsCarriedBy(PlayerSlot.Zero), Is.True);
            Assert.That(
                math.length(simulation.State.GetPlayer(PlayerSlot.Zero).Velocity),
                Is.LessThanOrEqualTo(5.3f + 0.00001f));
        }

        [Test]
        public void ManualDropUsesOneOperationAndExactAsymmetricLockDeadlines()
        {
            MatchSimulation simulation = SimulationTestFactory.Create(float2.zero, new float2(8f, 0f));
            InputCommand pickup = Interact(1u, 1u);
            Step(simulation, in pickup, default);
            InputCommand drop = Interact(2u, 2u);

            MatchEventBuffer events = Step(simulation, in drop, default);
            CoreRuntimeState core = simulation.State.Core;

            Assert.That(core.Mode, Is.EqualTo(CoreMode.DropLock));
            Assert.That(core.HasOwner, Is.False);
            Assert.That(simulation.State.GetPlayer(PlayerSlot.Zero).IsCarrying, Is.False);
            Assert.That(core.LastDropReason, Is.EqualTo(CoreDropReason.Manual));
            Assert.That(core.LastDropActionId, Is.EqualTo(2u));
            Assert.That(core.GetPickupLockRemainingTicks(PlayerSlot.One, simulation.State.Tick), Is.EqualTo(12u));
            Assert.That(core.GetPickupLockRemainingTicks(PlayerSlot.Zero, simulation.State.Tick), Is.EqualTo(45u));
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Type, Is.EqualTo(MatchEventType.CoreDropped));
            Assert.That(events[0].Value, Is.EqualTo((int)CoreDropReason.Manual));
            Assert.That(simulation.CoreDiagnostics[0].Velocity.x, Is.Zero.Within(0.00001f));
            Assert.That(simulation.CoreDiagnostics[0].Velocity.y, Is.EqualTo(4.5f).Within(0.00001f));

            Advance(simulation, 11);
            Assert.That(simulation.State.Core.IsPickupEligible(PlayerSlot.One, simulation.State.Tick), Is.False);
            Assert.That(
                simulation.State.Core.GetPickupLockRemainingTicks(PlayerSlot.One, simulation.State.Tick),
                Is.EqualTo(1u));

            Advance(simulation, 1);
            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Loose));
            Assert.That(simulation.State.Core.IsPickupEligible(PlayerSlot.One, simulation.State.Tick), Is.True);
            Assert.That(simulation.State.Core.IsPickupEligible(PlayerSlot.Zero, simulation.State.Tick), Is.False);

            Advance(simulation, 33);
            Assert.That(simulation.State.Core.IsPickupEligible(PlayerSlot.Zero, simulation.State.Tick), Is.True);
        }

        [TestCase(CoreDropReason.Pulse)]
        [TestCase(CoreDropReason.ShockGate)]
        [TestCase(CoreDropReason.Disconnect)]
        [TestCase(CoreDropReason.Recovery)]
        public void ForcedDropReasonsShareGeneralLockAndOwnershipCleanup(CoreDropReason reason)
        {
            MatchSimulation simulation = SimulationTestFactory.Create(float2.zero, new float2(8f, 0f));
            InputCommand pickup = Interact(1u, 1u);
            Step(simulation, in pickup, default);
            CoreForcedDropRequest request = new CoreForcedDropRequest(reason, float2.zero, 77u);

            Assert.That(simulation.TryQueueForcedCoreDrop(in request), Is.True);
            MatchEventBuffer events = Step(simulation);

            CoreRuntimeState core = simulation.State.Core;
            Assert.That(core.Mode, Is.EqualTo(CoreMode.DropLock));
            Assert.That(core.HasOwner, Is.False);
            Assert.That(core.LastDropReason, Is.EqualTo(reason));
            Assert.That(core.LastDropActionId, Is.EqualTo(77u));
            Assert.That(core.GetPickupLockRemainingTicks(PlayerSlot.Zero, simulation.State.Tick), Is.EqualTo(21u));
            Assert.That(core.GetPickupLockRemainingTicks(PlayerSlot.One, simulation.State.Tick), Is.EqualTo(21u));
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Type, Is.EqualTo(MatchEventType.CoreDropped));
            Assert.That(events[0].Value, Is.EqualTo((int)reason));
            Assert.That(simulation.TryQueueForcedCoreDrop(in request), Is.False);

            Advance(simulation, 20);
            Assert.That(simulation.State.Core.IsPickupEligible(PlayerSlot.Zero, simulation.State.Tick), Is.False);
            Advance(simulation, 1);
            Assert.That(simulation.State.Core.IsPickupEligible(PlayerSlot.Zero, simulation.State.Tick), Is.True);
            Assert.That(simulation.State.Core.IsPickupEligible(PlayerSlot.One, simulation.State.Tick), Is.True);
        }

        [Test]
        public void ForcedDropRequestRejectsUndefinedReasonValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CoreForcedDropRequest((CoreDropReason)255, float2.zero, 1u));
        }

        [Test]
        public void SeededContestedPickupsAlwaysResolveByDistanceTimeThenSlot()
        {
            System.Random random = new System.Random(0x5A17);
            for (int iteration = 0; iteration < 256; iteration++)
            {
                float zeroDistance = 0.46f + (float)random.NextDouble() * 0.64f;
                float oneDistance = 0.46f + (float)random.NextDouble() * 0.64f;
                uint zeroTick = (uint)random.Next(0, 2);
                uint oneTick = (uint)random.Next(0, 2);
                MatchSimulation simulation = SimulationTestFactory.Create(
                    new float2(-zeroDistance, 0f),
                    new float2(oneDistance, 0f));
                InputCommand zero = Interact(zeroTick, 1u);
                InputCommand one = Interact(oneTick, 1u);

                Step(simulation, in zero, in one);

                PlayerSlot expected;
                float difference = math.abs(zeroDistance - oneDistance);
                if (difference >= 0.05f)
                {
                    expected = zeroDistance < oneDistance ? PlayerSlot.Zero : PlayerSlot.One;
                }
                else if (zeroTick != oneTick)
                {
                    expected = zeroTick < oneTick ? PlayerSlot.Zero : PlayerSlot.One;
                }
                else
                {
                    expected = PlayerSlot.Zero;
                }

                Assert.That(simulation.State.Core.Owner, Is.EqualTo(expected), "Iteration " + iteration);
                Assert.That(
                    simulation.State.GetPlayer(PlayerSlot.Zero).IsCarrying ^
                    simulation.State.GetPlayer(PlayerSlot.One).IsCarrying,
                    Is.True,
                    "Iteration " + iteration);
            }
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

        private static void AssertPickupWinner(
            MatchSimulation simulation,
            MatchEventBuffer events,
            PlayerSlot expected)
        {
            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Carried));
            Assert.That(simulation.State.Core.Owner, Is.EqualTo(expected));
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Type, Is.EqualTo(MatchEventType.CorePickedUp));
            Assert.That(events[0].PlayerSlot, Is.EqualTo(expected));
        }

        private static void AssertDiagnosticKind(MatchSimulation simulation, CoreDiagnosticKind expected)
        {
            for (int index = 0; index < simulation.CoreDiagnostics.Count; index++)
            {
                if (simulation.CoreDiagnostics[index].Kind == expected)
                {
                    return;
                }
            }

            Assert.Fail("Expected core diagnostic " + expected + ".");
        }

        private static ArenaBakeData CreateWallArena()
        {
            BakedWall wall = new BakedWall(
                new ArenaElementId(10),
                new float2(0.55f, -4f),
                new float2(0.55f, 4f),
                new float2(1f, 0f),
                0.1f,
                new ArenaAabb(new float2(0.5f, -4f), new float2(0.6f, 4f)),
                new ArenaAabb(new float2(0.05f, -4.45f), new float2(1.05f, 4.45f)));
            return CreateArena(new[] { wall });
        }

        private static ArenaBakeData CreateArena(BakedWall[] walls)
        {
            const float halfExtent = 10f;
            return new ArenaBakeData(
                1,
                "core-lifecycle-test",
                "tests",
                Array.Empty<ArenaElementDescriptor>(),
                new BakedArenaBounds(new ArenaElementId(1), float2.zero, new float2(halfExtent)),
                walls,
                Array.Empty<BakedConvexObstacle>(),
                Array.Empty<BakedCircle>(),
                Array.Empty<BakedShockGate>(),
                Array.Empty<BakedBoostPad>(),
                new[]
                {
                    new BakedSpawn(new ArenaElementId(4), 0, new float2(1.1f, 0f), new float2(-1f, 0f)),
                    new BakedSpawn(new ArenaElementId(5), 1, new float2(5f, 0f), new float2(-1f, 0f)),
                },
                new BakedCoreReset(new ArenaElementId(2), float2.zero, 0.5f),
                Array.Empty<BakedBarrierForbiddenVolume>(),
                Array.Empty<BakedNavigationNode>(),
                Array.Empty<BakedNavigationEdge>(),
                new BakedCameraBounds(new ArenaElementId(3), float2.zero, new float2(halfExtent), halfExtent));
        }
    }
}
