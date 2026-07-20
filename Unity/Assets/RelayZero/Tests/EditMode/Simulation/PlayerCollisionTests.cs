using System;
using NUnit.Framework;
using RelayZero.Arena;
using RelayZero.Arena.Baking;
using RelayZero.Foundation;
using RelayZero.Simulation;
using Unity.Mathematics;
using UnityEditor;

namespace RelayZero.Tests.EditMode.Simulation
{
    public sealed class PlayerCollisionTests
    {
        private const float Radius = 0.45f;
        private const string SwitchyardBakePath = "Assets/RelayZero/Arena/SwitchyardArenaBake.asset";

        [Test]
        public void StaticValidityRejectsBoundsWallsPylonsAndNonFinitePositions()
        {
            ArenaBakeData arena = CreateWallAndPylonArena();

            Assert.That(StaticPlayerCollisionQueries.IsValidPosition(arena, new float2(0f, 0f), Radius), Is.True);
            Assert.That(StaticPlayerCollisionQueries.IsValidPosition(arena, new float2(4.7f, 0f), Radius), Is.False);
            Assert.That(StaticPlayerCollisionQueries.IsValidPosition(arena, new float2(1.8f, 0f), Radius), Is.False);
            Assert.That(
                StaticPlayerCollisionQueries.IsValidPosition(arena, new float2(3f, 0f), Radius),
                Is.True,
                "An open interior wall whose endpoints touch the AABB must not become an infinite half-plane.");
            Assert.That(StaticPlayerCollisionQueries.IsValidPosition(arena, new float2(-2f, 0f), Radius), Is.False);
            Assert.That(StaticPlayerCollisionQueries.IsValidPosition(arena, new float2(float.NaN, 0f), Radius), Is.False);
            Assert.That(StaticPlayerCollisionQueries.IsValidPosition(arena, new float2(float.PositiveInfinity, 0f), Radius), Is.False);
            Assert.That(StaticPlayerCollisionQueries.TryRecoverPosition(
                arena,
                new float2(float.NaN, float.PositiveInfinity),
                new float2(0f, 0f),
                new float2(-3f, -3f),
                Radius,
                out float2 recovered), Is.True);
            Assert.That(recovered, Is.EqualTo(new float2(0f, 0f)));
        }

        [Test]
        public void ShallowAcceptedWallContactCanMoveOutwardWithoutStickyRecovery()
        {
            ArenaBakeData arena = CreateWallAndPylonArena();
            float2 start = new float2(1.3000005f, 2f);
            Assert.That(StaticPlayerCollisionQueries.IsValidPosition(arena, start, Radius), Is.True);
            MatchSimulation simulation = SimulationTestFactory.Create(
                arena,
                start,
                new float2(-3f, 3f));
            TickInputs inputs = new TickInputs();
            inputs.Set(PlayerSlot.Zero, SimulationTestFactory.Movement(new float2(-1f, 0f)));

            simulation.Step(inputs, new MatchEventBuffer());

            PlayerState player = simulation.State.GetPlayer(PlayerSlot.Zero);
            Assert.That(player.Position.x, Is.LessThan(start.x));
            Assert.That(StaticPlayerCollisionQueries.IsValidPosition(arena, player.Position, Radius), Is.True);
            for (int index = 0; index < simulation.CollisionDiagnostics.Count; index++)
            {
                Assert.That(simulation.CollisionDiagnostics[index].Kind, Is.Not.EqualTo(CollisionDiagnosticKind.Recovery));
                Assert.That(simulation.CollisionDiagnostics[index].Kind, Is.Not.EqualTo(CollisionDiagnosticKind.IterationLimit));
            }
        }

