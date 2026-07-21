using System;
using NUnit.Framework;
using RelayZero.Arena;
using RelayZero.Foundation;
using RelayZero.Simulation;
using Unity.Mathematics;

namespace RelayZero.Tests.EditMode.Simulation
{
    public sealed class LooseCoreMovementTests
    {
        [Test]
        public void ForcedLaunchClampsThenAppliesFrameIndependentPerTickDrag()
        {
            MatchSimulation simulation = CreateCarriedSimulation(SimulationTestFactory.CreateOpenArena());
            CoreForcedDropRequest request = new CoreForcedDropRequest(
                CoreDropReason.Pulse,
                new float2(100f, 0f),
                1u);
            simulation.TryQueueForcedCoreDrop(in request);

            Step(simulation);

            float expectedSpeed = 12f * math.exp(-3f / 60f);
            CoreRuntimeState core = simulation.State.Core;
            Assert.That(core.Velocity.x, Is.EqualTo(expectedSpeed).Within(0.00001f));
            Assert.That(core.Velocity.y, Is.Zero.Within(0.00001f));
            Assert.That(core.Position.x, Is.EqualTo(expectedSpeed / 60f).Within(0.00001f));
            Assert.That(math.length(core.Velocity), Is.LessThanOrEqualTo(12f));
        }

        [Test]
        public void WallBounceUsesAuthoredNormalAndTangentialResponseWithoutTunneling()
        {
            ArenaBakeData arena = CreateCollisionArena(includeObstacle: false);
            MatchSimulation simulation = CreateCarriedSimulation(arena);
            CoreForcedDropRequest request = new CoreForcedDropRequest(
                CoreDropReason.Pulse,
                new float2(12f, 4f),
                2u);
            simulation.TryQueueForcedCoreDrop(in request);
            Step(simulation);

            bool bounced = false;
            for (int index = 0; index < 20 && !bounced; index++)
            {
                float2 beforeVelocity = simulation.State.Core.Velocity;
                Step(simulation);
                for (int diagnosticIndex = 0;
                     diagnosticIndex < simulation.CoreDiagnostics.Count;
                     diagnosticIndex++)
                {
                    CoreDiagnostic diagnostic = simulation.CoreDiagnostics[diagnosticIndex];
                    if (diagnostic.Kind != CoreDiagnosticKind.Bounce)
                    {
                        continue;
                    }

                    float2 incoming = beforeVelocity * simulation.Config.Core.PerTickDragMultiplier;
                    float normalSpeed = math.dot(incoming, diagnostic.Normal);
                    float2 normalVelocity = diagnostic.Normal * normalSpeed;
                    float2 tangentVelocity = incoming - normalVelocity;
                    float2 expected = tangentVelocity * 0.85f - normalVelocity * 0.55f;
                    Assert.That(diagnostic.ElementId, Is.EqualTo(new ArenaElementId(10)));
                    Assert.That(diagnostic.Velocity.x, Is.EqualTo(expected.x).Within(0.0001f));
                    Assert.That(diagnostic.Velocity.y, Is.EqualTo(expected.y).Within(0.0001f));
                    bounced = true;
                    break;
                }
            }

            Assert.That(bounced, Is.True);
            Assert.That(simulation.State.Core.Position.x, Is.LessThanOrEqualTo(0.6515f));
            Assert.That(simulation.State.Core.Velocity.x, Is.LessThan(0f));
            Assert.That(math.all(math.isfinite(simulation.State.Core.Position)), Is.True);
            Assert.That(math.all(math.isfinite(simulation.State.Core.Velocity)), Is.True);
        }

        [Test]
        public void ConvexPylonParticipatesInSweptCoreCollision()
        {
            ArenaBakeData arena = CreateCollisionArena(includeObstacle: true);
            MatchSimulation simulation = CreateCarriedSimulation(arena);
            CoreForcedDropRequest request = new CoreForcedDropRequest(
                CoreDropReason.Pulse,
                new float2(12f, 0f),
                3u);
            simulation.TryQueueForcedCoreDrop(in request);

            bool hitObstacle = false;
            for (int index = 0; index < 20 && !hitObstacle; index++)
            {
                Step(simulation);
                for (int diagnosticIndex = 0;
                     diagnosticIndex < simulation.CoreDiagnostics.Count;
                     diagnosticIndex++)
                {
                    CoreDiagnostic diagnostic = simulation.CoreDiagnostics[diagnosticIndex];
                    if (diagnostic.Kind == CoreDiagnosticKind.Bounce &&
                        diagnostic.ElementId == new ArenaElementId(20))
                    {
                        hitObstacle = true;
                        break;
                    }
                }
            }

            Assert.That(hitObstacle, Is.True);
            Assert.That(simulation.State.Core.Position.x, Is.LessThan(1.2015f));
            Assert.That(simulation.State.Core.Velocity.x, Is.LessThan(0f));
        }

