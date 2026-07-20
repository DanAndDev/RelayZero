using System;
using NUnit.Framework;
using RelayZero.Foundation;
using RelayZero.Simulation;

namespace RelayZero.Tests.EditMode.Simulation
{
    public sealed class MatchEventBufferTests
    {
        [Test]
        public void BufferAssignsOrderedIdsAndFailsLoudlyOnOverflow()
        {
            MatchEventBuffer buffer = new MatchEventBuffer();
            SimulationTick tick = new SimulationTick(42u);
            buffer.Reset(tick);

            for (int index = 0; index < MatchEventBuffer.Capacity; index++)
            {
                buffer.Add(MatchEventType.PlayerStateChanged, PlayerSlot.Zero, index);
            }

            Assert.That(buffer.Count, Is.EqualTo(64));
            Assert.That(buffer[0].Id, Is.EqualTo(new MatchEventId(tick, 0)));
            Assert.That(buffer[63].Id, Is.EqualTo(new MatchEventId(tick, 63)));
            Assert.Throws<InvalidOperationException>(
                () => buffer.Add(MatchEventType.PlayerStateChanged, PlayerSlot.One));
        }

        [Test]
        public void BufferReusesStorageForTheNextTick()
        {
            MatchEventBuffer buffer = new MatchEventBuffer();
            buffer.Reset(new SimulationTick(1u));
            buffer.Add(MatchEventType.PlayerStateChanged, PlayerSlot.Zero);

            SimulationTick nextTick = new SimulationTick(2u);
            buffer.Reset(nextTick);
            buffer.Add(MatchEventType.PlayerStateChanged, PlayerSlot.One);

            Assert.That(buffer.Count, Is.EqualTo(1));
            Assert.That(buffer[0].Id, Is.EqualTo(new MatchEventId(nextTick, 0)));
            Assert.That(buffer[0].PlayerSlot, Is.EqualTo(PlayerSlot.One));
            Assert.That(buffer.AsSpan().Length, Is.EqualTo(1));
        }
    }
}