        [Test]
        public void RecoveryDiagnosticReportsRejectedCandidateInsteadOfPreviousValidPosition()
        {
            BakedWall wallWithInvalidBroadPhase = new BakedWall(
                new ArenaElementId(12),
                new float2(2f, -4f),
                new float2(2f, 4f),
                new float2(-1f, 0f),
                0.5f,
                new ArenaAabb(new float2(50f), new float2(51f)),
                new ArenaAabb(new float2(49f), new float2(52f)));
            ArenaBakeData arena = CreateArena(
                new[] { wallWithInvalidBroadPhase },
                Array.Empty<BakedConvexObstacle>());
            float2 start = new float2(1.299f, 2f);
            MatchSimulation simulation = SimulationTestFactory.Create(
                arena,
                start,
                new float2(-3f, 3f));
            TickInputs inputs = new TickInputs();
            inputs.Set(PlayerSlot.Zero, SimulationTestFactory.Movement(new float2(1f, 0f)));

            simulation.Step(inputs, new MatchEventBuffer());

            Assert.That(simulation.CollisionDiagnostics.Count, Is.EqualTo(1));
            CollisionDiagnostic recovery = simulation.CollisionDiagnostics[0];
            Assert.That(recovery.Kind, Is.EqualTo(CollisionDiagnosticKind.Recovery));
            Assert.That(recovery.SweepStart.x, Is.GreaterThan(start.x));
            Assert.That(recovery.SweepStart, Is.EqualTo(start + recovery.RequestedDisplacement));
            Assert.That(recovery.ContactPosition, Is.EqualTo(start));
            Assert.That(simulation.State.GetPlayer(PlayerSlot.Zero).Position, Is.EqualTo(start));
        }