        [Test]
        public void LooseCorePassesThroughPlayerWithoutMovingOrKickingThem()
        {
            MatchSimulation simulation = SimulationTestFactory.Create(
                float2.zero,
                new float2(1.5f, 0f));
            InputCommand pickup = Interact(1u, 1u);
            Step(simulation, in pickup, default);
            CoreForcedDropRequest request = new CoreForcedDropRequest(
                CoreDropReason.Pulse,
                new float2(12f, 0f),
                4u);
            simulation.TryQueueForcedCoreDrop(in request);

            for (int index = 0; index < 24; index++)
            {
                Step(simulation);
            }

            Assert.That(simulation.State.Core.Position.x, Is.GreaterThan(1.8f));
            Assert.That(simulation.State.GetPlayer(PlayerSlot.One).Position, Is.EqualTo(new float2(1.5f, 0f)));
            Assert.That(simulation.State.GetPlayer(PlayerSlot.One).Velocity, Is.EqualTo(float2.zero));
        }

        [Test]
        public void CoreEntersRestOnlyAfterExactConfiguredQualificationTicks()
        {
            MatchSimulation simulation = CreateCarriedSimulation(SimulationTestFactory.CreateOpenArena());
            CoreForcedDropRequest request = new CoreForcedDropRequest(
                CoreDropReason.Disconnect,
                float2.zero,
                5u);
            simulation.TryQueueForcedCoreDrop(in request);

            Step(simulation);
            Assert.That(simulation.State.Core.RestTickCount, Is.EqualTo(1u));
            Assert.That(simulation.State.Core.IsResting, Is.False);
            Advance(simulation, 13);
            Assert.That(simulation.State.Core.RestTickCount, Is.EqualTo(14u));
            Assert.That(simulation.State.Core.IsResting, Is.False);

            Step(simulation);

            Assert.That(simulation.State.Core.RestTickCount, Is.EqualTo(15u));
            Assert.That(simulation.State.Core.IsResting, Is.True);
            Assert.That(simulation.State.Core.Velocity, Is.EqualTo(float2.zero));
            Assert.That(simulation.CoreDiagnostics[0].Kind, Is.EqualTo(CoreDiagnosticKind.Rested));
        }

        [Test]
        public void SameCoreStreamEndsIdenticallyAtThirtyAndOneHundredFortyFourRenderFramesPerSecond()
        {
            PlaybackResult atThirty = RunRenderCadence(30);
            PlaybackResult atOneFortyFour = RunRenderCadence(144);

            Assert.That(atThirty.Tick, Is.EqualTo(new SimulationTick(121u)));
            Assert.That(atOneFortyFour.Tick, Is.EqualTo(atThirty.Tick));
            Assert.That(atOneFortyFour.CorePosition.x, Is.EqualTo(atThirty.CorePosition.x).Within(0.000001f));
            Assert.That(atOneFortyFour.CorePosition.y, Is.EqualTo(atThirty.CorePosition.y).Within(0.000001f));
            Assert.That(atOneFortyFour.CoreVelocity.x, Is.EqualTo(atThirty.CoreVelocity.x).Within(0.000001f));
            Assert.That(atOneFortyFour.CoreVelocity.y, Is.EqualTo(atThirty.CoreVelocity.y).Within(0.000001f));
            Assert.That(atOneFortyFour.Checksum, Is.EqualTo(atThirty.Checksum));
        }

        [Test]
        public void SeededWallLaunchesRemainFiniteAndOutsideBlockingGeometry()
        {
            System.Random random = new System.Random(0xC0DE);
            ArenaBakeData arena = CreateCollisionArena(includeObstacle: false);
            for (int iteration = 0; iteration < 96; iteration++)
            {
                MatchSimulation simulation = CreateCarriedSimulation(arena);
                float lateral = -8f + (float)random.NextDouble() * 16f;
                CoreForcedDropRequest request = new CoreForcedDropRequest(
                    CoreDropReason.Pulse,
                    new float2(12f, lateral),
                    (uint)iteration);
                simulation.TryQueueForcedCoreDrop(in request);

                Advance(simulation, 18);

                CoreRuntimeState core = simulation.State.Core;
                Assert.That(math.all(math.isfinite(core.Position)), Is.True, "Position " + iteration);
                Assert.That(math.all(math.isfinite(core.Velocity)), Is.True, "Velocity " + iteration);
                Assert.That(core.Position.x, Is.LessThanOrEqualTo(0.6515f), "Wall penetration " + iteration);
            }
        }

        private static PlaybackResult RunRenderCadence(int framesPerSecond)
        {
            MatchSimulation simulation = CreateCarriedSimulation(SimulationTestFactory.CreateOpenArena());
            CoreForcedDropRequest request = new CoreForcedDropRequest(
                CoreDropReason.Pulse,
                new float2(8f, 2f),
                99u);
            simulation.TryQueueForcedCoreDrop(in request);
            SimulationHarness harness = new SimulationHarness(simulation);
            FixedTickRunner runner = new FixedTickRunner();
            int frameCount = framesPerSecond * 2;
            for (int frame = 0; frame < frameCount; frame++)
            {
                runner.Advance(1d / framesPerSecond, harness);
            }

            CoreRuntimeState core = simulation.State.Core;
            return new PlaybackResult(
                simulation.State.Tick,
                core.Position,
                core.Velocity,
                simulation.ComputeChecksum());
        }

