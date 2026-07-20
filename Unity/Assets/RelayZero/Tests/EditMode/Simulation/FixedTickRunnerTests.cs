using System;
using NUnit.Framework;
using RelayZero.Foundation;
using RelayZero.Simulation;
using Unity.Mathematics;

namespace RelayZero.Tests.EditMode.Simulation
{
    public sealed class FixedTickRunnerTests
    {
        [Test]
        public void RunnerAccumulatesPartialRenderFramesUntilATickIsDue()
        {
            MatchSimulation simulation = SimulationTestFactory.Create();
            FixedTickRunner runner = new FixedTickRunner();
            TickInputs inputs = new TickInputs();
            MatchEventBuffer events = new MatchEventBuffer();
            SimulationStep step = new SimulationStep(simulation, inputs, events);

            Assert.That(runner.Advance(1d / 120d, step), Is.Zero);
            Assert.That(simulation.State.Tick, Is.EqualTo(SimulationTick.Zero));
            Assert.That(step.InvocationCount, Is.Zero);
            Assert.That(runner.Advance(1d / 120d, step), Is.EqualTo(1));
            Assert.That(simulation.State.Tick, Is.EqualTo(new SimulationTick(1u)));
            Assert.That(step.InvocationCount, Is.EqualTo(1));
        }

        [Test]
        public void RunnerCapsCatchUpAtEightAndPreservesBacklog()
        {
            MatchSimulation simulation = SimulationTestFactory.Create();
            FixedTickRunner runner = new FixedTickRunner();
            TickInputs inputs = new TickInputs();
            MatchEventBuffer events = new MatchEventBuffer();
            SimulationStep step = new SimulationStep(simulation, inputs, events);

            Assert.That(runner.Advance(20d / 60d, step), Is.EqualTo(8));
            Assert.That(simulation.State.Tick, Is.EqualTo(new SimulationTick(8u)));
            Assert.That(runner.PendingWholeTicks, Is.EqualTo(12));
            Assert.That(runner.HasDegradedBacklog, Is.True);
            Assert.That(runner.Advance(0d, step), Is.EqualTo(8));
            Assert.That(runner.Advance(0d, step), Is.EqualTo(4));
            Assert.That(simulation.State.Tick, Is.EqualTo(new SimulationTick(20u)));
            Assert.That(runner.PendingWholeTicks, Is.Zero);
            Assert.That(step.InvocationCount, Is.EqualTo(20));
        }

        [Test]
        public void DegradedFlagStartsOnlyAboveOneHundredMillisecondsOfRetainedBacklog()
        {
            MatchSimulation boundarySimulation = SimulationTestFactory.Create();
            FixedTickRunner boundaryRunner = new FixedTickRunner();
            SimulationStep boundaryStep = new SimulationStep(
                boundarySimulation,
                new TickInputs(),
                new MatchEventBuffer());
            boundaryRunner.Advance(14d / 60d, boundaryStep);

            MatchSimulation degradedSimulation = SimulationTestFactory.Create();
            FixedTickRunner degradedRunner = new FixedTickRunner();
            SimulationStep degradedStep = new SimulationStep(
                degradedSimulation,
                new TickInputs(),
                new MatchEventBuffer());
            degradedRunner.Advance(15d / 60d, degradedStep);

            Assert.That(boundaryRunner.BacklogSeconds, Is.EqualTo(0.1d).Within(0.000000001d));
            Assert.That(boundaryRunner.HasDegradedBacklog, Is.False);
            Assert.That(degradedRunner.BacklogSeconds, Is.GreaterThan(0.1d));
            Assert.That(degradedRunner.HasDegradedBacklog, Is.True);
        }

        [Test]
        public void EquivalentThirtySixtyAndOneFortyFourHzCadencesProduceIdenticalState()
        {
            PlaybackResult thirty = RunOneSecond(30);
            PlaybackResult sixty = RunOneSecond(60);
            PlaybackResult oneFortyFour = RunOneSecond(144);

            Assert.That(thirty.Tick, Is.EqualTo(new SimulationTick(60u)));
            Assert.That(sixty.Tick, Is.EqualTo(thirty.Tick));
            Assert.That(oneFortyFour.Tick, Is.EqualTo(thirty.Tick));
            Assert.That(sixty.Position, Is.EqualTo(thirty.Position));
            Assert.That(oneFortyFour.Position, Is.EqualTo(thirty.Position));
            Assert.That(sixty.Velocity, Is.EqualTo(thirty.Velocity));
            Assert.That(oneFortyFour.Velocity, Is.EqualTo(thirty.Velocity));
            Assert.That(sixty.Checksum, Is.EqualTo(thirty.Checksum));
            Assert.That(oneFortyFour.Checksum, Is.EqualTo(thirty.Checksum));
            Assert.That(thirty.Position.x, Is.EqualTo(5.745f).Within(0.0001f));
        }

