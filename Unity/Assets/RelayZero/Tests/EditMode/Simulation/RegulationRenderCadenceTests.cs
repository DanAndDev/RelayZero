using NUnit.Framework;
using RelayZero.Foundation;
using RelayZero.Simulation;
using Unity.Mathematics;

namespace RelayZero.Tests.EditMode.Simulation
{
    public sealed class RegulationRenderCadenceTests
    {
        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        public void RegulationResultIsIndependentOfRenderCadence(int renderedFramesPerSecond)
        {
            PlaybackResult baseline = RunAtRenderCadence(60);
            PlaybackResult candidate = RunAtRenderCadence(renderedFramesPerSecond);

            Assert.That(candidate.Phase, Is.EqualTo(MatchPhase.Results));
            Assert.That(candidate.Tick, Is.EqualTo(baseline.Tick));
            Assert.That(candidate.PlayerZeroMilliPoints, Is.EqualTo(baseline.PlayerZeroMilliPoints));
            Assert.That(candidate.PlayerOneMilliPoints, Is.EqualTo(baseline.PlayerOneMilliPoints));
            Assert.That(candidate.Result.Reason, Is.EqualTo(baseline.Result.Reason));
            Assert.That(candidate.Result.Winner, Is.EqualTo(baseline.Result.Winner));
            Assert.That(candidate.Result.FinalizedTick, Is.EqualTo(baseline.Result.FinalizedTick));
            Assert.That(candidate.Checksum, Is.EqualTo(baseline.Checksum));
        }

        private static PlaybackResult RunAtRenderCadence(int renderedFramesPerSecond)
        {
            PlayerConfigValues player = PlayerConfigValues.GddDefaults;
            CoreConfigValues core = CoreConfigValues.GddDefaults;
            RegulationConfigValues regulation = new RegulationConfigValues(
                0.01f,
                10f,
                1,
                1000,
                2000,
                0.01f,
                0.05f);
            MatchSimulation simulation = SimulationTestFactory.Create(
                SimulationTestFactory.CreateOpenArena(),
                new float2(-0.8f, 0f),
                new float2(0.8f, 0f),
                in player,
                in core,
                in regulation,
                MatchStartMode.Regulation);
            RegulationStep step = new RegulationStep(simulation);
            FixedTickRunner runner = new FixedTickRunner();
            int frameCount = renderedFramesPerSecond * 2;
            for (int frame = 0; frame < frameCount; frame++)
            {
                runner.Advance(1d / renderedFramesPerSecond, step);
            }

            MatchState state = simulation.State;
            Assert.That(state.Clock.HasResult, Is.True);
            return new PlaybackResult(
                state.Tick,
                state.Clock.Phase,
                state.Score.PlayerZeroMilliPoints,
                state.Score.PlayerOneMilliPoints,
                state.Clock.Result,
                simulation.ComputeChecksum());
        }

        private sealed class RegulationStep : IFixedTickStep
        {
            private readonly MatchSimulation simulation;
            private readonly TickInputs inputs = new TickInputs();
            private readonly MatchEventBuffer events = new MatchEventBuffer();

            public RegulationStep(MatchSimulation simulation)
            {
                this.simulation = simulation;
            }

            public void Step()
            {
                SimulationTick tick = simulation.State.Tick.Next();
                InputButtons playerZeroButtons = tick.Value == 1u
                    ? InputButtons.Interact
                    : InputButtons.None;
                InputCommand playerZero = SimulationTestFactory.Command(
                    float2.zero,
                    playerZeroButtons,
                    tick.Value,
                    tick.Value,
                    new float2(1f, 0f));
                InputCommand playerOne = SimulationTestFactory.Command(
                    float2.zero,
                    InputButtons.None,
                    tick.Value,
                    tick.Value,
                    new float2(-1f, 0f));
                inputs.Set(PlayerSlot.Zero, in playerZero);
                inputs.Set(PlayerSlot.One, in playerOne);
                simulation.Step(inputs, events);
            }
        }

        private readonly struct PlaybackResult
        {
            public PlaybackResult(
                SimulationTick tick,
                MatchPhase phase,
                int playerZeroMilliPoints,
                int playerOneMilliPoints,
                MatchResult result,
                ulong checksum)
            {
                Tick = tick;
                Phase = phase;
                PlayerZeroMilliPoints = playerZeroMilliPoints;
                PlayerOneMilliPoints = playerOneMilliPoints;
                Result = result;
                Checksum = checksum;
            }

            public SimulationTick Tick { get; }
            public MatchPhase Phase { get; }
            public int PlayerZeroMilliPoints { get; }
            public int PlayerOneMilliPoints { get; }
            public MatchResult Result { get; }
            public ulong Checksum { get; }
        }
    }
}
