using System;
using NUnit.Framework;
using RelayZero.Foundation;
using RelayZero.Simulation;
using Unity.Mathematics;

namespace RelayZero.Tests.EditMode.Simulation
{
    public sealed class InputCommandTests
    {
        [Test]
        public void NonFiniteMovementIsRejectedAtTheBoundary()
        {
            SimulationTick tick = new SimulationTick(10u);
            CommandSequence sequence = new CommandSequence(20u);

            Assert.That(InputCommand.TryCreate(tick, sequence, new float2(float.NaN, 0f), out _), Is.False);
            Assert.That(InputCommand.TryCreate(tick, sequence, new float2(float.PositiveInfinity, 0f), out _), Is.False);
            Assert.That(InputCommand.TryCreate(tick, sequence, new float2(0f, float.NegativeInfinity), out _), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new InputCommand(tick, sequence, new float2(float.NaN, 0f)));
        }

        [Test]
        public void MovementAboveUnitLengthIsNormalizedWithoutChangingDirection()
        {
            InputCommand command = SimulationTestFactory.Movement(new float2(3f, 4f));

            Assert.That(math.length(command.Move), Is.EqualTo(1f).Within(0.00001f));
            Assert.That(command.Move.x, Is.EqualTo(0.6f).Within(0.00001f));
            Assert.That(command.Move.y, Is.EqualTo(0.8f).Within(0.00001f));
        }

        [Test]
        public void AnalogMovementBelowUnitLengthIsPreserved()
        {
            float2 analog = new float2(0.25f, -0.5f);
            InputCommand command = SimulationTestFactory.Movement(analog);

            Assert.That(command.Move, Is.EqualTo(analog));
        }

        [Test]
        public void FullCommandNormalizesAimAndPreservesButtonAndAcknowledgementFields()
        {
            InputCommand command = new InputCommand(
                new SimulationTick(10u),
                new CommandSequence(20u),
                new float2(0.25f, -0.5f),
                new float2(3f, 4f),
                InputButtons.Dash | InputButtons.Pulse,
                InputButtons.Barrier,
                new SimulationTick(9u),
                15u);

            Assert.That(command.Aim.x, Is.EqualTo(0.6f).Within(0.00001f));
            Assert.That(command.Aim.y, Is.EqualTo(0.8f).Within(0.00001f));
            Assert.That(command.PressedButtons, Is.EqualTo(InputButtons.Dash | InputButtons.Pulse));
            Assert.That(command.HeldButtons, Is.EqualTo(InputButtons.Barrier));
            Assert.That(command.LastReceivedServerTick, Is.EqualTo(new SimulationTick(9u)));
            Assert.That(command.LastReceivedSnapshotSequence, Is.EqualTo(15u));
        }

        [Test]
        public void NonFiniteAimAndUndefinedButtonsAreRejectedAtTheBoundary()
        {
            SimulationTick tick = new SimulationTick(10u);
            CommandSequence sequence = new CommandSequence(20u);
            InputButtons undefined = (InputButtons)(1 << 15);

            Assert.That(
                InputCommand.TryCreate(
                    tick,
                    sequence,
                    float2.zero,
                    new float2(float.NaN, 0f),
                    InputButtons.None,
                    InputButtons.None,
                    SimulationTick.Zero,
                    0u,
                    out _),
                Is.False);
            Assert.That(
                InputCommand.TryCreate(
                    tick,
                    sequence,
                    float2.zero,
                    new float2(1f, 0f),
                    undefined,
                    InputButtons.None,
                    SimulationTick.Zero,
                    0u,
                    out _),
                Is.False);
        }

        [Test]
        public void ThreeArgumentConstructorKeepsExtendedFieldsNeutral()
        {
            InputCommand command = new InputCommand(
                new SimulationTick(1u),
                new CommandSequence(2u),
                new float2(1f, 0f));

            Assert.That(command.Aim, Is.EqualTo(float2.zero));
            Assert.That(command.PressedButtons, Is.EqualTo(InputButtons.None));
            Assert.That(command.HeldButtons, Is.EqualTo(InputButtons.None));
            Assert.That(command.LastReceivedServerTick, Is.EqualTo(SimulationTick.Zero));
            Assert.That(command.LastReceivedSnapshotSequence, Is.Zero);
        }
    }
}
