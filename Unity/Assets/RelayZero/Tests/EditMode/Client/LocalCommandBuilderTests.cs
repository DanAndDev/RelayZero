using System;
using NUnit.Framework;
using RelayZero.Client.Prediction;
using RelayZero.Foundation;
using RelayZero.Simulation;
using Unity.Mathematics;

namespace RelayZero.Tests.EditMode.Client
{
    public sealed class LocalCommandBuilderTests
    {
        [Test]
        public void MoveProvidesAimFallbackUntilExplicitAimIsReceived()
        {
            LocalCommandBuilder builder = new LocalCommandBuilder();

            Assert.That(builder.TrySetMove(new float2(3f, 4f)), Is.True);
            InputCommand fallback = builder.Build(new SimulationTick(10u));

            Assert.That(fallback.Move.x, Is.EqualTo(0.6f).Within(0.00001f));
            Assert.That(fallback.Move.y, Is.EqualTo(0.8f).Within(0.00001f));
            Assert.That(fallback.Aim.x, Is.EqualTo(0.6f).Within(0.00001f));
            Assert.That(fallback.Aim.y, Is.EqualTo(0.8f).Within(0.00001f));
            Assert.That(builder.HasExplicitAim, Is.False);

            Assert.That(builder.TrySetAim(new float2(0f, -2f)), Is.True);
            Assert.That(builder.TrySetMove(new float2(-1f, 0f)), Is.True);
            Assert.That(builder.TrySetAim(float2.zero), Is.False);
            InputCommand explicitAim = builder.Build(new SimulationTick(11u));

            Assert.That(explicitAim.Move, Is.EqualTo(new float2(-1f, 0f)));
            Assert.That(explicitAim.Aim, Is.EqualTo(new float2(0f, -1f)));
            Assert.That(builder.HasExplicitAim, Is.True);
        }

        [Test]
        public void InvalidContinuousSamplesAreRejectedWithoutReplacingValidState()
        {
            LocalCommandBuilder builder = new LocalCommandBuilder();
            Assert.That(builder.TrySetMove(new float2(0.25f, -0.5f)), Is.True);
            Assert.That(builder.TrySetAim(new float2(1f, 0f)), Is.True);

            Assert.That(builder.TrySetMove(new float2(float.NaN, 0f)), Is.False);
            Assert.That(builder.TrySetAim(new float2(float.PositiveInfinity, 0f)), Is.False);

            InputCommand command = builder.Build(new SimulationTick(1u));
            Assert.That(command.Move, Is.EqualTo(new float2(0.25f, -0.5f)));
            Assert.That(command.Aim, Is.EqualTo(new float2(1f, 0f)));
        }

        [Test]
        public void PressEdgesAreOrLatchedAndTransferredExactlyOnce()
        {
            LocalCommandBuilder builder = new LocalCommandBuilder(new CommandSequence(50u));
            builder.LatchPressed(InputButtons.Dash);
            builder.LatchPressed(InputButtons.Pulse);
            builder.LatchPressed(InputButtons.Dash);

            InputCommand first = builder.Build(new SimulationTick(100u));
            InputCommand second = builder.Build(new SimulationTick(101u));

            Assert.That(first.PressedButtons, Is.EqualTo(InputButtons.Dash | InputButtons.Pulse));
            Assert.That(second.PressedButtons, Is.EqualTo(InputButtons.None));
            Assert.That(first.Sequence, Is.EqualTo(new CommandSequence(50u)));
            Assert.That(second.Sequence, Is.EqualTo(new CommandSequence(51u)));
        }

        [Test]
        public void HeldButtonsPersistUntilTheirStateChanges()
        {
            LocalCommandBuilder builder = new LocalCommandBuilder();
            builder.SetHeldButtons(InputButtons.Barrier | InputButtons.Interact);

            InputCommand first = builder.Build(new SimulationTick(1u));
            InputCommand second = builder.Build(new SimulationTick(2u));
            builder.SetButtonHeld(InputButtons.Barrier, false);
            InputCommand released = builder.Build(new SimulationTick(3u));

            Assert.That(first.HeldButtons, Is.EqualTo(InputButtons.Barrier | InputButtons.Interact));
            Assert.That(second.HeldButtons, Is.EqualTo(InputButtons.Barrier | InputButtons.Interact));
            Assert.That(released.HeldButtons, Is.EqualTo(InputButtons.Interact));
        }