        [Test]
        public void InvalidInitialPositionRecoversToTheStableSlotSpawn()
        {
            ArenaBakeData arena = CreateWallAndPylonArena();
            MatchSimulation simulation = SimulationTestFactory.Create(
                arena,
                new float2(-2f, 0f),
                new float2(0f, -3f));

            PlayerState recovered = simulation.State.GetPlayer(PlayerSlot.Zero);
            Assert.That(recovered.Position, Is.EqualTo(new float2(-3f, -3f)));
            Assert.That(recovered.LastValidPosition, Is.EqualTo(recovered.Position));
            Assert.That(simulation.CollisionDiagnostics.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(simulation.CollisionDiagnostics[0].Kind, Is.EqualTo(CollisionDiagnosticKind.Recovery));
        }

        [Test]
        public void SustainedDiagonalMovementSlidesAlongWallWithoutTunneling()
        {
            ArenaBakeData arena = CreateWallAndPylonArena();
            MatchSimulation simulation = SimulationTestFactory.Create(
                arena,
                new float2(0f, -3f),
                new float2(-3f, 3f));
            TickInputs inputs = new TickInputs();
            inputs.Set(PlayerSlot.Zero, SimulationTestFactory.Movement(new float2(1f, 1f)));
            MatchEventBuffer events = new MatchEventBuffer();
            bool observedImpact = false;

            for (int tick = 0; tick < 180; tick++)
            {
                simulation.Step(inputs, events);
                PlayerState player = simulation.State.GetPlayer(PlayerSlot.Zero);
                Assert.That(StaticPlayerCollisionQueries.IsValidPosition(arena, player.Position, Radius), Is.True);
                AssertNoUnexpectedRecovery(simulation.CollisionDiagnostics, tick);
                observedImpact |= HasDiagnosticKind(
                    simulation.CollisionDiagnostics,
                    CollisionDiagnosticKind.StaticImpact);
            }

            PlayerState final = simulation.State.GetPlayer(PlayerSlot.Zero);
            Assert.That(observedImpact, Is.True);
            Assert.That(final.Position.x, Is.LessThanOrEqualTo(1.302f));
            Assert.That(final.Position.y, Is.GreaterThan(0f), "The tangential component should continue along the wall.");
        }

        [Test]
        public void PairResolutionIsSymmetricAndLeavesNoOverlap()
        {
            MatchSimulation simulation = SimulationTestFactory.Create(
                new float2(-1f, 0f),
                new float2(1f, 0f));
            TickInputs inputs = new TickInputs();
            inputs.Set(PlayerSlot.Zero, SimulationTestFactory.Movement(new float2(1f, 0f)));
            inputs.Set(PlayerSlot.One, SimulationTestFactory.Movement(new float2(-1f, 0f)));
            MatchEventBuffer events = new MatchEventBuffer();
            bool observedPairContact = false;

            for (int tick = 0; tick < 90; tick++)
            {
                simulation.Step(inputs, events);
                PlayerState tickZero = simulation.State.GetPlayer(PlayerSlot.Zero);
                PlayerState tickOne = simulation.State.GetPlayer(PlayerSlot.One);
                Assert.That(tickZero.Position.x, Is.LessThan(tickOne.Position.x), "Players crossed at tick " + tick);
                AssertNoUnexpectedRecovery(simulation.CollisionDiagnostics, tick);
                observedPairContact |= HasDiagnosticKind(
                    simulation.CollisionDiagnostics,
                    CollisionDiagnosticKind.PlayerPairCorrection);
            }

            PlayerState zero = simulation.State.GetPlayer(PlayerSlot.Zero);
            PlayerState one = simulation.State.GetPlayer(PlayerSlot.One);
            float2 normal = math.normalizesafe(one.Position - zero.Position, new float2(1f, 0f));
            Assert.That(observedPairContact, Is.True);
            Assert.That(math.distance(zero.Position, one.Position), Is.GreaterThanOrEqualTo(Radius * 2f - 0.00001f));
            Assert.That(zero.Position.x, Is.EqualTo(-one.Position.x).Within(0.0001f));
            Assert.That(zero.Velocity.x, Is.EqualTo(-one.Velocity.x).Within(0.0001f));
            Assert.That(math.dot(one.Velocity - zero.Velocity, normal), Is.GreaterThanOrEqualTo(-0.00001f));
        }

        [Test]
        public void SweptPairCollisionPreventsCrossingAtLegalExtremeConfiguration()
        {
            const float smallRadius = 0.01f;
            PlayerConfigValues values = new PlayerConfigValues(
                100f,
                100f,
                1000f,
                1000f,
                smallRadius,
                720f);
            MatchSimulation simulation = CreateWithConfig(
                in values,
                new float2(-1f, 0f),
                new float2(1f, 0f));
            TickInputs inputs = new TickInputs();
            inputs.Set(PlayerSlot.Zero, SimulationTestFactory.Movement(new float2(1f, 0f)));
            inputs.Set(PlayerSlot.One, SimulationTestFactory.Movement(new float2(-1f, 0f)));
            MatchEventBuffer events = new MatchEventBuffer();
            bool observedPairContact = false;

            for (int tick = 0; tick < 5; tick++)
            {
                simulation.Step(inputs, events);
                PlayerState zero = simulation.State.GetPlayer(PlayerSlot.Zero);
                PlayerState one = simulation.State.GetPlayer(PlayerSlot.One);
                Assert.That(zero.Position.x, Is.LessThan(one.Position.x), "Players crossed at tick " + tick);
                Assert.That(
                    math.distance(zero.Position, one.Position),
                    Is.GreaterThanOrEqualTo(smallRadius * 2f - 0.00001f));
                AssertNoUnexpectedRecovery(simulation.CollisionDiagnostics, tick);
                observedPairContact |= HasDiagnosticKind(
                    simulation.CollisionDiagnostics,
                    CollisionDiagnosticKind.PlayerPairCorrection);
            }

            Assert.That(observedPairContact, Is.True);
        }

        [Test]
        public void PairSweepFindsEarliestContactBeforeEndpointsCross()
        {
            bool didHit = PlayerPairCollision.TrySweep(
                new float2(-1f, 0f),
                new float2(2f, 0f),
                new float2(1f, 0f),
                new float2(-2f, 0f),
                Radius,
                out PlayerPairSweepHit hit);

            Assert.That(didHit, Is.True);
            Assert.That(hit.StartedOverlapping, Is.False);
            Assert.That(hit.Time, Is.EqualTo(0.275f).Within(0.00001f));
            Assert.That(hit.Normal.x, Is.EqualTo(1f).Within(0.00001f));
            Assert.That(hit.Normal.y, Is.Zero.Within(0.00001f));
        }

        [Test]
        public void WallPinnedPairUsesBoundedCleanupWithoutEnteringStaticGeometry()
        {
            ArenaBakeData arena = CreateWallAndPylonArena();
            MatchSimulation simulation = SimulationTestFactory.Create(
                arena,
                new float2(1.25f, 2f),
                new float2(0.3f, 2f));
            TickInputs inputs = new TickInputs();
            inputs.Set(PlayerSlot.Zero, SimulationTestFactory.Movement(new float2(1f, 0f)));
            inputs.Set(PlayerSlot.One, SimulationTestFactory.Movement(new float2(1f, 0f)));
            MatchEventBuffer events = new MatchEventBuffer();
            bool observedPairContact = false;

            for (int tick = 0; tick < 120; tick++)
            {
                simulation.Step(inputs, events);
                PlayerState zero = simulation.State.GetPlayer(PlayerSlot.Zero);
                PlayerState one = simulation.State.GetPlayer(PlayerSlot.One);
                Assert.That(StaticPlayerCollisionQueries.IsValidPosition(arena, zero.Position, Radius), Is.True);
                Assert.That(StaticPlayerCollisionQueries.IsValidPosition(arena, one.Position, Radius), Is.True);
                Assert.That(math.distance(zero.Position, one.Position), Is.GreaterThanOrEqualTo(Radius * 2f - 0.00001f));
                AssertNoUnexpectedRecovery(simulation.CollisionDiagnostics, tick);
                observedPairContact |= HasDiagnosticKind(
                    simulation.CollisionDiagnostics,
                    CollisionDiagnosticKind.PlayerPairCorrection);
            }

            Assert.That(observedPairContact, Is.True);
        }

        [Test]
        public void PairMathSupportsEqualSplitImmovableAndCoincidentFallback()
        {
            float2 zero = float2.zero;
            float2 one = new float2(0.6f, 0f);
            Assert.That(PlayerPairCollision.TryResolveOverlap(
                ref zero,
                ref one,
                Radius,
                PlayerCollisionMobility.Movable,
                PlayerCollisionMobility.Movable,
                float2.zero,
                out float2 zeroCorrection,
                out float2 oneCorrection), Is.True);
            Assert.That(zeroCorrection, Is.EqualTo(-oneCorrection));
            Assert.That(math.distance(zero, one), Is.GreaterThanOrEqualTo(Radius * 2f));

            zero = float2.zero;
            one = float2.zero;
            Assert.That(PlayerPairCollision.TryResolveOverlap(
                ref zero,
                ref one,
                Radius,
                PlayerCollisionMobility.Immovable,
                PlayerCollisionMobility.Movable,
                new float2(0f, 1f),
                out zeroCorrection,
                out oneCorrection), Is.True);
            Assert.That(zeroCorrection, Is.EqualTo(float2.zero));
            Assert.That(oneCorrection.x, Is.Zero.Within(0.00001f));
            Assert.That(oneCorrection.y, Is.GreaterThanOrEqualTo(Radius * 2f));
        }

        [Test]
        public void SeededSwitchyardPathsRemainFiniteAndContained()
        {
            ArenaBakeData arena = LoadSwitchyard();
            Assert.That(
                StaticPlayerCollisionQueries.IsValidPosition(arena, new float2(13f, 9.5f), Radius),
                Is.False,
                "The rectangular bake bounds must not admit space beyond a chamfered perimeter wall.");
            MatchSimulation simulation = CreateAtBakedSpawns(arena);
            TickInputs inputs = new TickInputs();
            MatchEventBuffer events = new MatchEventBuffer();
            System.Random random = new System.Random(0x520402);

            for (uint tick = 1u; tick <= 12000u; tick++)
            {
                float2 move = new float2(
                    (float)(random.NextDouble() * 2d - 1d),
                    (float)(random.NextDouble() * 2d - 1d));
                InputCommand command = SimulationTestFactory.Movement(move, tick, tick);
                inputs.Set(PlayerSlot.Zero, in command);
                simulation.Step(inputs, events);
                PlayerState player = simulation.State.GetPlayer(PlayerSlot.Zero);

                Assert.That(math.all(math.isfinite(player.Position)), Is.True, "Non-finite position at tick " + tick);
                Assert.That(
                    StaticPlayerCollisionQueries.IsValidPosition(arena, player.Position, Radius),
                    Is.True,
                    "Invalid Switchyard position at tick " + tick + ": " + player.Position);
                AssertNoUnexpectedRecovery(simulation.CollisionDiagnostics, (int)tick);
                Assert.That(simulation.CollisionDiagnostics.DroppedCount, Is.Zero, "Dropped diagnostics at tick " + tick);
            }
        }

        [Test]
        public void FixedPlaybackMatchesAcrossThirtySixtyAndOneFortyFourRenderCadences()
        {
            PlaybackResult thirty = RunPlayback(30);
            PlaybackResult sixty = RunPlayback(60);
            PlaybackResult oneFortyFour = RunPlayback(144);

            Assert.That(thirty.Tick, Is.EqualTo(new SimulationTick(1200u)));
            Assert.That(sixty.Tick, Is.EqualTo(thirty.Tick));
            Assert.That(oneFortyFour.Tick, Is.EqualTo(thirty.Tick));
            Assert.That(sixty.Position, Is.EqualTo(thirty.Position));
            Assert.That(oneFortyFour.Position, Is.EqualTo(thirty.Position));
            Assert.That(sixty.Checksum, Is.EqualTo(thirty.Checksum));
            Assert.That(oneFortyFour.Checksum, Is.EqualTo(thirty.Checksum));
        }

        [Test]
        public void WarmedMovementTickHasNoRecurringManagedAllocation()
        {
            ArenaBakeData arena = CreateWallAndPylonArena();
            MatchSimulation simulation = SimulationTestFactory.Create(
                arena,
                new float2(0f, 2f),
                new float2(-3f, -3f));
            TickInputs inputs = new TickInputs();
            InputCommand command = SimulationTestFactory.Movement(new float2(1f, 0f));
            inputs.Set(PlayerSlot.Zero, in command);
            MatchEventBuffer events = new MatchEventBuffer();

            for (int warmup = 0; warmup < 128; warmup++)
            {
                simulation.Step(inputs, events);
            }

            Assert.That(
                HasDiagnosticKind(simulation.CollisionDiagnostics, CollisionDiagnosticKind.StaticImpact),
                Is.True,
                "Warmup must place the player in sustained wall contact before measuring allocations.");

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int tick = 0; tick < 1024; tick++)
            {
                simulation.Step(inputs, events);
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero, "A warmed simulation tick must reuse its state and diagnostic buffers.");
        }