        private static MatchSimulation CreateCarriedSimulation(ArenaBakeData arena)
        {
            MatchSimulation simulation = SimulationTestFactory.Create(
                arena,
                float2.zero,
                new float2(8f, 0f));
            InputCommand pickup = Interact(1u, 1u);
            Step(simulation, in pickup, default);
            Assert.That(simulation.State.Core.IsCarriedBy(PlayerSlot.Zero), Is.True);
            return simulation;
        }

        private static InputCommand Interact(uint tick, uint sequence)
        {
            return SimulationTestFactory.Command(float2.zero, InputButtons.Interact, tick, sequence);
        }

        private static void Step(MatchSimulation simulation)
        {
            InputCommand empty = default;
            Step(simulation, in empty, in empty);
        }

        private static void Step(
            MatchSimulation simulation,
            in InputCommand zero,
            in InputCommand one)
        {
            TickInputs inputs = new TickInputs();
            inputs.Set(PlayerSlot.Zero, in zero);
            inputs.Set(PlayerSlot.One, in one);
            simulation.Step(inputs, new MatchEventBuffer());
        }

        private static void Advance(MatchSimulation simulation, int ticks)
        {
            for (int index = 0; index < ticks; index++)
            {
                Step(simulation);
            }
        }

        private static ArenaBakeData CreateCollisionArena(bool includeObstacle)
        {
            BakedWall[] walls = includeObstacle
                ? Array.Empty<BakedWall>()
                : new[]
                {
                    new BakedWall(
                        new ArenaElementId(10),
                        new float2(1f, -8f),
                        new float2(1f, 8f),
                        new float2(-1f, 0f),
                        0.1f,
                        new ArenaAabb(new float2(0.95f, -8f), new float2(1.05f, 8f)),
                        new ArenaAabb(new float2(0.5f, -8.45f), new float2(1.5f, 8.45f))),
                };
            BakedConvexObstacle[] obstacles = includeObstacle
                ? new[]
                {
                    new BakedConvexObstacle(
                        new ArenaElementId(20),
                        new[]
                        {
                            new float2(1.5f, -1f),
                            new float2(2.5f, -1f),
                            new float2(2.5f, 1f),
                            new float2(1.5f, 1f),
                        },
                        new[]
                        {
                            new float2(0f, -1f),
                            new float2(1f, 0f),
                            new float2(0f, 1f),
                            new float2(-1f, 0f),
                        },
                        new ArenaAabb(new float2(1.5f, -1f), new float2(2.5f, 1f)),
                        new ArenaAabb(new float2(1.05f, -1.45f), new float2(2.95f, 1.45f))),
                }
                : Array.Empty<BakedConvexObstacle>();

            const float halfExtent = 20f;
            return new ArenaBakeData(
                1,
                "loose-core-collision-test",
                "tests",
                Array.Empty<ArenaElementDescriptor>(),
                new BakedArenaBounds(new ArenaElementId(1), float2.zero, new float2(halfExtent)),
                walls,
                obstacles,
                Array.Empty<BakedCircle>(),
                Array.Empty<BakedShockGate>(),
                Array.Empty<BakedBoostPad>(),
                new[]
                {
                    new BakedSpawn(new ArenaElementId(4), 0, float2.zero, new float2(1f, 0f)),
                    new BakedSpawn(new ArenaElementId(5), 1, new float2(8f, 0f), new float2(-1f, 0f)),
                },
                new BakedCoreReset(new ArenaElementId(2), float2.zero, 0.5f),
                Array.Empty<BakedBarrierForbiddenVolume>(),
                Array.Empty<BakedNavigationNode>(),
                Array.Empty<BakedNavigationEdge>(),
                new BakedCameraBounds(new ArenaElementId(3), float2.zero, new float2(halfExtent), halfExtent));
        }

        private sealed class SimulationHarness : IFixedTickStep
        {
            private readonly MatchSimulation simulation;
            private readonly TickInputs inputs = new TickInputs();
            private readonly MatchEventBuffer events = new MatchEventBuffer();

            public SimulationHarness(MatchSimulation simulation)
            {
                this.simulation = simulation;
            }

            public void Step()
            {
                simulation.Step(inputs, events);
            }
        }

        private readonly struct PlaybackResult
        {
            public PlaybackResult(
                SimulationTick tick,
                float2 corePosition,
                float2 coreVelocity,
                ulong checksum)
            {
                Tick = tick;
                CorePosition = corePosition;
                CoreVelocity = coreVelocity;
                Checksum = checksum;
            }

            public SimulationTick Tick { get; }
            public float2 CorePosition { get; }
            public float2 CoreVelocity { get; }
            public ulong Checksum { get; }
        }
    }
}