        [Test]
        public void GameplayBlockNeutralizesCommandsAndDiscardsActionState()
        {
            LocalCommandBuilder builder = new LocalCommandBuilder();
            builder.TrySetMove(new float2(1f, 0f));
            builder.TrySetAim(new float2(0f, 1f));
            builder.LatchPressed(InputButtons.Dash);
            builder.SetHeldButtons(InputButtons.Barrier);

            builder.SetGameplayBlocked(true);
            Assert.That(builder.TrySetMove(new float2(-1f, 0f)), Is.False);
            Assert.That(builder.TrySetAim(new float2(1f, 0f)), Is.False);
            builder.LatchPressed(InputButtons.Pulse);
            builder.SetHeldButtons(InputButtons.Interact);
            InputCommand blocked = builder.Build(new SimulationTick(5u));

            Assert.That(blocked.Move, Is.EqualTo(float2.zero));
            Assert.That(blocked.Aim, Is.EqualTo(float2.zero));
            Assert.That(blocked.PressedButtons, Is.EqualTo(InputButtons.None));
            Assert.That(blocked.HeldButtons, Is.EqualTo(InputButtons.None));

            builder.SetGameplayBlocked(false);
            InputCommand unblocked = builder.Build(new SimulationTick(6u));

            Assert.That(unblocked.Move, Is.EqualTo(float2.zero));
            Assert.That(unblocked.Aim, Is.EqualTo(new float2(0f, 1f)));
            Assert.That(unblocked.PressedButtons, Is.EqualTo(InputButtons.None));
            Assert.That(unblocked.HeldButtons, Is.EqualTo(InputButtons.None));
        }

        [Test]
        public void BuildIncludesLatestAcknowledgementsAndSequenceWrapsMonotonically()
        {
            LocalCommandBuilder builder = new LocalCommandBuilder(new CommandSequence(uint.MaxValue));
            builder.SetAcknowledgements(new SimulationTick(77u), 88u);

            InputCommand beforeWrap = builder.Build(new SimulationTick(1u));
            InputCommand afterWrap = builder.Build(new SimulationTick(2u));

            Assert.That(beforeWrap.Sequence, Is.EqualTo(new CommandSequence(uint.MaxValue)));
            Assert.That(afterWrap.Sequence, Is.EqualTo(CommandSequence.Zero));
            Assert.That(beforeWrap.LastReceivedServerTick, Is.EqualTo(new SimulationTick(77u)));
            Assert.That(beforeWrap.LastReceivedSnapshotSequence, Is.EqualTo(88u));
        }

        [Test]
        public void UndefinedButtonFlagsAreRejected()
        {
            LocalCommandBuilder builder = new LocalCommandBuilder();
            InputButtons undefined = (InputButtons)(1 << 15);

            Assert.Throws<ArgumentOutOfRangeException>(() => builder.LatchPressed(undefined));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetHeldButtons(undefined));
        }

        [Test]
        public void EquivalentKeyboardMouseAndGamepadSamplesProduceTheSameCommandRepresentation()
        {
            LocalCommandBuilder keyboardMouse = new LocalCommandBuilder(new CommandSequence(7u));
            LocalCommandBuilder gamepad = new LocalCommandBuilder(new CommandSequence(7u));

            ApplyEquivalentSample(keyboardMouse);
            ApplyEquivalentSample(gamepad);
            InputCommand keyboardMouseCommand = keyboardMouse.Build(new SimulationTick(42u));
            InputCommand gamepadCommand = gamepad.Build(new SimulationTick(42u));

            Assert.That(gamepadCommand.ClientTick, Is.EqualTo(keyboardMouseCommand.ClientTick));
            Assert.That(gamepadCommand.Sequence, Is.EqualTo(keyboardMouseCommand.Sequence));
            Assert.That(gamepadCommand.Move, Is.EqualTo(keyboardMouseCommand.Move));
            Assert.That(gamepadCommand.Aim, Is.EqualTo(keyboardMouseCommand.Aim));
            Assert.That(gamepadCommand.PressedButtons, Is.EqualTo(keyboardMouseCommand.PressedButtons));
            Assert.That(gamepadCommand.HeldButtons, Is.EqualTo(keyboardMouseCommand.HeldButtons));
        }

        private static void ApplyEquivalentSample(LocalCommandBuilder builder)
        {
            builder.TrySetMove(new float2(0.5f, -0.25f));
            builder.TrySetAim(new float2(1f, 1f));
            builder.LatchPressed(InputButtons.Dash | InputButtons.Pulse);
            builder.SetHeldButtons(InputButtons.Barrier);
        }
    }
}