        private static PlaybackResult RunPlayback(int renderedFramesPerSecond)
        {
            ArenaBakeData arena = LoadSwitchyard();
            MatchSimulation simulation = CreateAtBakedSpawns(arena);
            PlaybackStep playback = new PlaybackStep(simulation);
            FixedTickRunner runner = new FixedTickRunner();
            int frameCount = renderedFramesPerSecond * 20;
            double frameDuration = 1d / renderedFramesPerSecond;
            for (int frame = 0; frame < frameCount; frame++)
            {
                runner.Advance(frameDuration, playback);
            }

            PlayerState player = simulation.State.GetPlayer(PlayerSlot.Zero);
            return new PlaybackResult(simulation.State.Tick, player.Position, simulation.ComputeChecksum());
        }

        private static MatchSimulation CreateWithConfig(
            in PlayerConfigValues values,
            float2 playerZeroPosition,
            float2 playerOnePosition)
        {
            MatchConfig config = ConfigCompiler.Compile(in values);
            MatchRoster roster = new MatchRoster(
                new PlayerId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
                new PlayerId(Guid.Parse("22222222-2222-2222-2222-222222222222")));
            MatchInitialization initialization = new MatchInitialization(
                new MatchId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
                0xC0FFEEul,
                roster,
                playerZeroPosition,
                playerOnePosition);
            MatchSimulation simulation = new MatchSimulation();
            simulation.Initialize(config, SimulationTestFactory.CreateOpenArena(), in initialization);
            return simulation;
        }

