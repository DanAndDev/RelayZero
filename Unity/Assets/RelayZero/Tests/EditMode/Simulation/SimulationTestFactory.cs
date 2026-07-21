using System;
using RelayZero.Arena;
using RelayZero.Foundation;
using RelayZero.Simulation;
using Unity.Mathematics;

namespace RelayZero.Tests.EditMode.Simulation
{
    internal static class SimulationTestFactory
    {
        public static MatchSimulation Create()
        {
            return Create(new float2(0f, 0f), new float2(2f, 0f));
        }

        public static MatchSimulation Create(float2 playerZeroPosition, float2 playerOnePosition)
        {
            return Create(CreateOpenArena(), playerZeroPosition, playerOnePosition);
        }

        public static MatchSimulation Create(
            ArenaBakeData arena,
            float2 playerZeroPosition,
            float2 playerOnePosition)
        {
            PlayerConfigValues values = PlayerConfigValues.GddDefaults;
            CoreConfigValues coreValues = CoreConfigValues.GddDefaults;
            return Create(arena, playerZeroPosition, playerOnePosition, in values, in coreValues);
        }

        public static MatchSimulation Create(
            ArenaBakeData arena,
            float2 playerZeroPosition,
            float2 playerOnePosition,
            in PlayerConfigValues playerValues,
            in CoreConfigValues coreValues)
        {
            RegulationConfigValues regulationValues = RegulationConfigValues.GddDefaults;
            return Create(
                arena,
                playerZeroPosition,
                playerOnePosition,
                in playerValues,
                in coreValues,
                in regulationValues,
                MatchStartMode.Regulation);
        }

        public static MatchSimulation Create(
            ArenaBakeData arena,
            float2 playerZeroPosition,
            float2 playerOnePosition,
            in PlayerConfigValues playerValues,
            in CoreConfigValues coreValues,
            in RegulationConfigValues regulationValues,
            MatchStartMode startMode)
        {
            MatchConfig config = ConfigCompiler.Compile(in playerValues, in coreValues, in regulationValues);
            MatchRoster roster = new MatchRoster(
                new PlayerId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
                new PlayerId(Guid.Parse("22222222-2222-2222-2222-222222222222")));
            MatchInitialization initialization = new MatchInitialization(
                new MatchId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
                0xC0FFEEul,
                roster,
                playerZeroPosition,
                playerOnePosition,
                new float2(0f, 1f),
                new float2(0f, -1f),
                startMode);
            MatchSimulation simulation = new MatchSimulation();
            simulation.Initialize(config, arena, in initialization);
            return simulation;
        }

        public static MatchSimulation CreateCountdown(float2 playerZeroPosition, float2 playerOnePosition)
        {
            ArenaBakeData arena = CreateOpenArena();
            PlayerConfigValues playerValues = PlayerConfigValues.GddDefaults;
            CoreConfigValues coreValues = CoreConfigValues.GddDefaults;
            RegulationConfigValues regulationValues = RegulationConfigValues.GddDefaults;
            return Create(
                arena,
                playerZeroPosition,
                playerOnePosition,
                in playerValues,
                in coreValues,
                in regulationValues,
                MatchStartMode.Countdown);
        }

        public static ArenaBakeData CreateOpenArena(float halfExtent = 20f)
        {
            ArenaElementId boundsId = new ArenaElementId(1);
            ArenaElementId coreId = new ArenaElementId(2);
            ArenaElementId cameraId = new ArenaElementId(3);
            return new ArenaBakeData(
                1,
                "simulation-test-arena",
                "tests",
                Array.Empty<ArenaElementDescriptor>(),
                new BakedArenaBounds(boundsId, float2.zero, new float2(halfExtent)),
                Array.Empty<BakedWall>(),
                Array.Empty<BakedConvexObstacle>(),
                Array.Empty<BakedCircle>(),
                Array.Empty<BakedShockGate>(),
                Array.Empty<BakedBoostPad>(),
                new[]
                {
                    new BakedSpawn(new ArenaElementId(4), 0, new float2(0f, 0f), new float2(0f, 1f)),
                    new BakedSpawn(new ArenaElementId(5), 1, new float2(2f, 0f), new float2(0f, -1f)),
                },
                new BakedCoreReset(coreId, float2.zero, 0.5f),
                Array.Empty<BakedBarrierForbiddenVolume>(),
                Array.Empty<BakedNavigationNode>(),
                Array.Empty<BakedNavigationEdge>(),
                new BakedCameraBounds(cameraId, float2.zero, new float2(halfExtent), halfExtent));
        }

        public static InputCommand Movement(float2 move, uint tick = 1u, uint sequence = 1u)
        {
            return new InputCommand(
                new SimulationTick(tick),
                new CommandSequence(sequence),
                move);
        }

        public static InputCommand Command(
            float2 move,
            InputButtons pressedButtons,
            uint tick,
            uint sequence,
            float2 aim = default)
        {
            return new InputCommand(
                new SimulationTick(tick),
                new CommandSequence(sequence),
                move,
                aim,
                pressedButtons,
                InputButtons.None,
                SimulationTick.Zero,
                0u);
        }
    }
}
