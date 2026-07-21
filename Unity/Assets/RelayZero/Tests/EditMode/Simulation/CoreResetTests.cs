using System;
using NUnit.Framework;
using RelayZero.Arena;
using RelayZero.Foundation;
using RelayZero.Simulation;
using Unity.Mathematics;

namespace RelayZero.Tests.EditMode.Simulation
{
    public sealed class CoreResetTests
    {
        [Test]
        public void ContinuousInvalidBoundsUsesExactResetTravelAndCenterLockDurations()
        {
            ArenaBakeData arena = CreateArena(
                halfExtent: 3f,
                walls: Array.Empty<BakedWall>(),
                gates: Array.Empty<BakedShockGate>());
            MatchSimulation simulation = CreateCarriedSimulation(arena);
            CoreForcedDropRequest request = new CoreForcedDropRequest(
                CoreDropReason.Pulse,
                new float2(-12f, 0f),
                1u);
            simulation.TryQueueForcedCoreDrop(in request);

            MatchEventBuffer events = Step(simulation);
            int searchTicks = 0;
            while (simulation.State.Core.InvalidTickCount == 0u && searchTicks++ < 90)
            {
                events = Step(simulation);
            }

            Assert.That(simulation.State.Core.InvalidTickCount, Is.EqualTo(1u));
            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Loose));
            Advance(simulation, 178);
            Assert.That(simulation.State.Core.InvalidTickCount, Is.EqualTo(179u));
            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Loose));

            events = Step(simulation);

            SimulationTick resetStartTick = simulation.State.Tick;
            float2 resetOrigin = simulation.State.Core.ResetOriginPosition;
            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Resetting));
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Type, Is.EqualTo(MatchEventType.CoreResetStarted));
            Assert.That(events[0].HasPlayerSlot, Is.False);
            Assert.That(math.all(resetOrigin == arena.CoreReset.Position), Is.False);

            Advance(simulation, 89);
            Assert.That(simulation.State.Tick, Is.EqualTo(resetStartTick + 89u));
            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Resetting));
            Assert.That(simulation.State.Core.Position, Is.EqualTo(resetOrigin));

            events = Step(simulation);
            Assert.That(simulation.State.Tick, Is.EqualTo(resetStartTick + 90u));
            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Locked));
            Assert.That(simulation.State.Core.Position, Is.EqualTo(arena.CoreReset.Position));
            Assert.That(simulation.State.Core.Velocity, Is.EqualTo(float2.zero));
            Assert.That(events.Count, Is.Zero);

            InputCommand lockedPickup = Interact(simulation.State.Tick.Next().Value, 20u);
            events = Step(simulation, in lockedPickup, default);
            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Locked));
            Assert.That(events.Count, Is.Zero);
            Advance(simulation, 28);
            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Locked));

            events = Step(simulation);
            Assert.That(simulation.State.Tick, Is.EqualTo(resetStartTick + 120u));
            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Loose));
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Type, Is.EqualTo(MatchEventType.CoreResetCompleted));
            Assert.That(events[0].HasPlayerSlot, Is.False);
        }

        [Test]
        public void GeometricallyValidCoreInPlayerInaccessibleChannelBecomesUnreachable()
        {
            BakedWall[] channelWalls =
            {
                CreateVerticalWall(new ArenaElementId(10), -0.375f, -10f, 10f),
                CreateVerticalWall(new ArenaElementId(11), 0.375f, -10f, 10f),
            };
            ArenaBakeData arena = CreateArena(
                halfExtent: 12f,
                walls: channelWalls,
                gates: Array.Empty<BakedShockGate>(),
                playerZeroSpawn: new float2(2f, 0f),
                playerOneSpawn: new float2(3f, 0f));
            MatchSimulation simulation = SimulationTestFactory.Create(
                arena,
                new float2(2f, 0f),
                new float2(3f, 0f));

            Step(simulation);

            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Loose));
            Assert.That(simulation.State.Core.InvalidTickCount, Is.EqualTo(1u));
            AssertDiagnosticKind(simulation, CoreDiagnosticKind.InvalidOrUnreachable);
            Advance(simulation, 178);
            Assert.That(simulation.State.Core.InvalidTickCount, Is.EqualTo(179u));

            MatchEventBuffer events = Step(simulation);

            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Resetting));
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Type, Is.EqualTo(MatchEventType.CoreResetStarted));
        }

        [Test]
        public void RoomySealedPocketRequiresAPathToItsValidPickupStandPoints()
        {
            BakedWall[] pocketWalls =
            {
                CreateVerticalWall(new ArenaElementId(10), -2f, -2f, 2f),
                CreateVerticalWall(new ArenaElementId(11), 2f, -2f, 2f),
                CreateHorizontalWall(new ArenaElementId(12), -2f, 2f, -2f),
                CreateHorizontalWall(new ArenaElementId(13), -2f, 2f, 2f),
                // Keep the pocket from being classified as the arena's single closed perimeter.
                CreateHorizontalWall(new ArenaElementId(14), -1f, 1f, 7f),
            };
            BakedNavigationNode[] nodes =
            {
                new BakedNavigationNode(
                    new ArenaElementId(20),
                    BakedNavigationKind.Spawn,
                    new float2(-4f, 0f),
                    8f,
                    Array.Empty<ArenaElementId>()),
                new BakedNavigationNode(
                    new ArenaElementId(21),
                    BakedNavigationKind.Spawn,
                    new float2(4f, 0f),
                    8f,
                    Array.Empty<ArenaElementId>()),
            };
            ArenaBakeData arena = CreateArena(
                halfExtent: 8f,
                walls: pocketWalls,
                gates: Array.Empty<BakedShockGate>(),
                playerZeroSpawn: new float2(-4f, 0f),
                playerOneSpawn: new float2(4f, 0f),
                navigationNodes: nodes);
            MatchSimulation simulation = SimulationTestFactory.Create(
                arena,
                new float2(-4f, 0f),
                new float2(4f, 0f));

            Step(simulation);

            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Loose));
            Assert.That(simulation.State.Core.InvalidTickCount, Is.EqualTo(1u));
            Advance(simulation, 179);
            Assert.That(simulation.State.Core.Mode, Is.EqualTo(CoreMode.Resetting));
        }

        [Test]
        public void InvalidTimerClearsWhenMovingCoreLeavesGateVolumeBeforeDeadline()
        {
            BakedShockGate gate = new BakedShockGate(
                new ArenaElementId(30),
                BakedPowerSide.Alpha,
                new float2(1f, 0f),
                new float2(0.4f, 2f),
                new float2(1f, 0f),
                new ArenaAabb(new float2(0.6f, -2f), new float2(1.4f, 2f)));
            ArenaBakeData arena = CreateArena(
                halfExtent: 20f,
                walls: Array.Empty<BakedWall>(),
                gates: new[] { gate },
                playerOneSpawn: new float2(8f, 0f));
            MatchSimulation simulation = CreateCarriedSimulation(arena);
            CoreForcedDropRequest request = new CoreForcedDropRequest(
                CoreDropReason.Pulse,
                new float2(12f, 0f),
                2u);
            simulation.TryQueueForcedCoreDrop(in request);

            bool becameInvalid = false;
            bool recovered = false;
            for (int index = 0; index < 40; index++)
            {
                Step(simulation);
                if (simulation.State.Core.InvalidTickCount > 0u)
                {
                    becameInvalid = true;
                }
                else if (becameInvalid)
                {
                    recovered = true;
                    break;
                }
            }

            Assert.That(becameInvalid, Is.True);
            Assert.That(recovered, Is.True);
            Assert.That(simulation.State.Core.InvalidTickCount, Is.Zero);
            Assert.That(simulation.State.Core.Mode, Is.Not.EqualTo(CoreMode.Resetting));
        }

        [Test]
        public void ReplayingSameInvalidScenarioProducesSameResetTickAndChecksum()
        {
            PlaybackResult first = RunInvalidScenario();
            PlaybackResult second = RunInvalidScenario();

            Assert.That(second.Tick, Is.EqualTo(first.Tick));
            Assert.That(second.Mode, Is.EqualTo(CoreMode.Resetting));
            Assert.That(second.Checksum, Is.EqualTo(first.Checksum));
        }

        private static PlaybackResult RunInvalidScenario()
        {
            ArenaBakeData arena = CreateArena(
                halfExtent: 3f,
                walls: Array.Empty<BakedWall>(),
                gates: Array.Empty<BakedShockGate>());
            MatchSimulation simulation = CreateCarriedSimulation(arena);
            CoreForcedDropRequest request = new CoreForcedDropRequest(
                CoreDropReason.Pulse,
                new float2(-12f, 2f),
                3u);
            simulation.TryQueueForcedCoreDrop(in request);

            int safety = 0;
            while (simulation.State.Core.Mode != CoreMode.Resetting && safety++ < 400)
            {
                Step(simulation);
            }

            Assert.That(safety, Is.LessThan(400));
            return new PlaybackResult(
                simulation.State.Tick,
                simulation.State.Core.Mode,
                simulation.ComputeChecksum());
        }

        private static MatchSimulation CreateCarriedSimulation(ArenaBakeData arena)
        {
            BakedSpawn zero = FindSpawn(arena, PlayerSlot.Zero);
            BakedSpawn one = FindSpawn(arena, PlayerSlot.One);
            MatchSimulation simulation = SimulationTestFactory.Create(
                arena,
                zero.Position,
                one.Position);
            InputCommand pickup = Interact(1u, 1u);
            Step(simulation, in pickup, default);
            Assert.That(simulation.State.Core.IsCarriedBy(PlayerSlot.Zero), Is.True);
            return simulation;
        }

        private static BakedSpawn FindSpawn(ArenaBakeData arena, PlayerSlot slot)
        {
            for (int index = 0; index < arena.Spawns.Count; index++)
            {
                if (arena.Spawns[index].PlayerSlot == slot.Index)
                {
                    return arena.Spawns[index];
                }
            }

            throw new InvalidOperationException("Missing test spawn.");
        }

        private static InputCommand Interact(uint tick, uint sequence)
        {
            return SimulationTestFactory.Command(float2.zero, InputButtons.Interact, tick, sequence);
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

        private static void Advance(MatchSimulation simulation, int ticks)
        {
            for (int index = 0; index < ticks; index++)
            {
                Step(simulation);
            }
        }

        private static BakedWall CreateVerticalWall(
            ArenaElementId id,
            float x,
            float minimumY,
            float maximumY)
        {
            const float halfThickness = 0.05f;
            return new BakedWall(
                id,
                new float2(x, minimumY),
                new float2(x, maximumY),
                new float2(1f, 0f),
                halfThickness * 2f,
                new ArenaAabb(
                    new float2(x - halfThickness, minimumY),
                    new float2(x + halfThickness, maximumY)),
                new ArenaAabb(
                    new float2(x - halfThickness - 0.45f, minimumY - 0.45f),
                    new float2(x + halfThickness + 0.45f, maximumY + 0.45f)));
        }

        private static BakedWall CreateHorizontalWall(
            ArenaElementId id,
            float minimumX,
            float maximumX,
            float y)
        {
            const float halfThickness = 0.05f;
            return new BakedWall(
                id,
                new float2(minimumX, y),
                new float2(maximumX, y),
                new float2(0f, 1f),
                halfThickness * 2f,
                new ArenaAabb(
                    new float2(minimumX, y - halfThickness),
                    new float2(maximumX, y + halfThickness)),
                new ArenaAabb(
                    new float2(minimumX - 0.45f, y - halfThickness - 0.45f),
                    new float2(maximumX + 0.45f, y + halfThickness + 0.45f)));
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

        private static ArenaBakeData CreateArena(
            float halfExtent,
            BakedWall[] walls,
            BakedShockGate[] gates,
            float2 playerZeroSpawn = default,
            float2 playerOneSpawn = default,
            BakedNavigationNode[] navigationNodes = null!)
        {
            if (math.all(playerOneSpawn == float2.zero))
            {
                playerOneSpawn = new float2(2f, 0f);
            }

            return new ArenaBakeData(
                1,
                "core-reset-test",
                "tests",
                Array.Empty<ArenaElementDescriptor>(),
                new BakedArenaBounds(new ArenaElementId(1), float2.zero, new float2(halfExtent)),
                walls,
                Array.Empty<BakedConvexObstacle>(),
                Array.Empty<BakedCircle>(),
                gates,
                Array.Empty<BakedBoostPad>(),
                new[]
                {
                    new BakedSpawn(new ArenaElementId(4), 0, playerZeroSpawn, new float2(1f, 0f)),
                    new BakedSpawn(new ArenaElementId(5), 1, playerOneSpawn, new float2(-1f, 0f)),
                },
                new BakedCoreReset(new ArenaElementId(2), float2.zero, 0.5f),
                Array.Empty<BakedBarrierForbiddenVolume>(),
                navigationNodes ?? Array.Empty<BakedNavigationNode>(),
                Array.Empty<BakedNavigationEdge>(),
                new BakedCameraBounds(new ArenaElementId(3), float2.zero, new float2(halfExtent), halfExtent));
        }

        private readonly struct PlaybackResult
        {
            public PlaybackResult(SimulationTick tick, CoreMode mode, ulong checksum)
            {
                Tick = tick;
                Mode = mode;
                Checksum = checksum;
            }

            public SimulationTick Tick { get; }
            public CoreMode Mode { get; }
            public ulong Checksum { get; }
        }
    }
}