        private static bool HasDiagnosticKind(
            CollisionDiagnosticBuffer diagnostics,
            CollisionDiagnosticKind kind)
        {
            for (int index = 0; index < diagnostics.Count; index++)
            {
                if (diagnostics[index].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AssertNoUnexpectedRecovery(CollisionDiagnosticBuffer diagnostics, int tick)
        {
            for (int index = 0; index < diagnostics.Count; index++)
            {
                CollisionDiagnosticKind kind = diagnostics[index].Kind;
                Assert.That(kind, Is.Not.EqualTo(CollisionDiagnosticKind.Recovery), "Unexpected recovery at tick " + tick);
                Assert.That(kind, Is.Not.EqualTo(CollisionDiagnosticKind.IterationLimit), "Iteration limit at tick " + tick);
            }
        }

        private static MatchSimulation CreateAtBakedSpawns(ArenaBakeData arena)
        {
            BakedSpawn zero = FindSpawn(arena, 0);
            BakedSpawn one = FindSpawn(arena, 1);
            PlayerConfigValues values = PlayerConfigValues.GddDefaults;
            MatchConfig config = ConfigCompiler.Compile(in values);
            MatchRoster roster = new MatchRoster(
                new PlayerId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
                new PlayerId(Guid.Parse("22222222-2222-2222-2222-222222222222")));
            MatchInitialization initialization = new MatchInitialization(
                new MatchId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
                0xC0FFEEul,
                roster,
                zero.Position,
                one.Position,
                zero.FacingDirection,
                one.FacingDirection);
            MatchSimulation simulation = new MatchSimulation();
            simulation.Initialize(config, arena, in initialization);
            return simulation;
        }

        private static ArenaBakeData LoadSwitchyard()
        {
            ArenaBakeAsset asset = AssetDatabase.LoadAssetAtPath<ArenaBakeAsset>(SwitchyardBakePath);
            Assert.That(asset, Is.Not.Null, "Bake the Switchyard before running collision tests.");
            return asset.CreateRuntimeData();
        }

        private static BakedSpawn FindSpawn(ArenaBakeData arena, byte slot)
        {
            for (int index = 0; index < arena.Spawns.Count; index++)
            {
                if (arena.Spawns[index].PlayerSlot == slot)
                {
                    return arena.Spawns[index];
                }
            }

            throw new InvalidOperationException("Missing slot " + slot + " spawn.");
        }

        private static ArenaBakeData CreateWallAndPylonArena()
        {
            BakedWall wall = new BakedWall(
                new ArenaElementId(10),
                new float2(2f, -5f),
                new float2(2f, 5f),
                new float2(-1f, 0f),
                0.5f,
                new ArenaAabb(new float2(1.75f, -5.25f), new float2(2.25f, 5.25f)),
                new ArenaAabb(new float2(1.3f, -5.7f), new float2(2.7f, 5.7f)));
            float2[] vertices =
            {
                new float2(-2.5f, -0.5f),
                new float2(-1.5f, -0.5f),
                new float2(-1.5f, 0.5f),
                new float2(-2.5f, 0.5f),
            };
            BakedConvexObstacle obstacle = new BakedConvexObstacle(
                new ArenaElementId(11),
                vertices,
                ArenaGeometry.ComputeOutwardNormals(vertices),
                new ArenaAabb(new float2(-2.5f, -0.5f), new float2(-1.5f, 0.5f)),
                new ArenaAabb(new float2(-2.95f, -0.95f), new float2(-1.05f, 0.95f)));
            return CreateArena(new[] { wall }, new[] { obstacle });
        }

        private static ArenaBakeData CreateArena(BakedWall[] walls, BakedConvexObstacle[] obstacles)
        {
            return new ArenaBakeData(
                1,
                "collision-tests",
                "tests",
                Array.Empty<ArenaElementDescriptor>(),
                new BakedArenaBounds(new ArenaElementId(1), float2.zero, new float2(5f)),
                walls,
                obstacles,
                Array.Empty<BakedCircle>(),
                Array.Empty<BakedShockGate>(),
                Array.Empty<BakedBoostPad>(),
                new[]
                {
                    new BakedSpawn(new ArenaElementId(2), 0, new float2(-3f, -3f), new float2(0f, 1f)),
                    new BakedSpawn(new ArenaElementId(3), 1, new float2(-3f, 3f), new float2(0f, -1f)),
                },
                new BakedCoreReset(new ArenaElementId(4), float2.zero, 0.25f),
                Array.Empty<BakedBarrierForbiddenVolume>(),
                Array.Empty<BakedNavigationNode>(),
                Array.Empty<BakedNavigationEdge>(),
                new BakedCameraBounds(new ArenaElementId(5), float2.zero, new float2(5f), 5f));
        }

        private sealed class PlaybackStep : IFixedTickStep
        {
            private readonly MatchSimulation simulation;
            private readonly TickInputs inputs = new TickInputs();
            private readonly MatchEventBuffer events = new MatchEventBuffer();

            public PlaybackStep(MatchSimulation simulation)
            {
                this.simulation = simulation;
            }

            public void Step()
            {
                SimulationTick tick = simulation.State.Tick.Next();
                uint phase = (tick.Value - 1u) % 480u;
                float2 move = phase < 120u
                    ? new float2(1f, 0f)
                    : phase < 240u
                        ? new float2(0f, 1f)
                        : phase < 360u
                            ? new float2(-1f, 0f)
                            : new float2(0f, -1f);
                InputCommand command = new InputCommand(tick, new CommandSequence(tick.Value), move);
                inputs.Set(PlayerSlot.Zero, in command);
                simulation.Step(inputs, events);
            }
        }

        private readonly struct PlaybackResult
        {
            public PlaybackResult(SimulationTick tick, float2 position, ulong checksum)
            {
                Tick = tick;
                Position = position;
                Checksum = checksum;
            }

            public SimulationTick Tick { get; }
            public float2 Position { get; }
            public ulong Checksum { get; }
        }
    }
}
