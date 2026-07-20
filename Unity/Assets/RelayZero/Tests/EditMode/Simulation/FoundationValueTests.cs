using System;
using NUnit.Framework;
using RelayZero.Foundation;

namespace RelayZero.Tests.EditMode.Simulation
{
    public sealed class FoundationValueTests
    {
        [Test]
        public void SimulationTickOrdersAcrossUIntWrap()
        {
            SimulationTick beforeWrap = new SimulationTick(uint.MaxValue);
            SimulationTick afterWrap = beforeWrap.Next();

            Assert.That(afterWrap, Is.EqualTo(SimulationTick.Zero));
            Assert.That(afterWrap.IsNewerThan(beforeWrap), Is.True);
            Assert.That(beforeWrap.IsNewerThan(afterWrap), Is.False);
            Assert.That(afterWrap.DistanceSince(beforeWrap), Is.EqualTo(1u));
        }

        [Test]
        public void CommandSequenceOrdersAcrossUIntWrap()
        {
            CommandSequence beforeWrap = new CommandSequence(uint.MaxValue);
            CommandSequence afterWrap = beforeWrap.Next();

            Assert.That(afterWrap, Is.EqualTo(CommandSequence.Zero));
            Assert.That(afterWrap.IsNewerThan(beforeWrap), Is.True);
            Assert.That(beforeWrap.IsNewerThan(afterWrap), Is.False);
            Assert.That(afterWrap.DistanceSince(beforeWrap), Is.EqualTo(1u));
        }

        [Test]
        public void HalfRangeIsIntentionallyAmbiguous()
        {
            SimulationTick firstTick = SimulationTick.Zero;
            SimulationTick oppositeTick = new SimulationTick(0x80000000u);
            CommandSequence firstSequence = CommandSequence.Zero;
            CommandSequence oppositeSequence = new CommandSequence(0x80000000u);

            Assert.That(firstTick.IsNewerThan(oppositeTick), Is.False);
            Assert.That(oppositeTick.IsNewerThan(firstTick), Is.False);
            Assert.That(firstSequence.IsNewerThan(oppositeSequence), Is.False);
            Assert.That(oppositeSequence.IsNewerThan(firstSequence), Is.False);
        }

        [Test]
        public void PlayerSlotAcceptsExactlyTwoValues()
        {
            Assert.That(PlayerSlot.Zero.Other, Is.EqualTo(PlayerSlot.One));
            Assert.That(PlayerSlot.One.Other, Is.EqualTo(PlayerSlot.Zero));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new PlayerSlot(2));
        }
    }
}