        [Test]
        public void CatchUpDriverCanResolveDifferentInputForEveryDueTick()
        {
            MatchSimulation simulation = SimulationTestFactory.Create();
            FixedTickRunner runner = new FixedTickRunner();
            PerTickInputStep step = new PerTickInputStep(simulation);

            Assert.That(runner.Advance(2d / 60d, step), Is.EqualTo(2));

            PlayerState player = simulation.State.GetPlayer(PlayerSlot.Zero);
            Assert.That(step.InvocationCount, Is.EqualTo(2));
            Assert.That(player.Velocity, Is.EqualTo(float2.zero));
            Assert.That(player.Position.x, Is.EqualTo((38f / 60f) / 60f).Within(0.00001f));
        }

        [Test]
        public void RunnerRejectsInvalidElapsedTimeWithoutAdvancingState()
        {
            MatchSimulation simulation = SimulationTestFactory.Create();
            FixedTickRunner runner = new FixedTickRunner();
            TickInputs inputs = new TickInputs();
            MatchEventBuffer events = new MatchEventBuffer();
            SimulationStep step = new SimulationStep(simulation, inputs, events);

            Assert.Throws<ArgumentOutOfRangeException>(() => runner.Advance(double.NaN, step));
            Assert.Throws<ArgumentOutOfRangeException>(() => runner.Advance(double.PositiveInfinity, step));
            Assert.Throws<ArgumentOutOfRangeException>(() => runner.Advance(-0.01d, step));
            Assert.That(simulation.State.Tick, Is.EqualTo(SimulationTick.Zero));
            Assert.That(runner.PendingWholeTicks, Is.Zero);
        }

        private static PlaybackResult RunOneSecond(int renderedFramesPerSecond)
        {
            MatchSimulation simulation = SimulationTestFactory.Create(
                new float2(0f, 0f),
                new float2(10f, 0f));
            FixedTickRunner runner = new FixedTickRunner();
            TickInputs inputs = new TickInputs();
            inputs.Set(PlayerSlot.Zero, SimulationTestFactory.Movement(new float2(1f, 0f)));
            MatchEventBuffer events = new MatchEventBuffer();
            SimulationStep step = new SimulationStep(simulation, inputs, events);

            for (int frame = 0; frame < renderedFramesPerSecond; frame++)
            {
                runner.Advance(1d / renderedFramesPerSecond, step);
            }

            PlayerState player = simulation.State.GetPlayer(PlayerSlot.Zero);
            return new PlaybackResult(simulation.State.Tick, player.Position, player.Velocity, simulation.ComputeChecksum());
        }

        private sealed class SimulationStep : IFixedTickStep
        {
            private readonly MatchSimulation simulation;
            private readonly TickInputs inputs;
            private readonly MatchEventBuffer events;

            public SimulationStep(MatchSimulation simulation, TickInputs inputs, MatchEventBuffer events)
            {
                this.simulation = simulation;
                this.inputs = inputs;
                this.events = events;
            }

            public int InvocationCount { get; private set; }

            public void Step()
            {
                simulation.Step(inputs, events);
                InvocationCount++;
            }
        }

        private sealed class PerTickInputStep : IFixedTickStep
        {
            private readonly MatchSimulation simulation;
            private readonly TickInputs inputs = new TickInputs();
            private readonly MatchEventBuffer events = new MatchEventBuffer();

            public PerTickInputStep(MatchSimulation simulation)
            {
                this.simulation = simulation;
            }

            public int InvocationCount { get; private set; }

            public void Step()
            {
                float2 move = InvocationCount == 0 ? new float2(1f, 0f) : float2.zero;
                inputs.Set(
                    PlayerSlot.Zero,
                    SimulationTestFactory.Movement(move, (uint)InvocationCount, (uint)InvocationCount));
                simulation.Step(inputs, events);
                InvocationCount++;
            }
        }

        private readonly struct PlaybackResult
        {
            public PlaybackResult(SimulationTick tick, float2 position, float2 velocity, ulong checksum)
            {
                Tick = tick;
                Position = position;
                Velocity = velocity;
                Checksum = checksum;
            }

            public SimulationTick Tick { get; }

            public float2 Position { get; }

            public float2 Velocity { get; }

            public ulong Checksum { get; }
        }
    }
}
