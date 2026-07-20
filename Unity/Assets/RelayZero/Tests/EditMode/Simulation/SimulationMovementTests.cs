using System;
using NUnit.Framework;
using RelayZero.Foundation;
using RelayZero.Simulation;
using Unity.Mathematics;

namespace RelayZero.Tests.EditMode.Simulation
{
    public sealed class SimulationMovementTests
    {
        [Test]
        public void StepAcceleratesThenIntegratesExactlyOneFixedTick()
        {
            MatchSimulation simulation = SimulationTestFactory.Create();
            TickInputs inputs = new TickInputs();
            inputs.Set(PlayerSlot.Zero, SimulationTestFactory.Movement(new float2(1f, 0f)));

            simulation.Step(inputs, new MatchEventBuffer());

            PlayerState player = simulation.State.GetPlayer(PlayerSlot.Zero);
            float firstTickSpeed = 38f / 60f;
            Assert.That(simulation.State.Tick, Is.EqualTo(new SimulationTick(1u)));
            Assert.That(player.Velocity.x, Is.EqualTo(firstTickSpeed).Within(0.00001f));
            Assert.That(player.Velocity.y, Is.Zero);
            Assert.That(player.Position.x, Is.EqualTo(firstTickSpeed / 60f).Within(0.00001f));
        }

        [Test]
        public void SustainedInputAcceleratesWithoutExceedingMaximumSpeed()
        {
            MatchSimulation simulation = SimulationTestFactory.Create(
                new float2(0f, 0f),
                new float2(10f, 0f));
            TickInputs inputs = new TickInputs();
            inputs.Set(PlayerSlot.Zero, SimulationTestFactory.Movement(new float2(1f, 0f)));
            MatchEventBuffer events = new MatchEventBuffer();

            for (int tick = 0; tick < 60; tick++)
            {
                simulation.Step(inputs, events);
                Assert.That(
                    math.length(simulation.State.GetPlayer(PlayerSlot.Zero).Velocity),
                    Is.LessThanOrEqualTo(6.2f + 0.00001f));
            }

            PlayerState player = simulation.State.GetPlayer(PlayerSlot.Zero);
            Assert.That(player.Velocity.x, Is.EqualTo(6.2f).Within(0.00001f));
            Assert.That(player.Position.x, Is.EqualTo(5.745f).Within(0.0001f));
        }

        [Test]
        public void ReleasedInputDeceleratesToRestWithoutReversing()
        {
            MatchSimulation simulation = SimulationTestFactory.Create(
                new float2(0f, 0f),
                new float2(10f, 0f));
            TickInputs inputs = new TickInputs();
            MatchEventBuffer events = new MatchEventBuffer();
            inputs.Set(PlayerSlot.Zero, SimulationTestFactory.Movement(new float2(1f, 0f)));
            for (int tick = 0; tick < 12; tick++)
            {
                simulation.Step(inputs, events);
            }

            float startPosition = simulation.State.GetPlayer(PlayerSlot.Zero).Position.x;
            inputs.Set(PlayerSlot.Zero, SimulationTestFactory.Movement(float2.zero));
            for (int tick = 0; tick < 8; tick++)
            {
                simulation.Step(inputs, events);
                Assert.That(simulation.State.GetPlayer(PlayerSlot.Zero).Velocity.x, Is.GreaterThanOrEqualTo(0f));
            }

            PlayerState stopped = simulation.State.GetPlayer(PlayerSlot.Zero);
            Assert.That(stopped.Velocity, Is.EqualTo(float2.zero));
            Assert.That(stopped.Position.x - startPosition, Is.EqualTo(0.35f).Within(0.0001f));
        }

        [Test]
        public void BothFixedSlotsAdvanceThroughTheSameSimulationStep()
        {
            MatchSimulation simulation = SimulationTestFactory.Create(new float2(-1f, 0f), new float2(1f, 0f));
            TickInputs inputs = new TickInputs();
            inputs.Set(PlayerSlot.Zero, SimulationTestFactory.Movement(new float2(1f, 0f)));
            inputs.Set(PlayerSlot.One, SimulationTestFactory.Movement(new float2(-1f, 0f)));

            simulation.Step(inputs, new MatchEventBuffer());

            PlayerState zero = simulation.State.GetPlayer(PlayerSlot.Zero);
            PlayerState one = simulation.State.GetPlayer(PlayerSlot.One);
            Assert.That(zero.Position.x, Is.GreaterThan(-1f));
            Assert.That(one.Position.x, Is.LessThan(1f));
            Assert.That(math.all(math.isfinite(zero.Position)), Is.True);
            Assert.That(math.all(math.isfinite(one.Position)), Is.True);
        }

        [Test]
        public void FacingTurnsTowardAimAtTheAuthoredPerTickLimit()
        {
            MatchSimulation simulation = SimulationTestFactory.Create();
            TickInputs inputs = new TickInputs();
            InputCommand command = new InputCommand(
                new SimulationTick(1u),
                new CommandSequence(1u),
                float2.zero,
                new float2(1f, 0f),
                InputButtons.None,
                InputButtons.None,
                SimulationTick.Zero,
                0u);
            inputs.Set(PlayerSlot.Zero, in command);

            simulation.Step(inputs, new MatchEventBuffer());

            PlayerState player = simulation.State.GetPlayer(PlayerSlot.Zero);
            float expectedRadians = math.radians(12f);
            Assert.That(player.FacingDirection.x, Is.EqualTo(math.sin(expectedRadians)).Within(0.00001f));
            Assert.That(player.FacingDirection.y, Is.EqualTo(math.cos(expectedRadians)).Within(0.00001f));
        }

        [Test]
        public void DefaultInitializationCannotBypassStrongIdAndRosterValidation()
        {
            PlayerConfigValues values = PlayerConfigValues.GddDefaults;
            MatchConfig config = ConfigCompiler.Compile(in values);
            MatchSimulation simulation = new MatchSimulation();
            MatchInitialization invalid = default;
            RelayZero.Arena.ArenaBakeData arena = SimulationTestFactory.CreateOpenArena();

            Assert.Throws<ArgumentException>(() => simulation.Initialize(config, arena, in invalid));
            Assert.That(simulation.IsInitialized, Is.False);
        }
    }
}
