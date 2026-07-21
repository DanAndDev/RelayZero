using System;
using NUnit.Framework;
using RelayZero.Foundation;
using RelayZero.Simulation;
using Unity.Mathematics;

namespace RelayZero.Tests.EditMode.Simulation
{
    public sealed class RegulationAllocationTests
    {
        [Test]
        public void WarmedEligibleScoringAndClockTicksAllocateNoManagedMemory()
        {
            MatchSimulation simulation = SimulationTestFactory.Create(
                new float2(-0.8f, 0f),
                new float2(0.8f, 0f));
            TickInputs inputs = new TickInputs();
            MatchEventBuffer events = new MatchEventBuffer();
            SetCommands(inputs, 1u, InputButtons.Interact);
            simulation.Step(inputs, events);

            SetCommands(inputs, 2u, InputButtons.None);
            for (int tick = 0; tick < 40; tick++)
            {
                simulation.Step(inputs, events);
            }

            Assert.That(simulation.State.Score.PlayerZeroMilliPoints, Is.GreaterThan(0));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int tick = 0; tick < 512; tick++)
            {
                simulation.Step(inputs, events);
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero, "Scoring, score events, and the regulation clock must reuse fixed storage.");
        }

        private static void SetCommands(TickInputs inputs, uint sequence, InputButtons playerZeroButtons)
        {
            InputCommand playerZero = SimulationTestFactory.Command(
                float2.zero,
                playerZeroButtons,
                sequence,
                sequence,
                new float2(1f, 0f));
            InputCommand playerOne = SimulationTestFactory.Command(
                float2.zero,
                InputButtons.None,
                sequence,
                sequence,
                new float2(-1f, 0f));
            inputs.Set(PlayerSlot.Zero, in playerZero);
            inputs.Set(PlayerSlot.One, in playerOne);
        }
    }
}
