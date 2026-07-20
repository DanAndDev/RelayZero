using System;
using System.Globalization;
using RelayZero.Arena;
using RelayZero.Arena.Baking;
using RelayZero.Client.Prediction;
using RelayZero.Foundation;
using RelayZero.Simulation;
using RelayZero.Simulation.Authoring;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RelayZero.Client.Presentation
{
    public enum MovementDemoInputMode : byte
    {
        Manual,
        FixedPlayback,
    }

    public enum MovementDemoFrameCap : short
    {
        Unlimited = -1,
        Fps30 = 30,
        Fps60 = 60,
        Fps120 = 120,
        Fps144 = 144,
    }

    public sealed class SwitchyardMovementDemo : MonoBehaviour, IFixedTickStep
    {
        public const uint FixedPlaybackDurationTicks = 1200u;

        [SerializeField]
        private MatchConfigAsset matchConfigAsset = null!;

        [SerializeField]
        private ArenaBakeAsset arenaBakeAsset = null!;

        [SerializeField]
        private Camera stableCamera = null!;

        [SerializeField]
        private Transform playerZeroView = null!;

        [SerializeField]
        private Transform playerOneView = null!;

        [SerializeField]
        private MovementDemoInputMode inputMode;

        [SerializeField]
        private bool collisionOverlayEnabled;

        [SerializeField]
        private MovementDemoFrameCap frameCap = MovementDemoFrameCap.Fps30;

        [SerializeField]
        [Range(0.05f, 0.95f)]
        private float gamepadAimDeadZone = LocalInputSampler.DefaultGamepadAimDeadZone;

        private readonly TickInputs tickInputs = new TickInputs();
        private readonly MatchEventBuffer eventBuffer = new MatchEventBuffer();

        private MatchSimulation simulation = null!;
        private MatchConfig compiledConfig = null!;
        private ArenaBakeData bakedArena = null!;
        private BakedSpawn zeroSpawn = null!;
        private BakedSpawn oneSpawn = null!;
        private FixedTickRunner fixedTickRunner = null!;
        private LocalCommandBuilder commandBuilder = null!;
        private LocalInputSampler inputSampler = null!;
        private CollisionOverlayRenderer collisionOverlay = null!;
        private GUIStyle overlayStyle = null!;
        private int previousVSyncCount;
        private int previousTargetFrameRate;
        private float smoothedFramesPerSecond;
        private CollisionDiagnostic latchedImpact;
        private float latchedImpactSecondsRemaining;
        private int renderedFrameCollisionCount;
        private int renderedFrameDroppedDiagnosticCount;
        private bool playbackComplete;
        private bool initialized;

        public bool IsInitialized => initialized;

        public MovementDemoInputMode InputMode => inputMode;

        public bool CollisionOverlayEnabled => collisionOverlayEnabled;

        public MovementDemoFrameCap FrameCap => frameCap;

        public MatchSimulation Simulation => simulation;

        public LocalControlScheme ActiveControlScheme => inputSampler == null
            ? LocalControlScheme.KeyboardMouse
            : inputSampler.ActiveScheme;

        public void Configure(
            MatchConfigAsset matchConfig,
            ArenaBakeAsset arenaBake,
            Camera cameraReference,
            Transform zeroView,
            Transform oneView)
        {
            matchConfigAsset = matchConfig;
            arenaBakeAsset = arenaBake;
            stableCamera = cameraReference;
            playerZeroView = zeroView;
            playerOneView = oneView;
        }

        public void SetInputMode(MovementDemoInputMode nextMode)
        {
            if (inputMode == nextMode)
            {
                return;
            }

            inputMode = nextMode;
            if (initialized)
            {
                RestartSimulation();
            }
        }

        public void SetCollisionOverlayEnabled(bool enabled)
        {
            collisionOverlayEnabled = enabled;
            collisionOverlay?.SetVisible(enabled);
        }

        public void SetFrameCap(MovementDemoFrameCap nextFrameCap)
        {
            frameCap = nextFrameCap;
            if (Application.isPlaying && isActiveAndEnabled)
            {
                ApplyFrameCap();
                if (initialized && inputMode == MovementDemoInputMode.FixedPlayback)
                {
                    RestartSimulation();
                }
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            previousVSyncCount = QualitySettings.vSyncCount;
            previousTargetFrameRate = Application.targetFrameRate;
            try
            {
                InitializeDemo();
                ApplyFrameCap();
            }
            catch
            {
                ReleaseDemo();
                QualitySettings.vSyncCount = previousVSyncCount;
                Application.targetFrameRate = previousTargetFrameRate;
                throw;
            }
        }

        private void OnDisable()
        {
            if (!initialized)
            {
                return;
            }

            QualitySettings.vSyncCount = previousVSyncCount;
            Application.targetFrameRate = previousTargetFrameRate;
            ReleaseDemo();
        }

        private void ReleaseDemo()
        {
            inputSampler?.Dispose();
            inputSampler = null!;
            collisionOverlay?.Dispose();
            collisionOverlay = null!;
            simulation = null!;
            fixedTickRunner = null!;
            commandBuilder = null!;
            compiledConfig = null!;
            bakedArena = null!;
            zeroSpawn = null!;
            oneSpawn = null!;
            initialized = false;
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            HandleDevelopmentHotkeys();

            PlayerState localPlayer = simulation.State.GetPlayer(PlayerSlot.Zero);
            if (inputMode == MovementDemoInputMode.FixedPlayback)
            {
                commandBuilder.SetGameplayBlocked(true);
            }
            else
            {
                inputSampler.SampleFrame(commandBuilder, stableCamera, localPlayer.Position, false);
            }

            renderedFrameCollisionCount = 0;
            renderedFrameDroppedDiagnosticCount = 0;
            latchedImpactSecondsRemaining = math.max(
                0f,
                latchedImpactSecondsRemaining - Time.unscaledDeltaTime);
            fixedTickRunner.Advance(Time.unscaledDeltaTime, this);

            if (Time.unscaledDeltaTime > 0f)
            {
                float instantaneous = 1f / Time.unscaledDeltaTime;
                smoothedFramesPerSecond = smoothedFramesPerSecond <= 0f
                    ? instantaneous
                    : math.lerp(smoothedFramesPerSecond, instantaneous, 0.1f);
            }

            collisionOverlay.SetVisible(collisionOverlayEnabled);
            if (collisionOverlayEnabled)
            {
                collisionOverlay.Render(
                    latchedImpactSecondsRemaining > 0f,
                    in latchedImpact,
                    simulation.Config.Player.RadiusMeters);
            }
        }

        private void LateUpdate()
        {
            if (!initialized)
            {
                return;
            }

            ApplyView(playerZeroView, simulation.State.GetPlayer(PlayerSlot.Zero));
            ApplyView(playerOneView, simulation.State.GetPlayer(PlayerSlot.One));
        }

        public void Step()
        {
            if (inputMode == MovementDemoInputMode.FixedPlayback && playbackComplete)
            {
                return;
            }

            SimulationTick clientTick = simulation.State.Tick.Next();
            InputCommand sampledCommand = commandBuilder.Build(clientTick);
            InputCommand localCommand = inputMode == MovementDemoInputMode.FixedPlayback
                ? CreateFixedPlaybackCommand(in sampledCommand)
                : sampledCommand;
            InputCommand remoteCommand = new InputCommand(
                clientTick,
                new CommandSequence(clientTick.Value),
                float2.zero);

            tickInputs.Set(PlayerSlot.Zero, in localCommand);
            tickInputs.Set(PlayerSlot.One, in remoteCommand);
            simulation.Step(tickInputs, eventBuffer);
            CaptureCollisionDiagnostics();

            if (inputMode == MovementDemoInputMode.FixedPlayback &&
                simulation.State.Tick.Value >= FixedPlaybackDurationTicks)
            {
                playbackComplete = true;
            }
        }

        private void OnGUI()
        {
            if (!initialized)
            {
                return;
            }

            PlayerState player = simulation.State.GetPlayer(PlayerSlot.Zero);
            if (overlayStyle == null)
            {
                overlayStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = 16,
                    normal = { textColor = Color.white },
                    padding = new RectOffset(10, 10, 8, 8),
                };
            }

            string text =
                "Switchyard Movement\n" +
                "Input: " + inputMode + (playbackComplete ? " (complete)" : string.Empty) +
                "  |  Overlay: " + (collisionOverlayEnabled ? "On" : "Off") + "\n" +
                "F7 restart  |  F8 cap: " + FormatFrameCap(frameCap) +
                "  |  F9 input  |  F10 overlay\n" +
                "Tick: " + simulation.State.Tick.Value + " @ 60 Hz  |  Render: " +
                smoothedFramesPerSecond.ToString("0.0", CultureInfo.InvariantCulture) + " FPS\n" +
                "Device: " + inputSampler.ActiveScheme + "  |  Position: " +
                player.Position.x.ToString("0.000", CultureInfo.InvariantCulture) + ", " +
                player.Position.y.ToString("0.000", CultureInfo.InvariantCulture) + "\n" +
                "Checksum: " + simulation.ComputeChecksum().ToString("X16", CultureInfo.InvariantCulture) +
                "  |  Frame collisions: " + renderedFrameCollisionCount +
                " (dropped " + renderedFrameDroppedDiagnosticCount + ")";
            GUI.Label(new Rect(16f, 16f, 720f, 144f), text, overlayStyle);
        }

        private void InitializeDemo()
        {
            if (matchConfigAsset == null || arenaBakeAsset == null || stableCamera == null ||
                playerZeroView == null || playerOneView == null)
            {
                throw new InvalidOperationException(
                    "Switchyard movement requires configuration, arena bake, stable camera, and two view transforms.");
            }

            compiledConfig = matchConfigAsset.Compile();
            bakedArena = arenaBakeAsset.CreateRuntimeData();
            zeroSpawn = FindSpawn(bakedArena, PlayerSlot.Zero);
            oneSpawn = FindSpawn(bakedArena, PlayerSlot.One);
            inputSampler = new LocalInputSampler(gamepadAimDeadZone);
            inputSampler.Enable();
            collisionOverlay = new CollisionOverlayRenderer(transform);
            collisionOverlay.SetVisible(collisionOverlayEnabled);
            stableCamera.orthographic = true;
            stableCamera.orthographicSize = bakedArena.CameraBounds.OrthographicSize;
            Vector3 cameraPosition = stableCamera.transform.position;
            float2 cameraCenter = bakedArena.CameraBounds.Center;
            stableCamera.transform.position = new Vector3(cameraCenter.x, cameraPosition.y, cameraCenter.y);
            RestartSimulation();
            initialized = true;
        }

        private void HandleDevelopmentHotkeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f7Key.wasPressedThisFrame)
            {
                RestartSimulation();
            }

            if (keyboard.f8Key.wasPressedThisFrame)
            {
                SetFrameCap(NextFrameCap(frameCap));
            }

            if (keyboard.f9Key.wasPressedThisFrame)
            {
                SetInputMode(inputMode == MovementDemoInputMode.Manual
                    ? MovementDemoInputMode.FixedPlayback
                    : MovementDemoInputMode.Manual);
            }

            if (keyboard.f10Key.wasPressedThisFrame)
            {
                SetCollisionOverlayEnabled(!collisionOverlayEnabled);
            }
        }

        private void ApplyFrameCap()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = (int)frameCap;
        }

        private static InputCommand CreateFixedPlaybackCommand(in InputCommand sampledCommand)
        {
            uint phase = (sampledCommand.ClientTick.Value - 1u) % 300u;
            float2 move = phase < 90u
                ? math.normalize(new float2(0.7f, -1f))
                : phase < 150u
                    ? new float2(1f, 0f)
                    : phase < 240u
                        ? math.normalize(new float2(-0.7f, 1f))
                        : new float2(-1f, 0f);
            return new InputCommand(
                sampledCommand.ClientTick,
                sampledCommand.Sequence,
                move,
                move,
                InputButtons.None,
                InputButtons.None,
                sampledCommand.LastReceivedServerTick,
                sampledCommand.LastReceivedSnapshotSequence);
        }

        private void RestartSimulation()
        {
            MatchRoster roster = new MatchRoster(
                new PlayerId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
                new PlayerId(Guid.Parse("22222222-2222-2222-2222-222222222222")));
            MatchInitialization initialization = new MatchInitialization(
                new MatchId(Guid.Parse("10000000-0000-0000-0000-000000000001")),
                0xC0FFEEul,
                roster,
                zeroSpawn.Position,
                oneSpawn.Position,
                zeroSpawn.FacingDirection,
                oneSpawn.FacingDirection);

            MatchSimulation restartedSimulation = new MatchSimulation();
            restartedSimulation.Initialize(compiledConfig, bakedArena, in initialization);
            simulation = restartedSimulation;
            fixedTickRunner = new FixedTickRunner();
            commandBuilder = new LocalCommandBuilder(new CommandSequence(1u));
            commandBuilder.SetGameplayBlocked(inputMode == MovementDemoInputMode.FixedPlayback);
            latchedImpact = default;
            latchedImpactSecondsRemaining = 0f;
            renderedFrameCollisionCount = 0;
            renderedFrameDroppedDiagnosticCount = 0;
            playbackComplete = false;
            smoothedFramesPerSecond = 0f;
            ApplyView(playerZeroView, simulation.State.GetPlayer(PlayerSlot.Zero));
            ApplyView(playerOneView, simulation.State.GetPlayer(PlayerSlot.One));
        }

        private void CaptureCollisionDiagnostics()
        {
            CollisionDiagnosticBuffer diagnostics = simulation.CollisionDiagnostics;
            renderedFrameCollisionCount += diagnostics.Count;
            renderedFrameDroppedDiagnosticCount += diagnostics.DroppedCount;
            for (int index = 0; index < diagnostics.Count; index++)
            {
                CollisionDiagnostic candidate = diagnostics[index];
                if (candidate.Kind == CollisionDiagnosticKind.StaticImpact &&
                    candidate.Slot == PlayerSlot.Zero)
                {
                    latchedImpact = candidate;
                    latchedImpactSecondsRemaining = 0.25f;
                }
            }
        }

        private static BakedSpawn FindSpawn(ArenaBakeData arena, PlayerSlot slot)
        {
            for (int index = 0; index < arena.Spawns.Count; index++)
            {
                BakedSpawn spawn = arena.Spawns[index];
                if (spawn.PlayerSlot == slot.Index)
                {
                    return spawn;
                }
            }

            throw new InvalidOperationException("Arena bake is missing player slot " + slot.Index + " spawn data.");
        }

        private static void ApplyView(Transform view, PlayerState player)
        {
            Vector3 position = view.position;
            view.position = new Vector3(player.Position.x, position.y, player.Position.y);
            float2 facing = player.FacingDirection;
            view.rotation = Quaternion.LookRotation(new Vector3(facing.x, 0f, facing.y), Vector3.up);
        }

        private static MovementDemoFrameCap NextFrameCap(MovementDemoFrameCap current)
        {
            switch (current)
            {
                case MovementDemoFrameCap.Fps30: return MovementDemoFrameCap.Fps60;
                case MovementDemoFrameCap.Fps60: return MovementDemoFrameCap.Fps120;
                case MovementDemoFrameCap.Fps120: return MovementDemoFrameCap.Fps144;
                case MovementDemoFrameCap.Fps144: return MovementDemoFrameCap.Unlimited;
                default: return MovementDemoFrameCap.Fps30;
            }
        }

        private static string FormatFrameCap(MovementDemoFrameCap value)
        {
            return value == MovementDemoFrameCap.Unlimited
                ? "Unlimited"
                : ((int)value).ToString(CultureInfo.InvariantCulture);
        }

        private sealed class CollisionOverlayRenderer : IDisposable
        {
            private const int CirclePointCount = 33;

            private readonly GameObject root;
            private readonly Material material;
            private readonly LineRenderer circle;
            private readonly LineRenderer sweep;
            private readonly LineRenderer normal;
            private readonly LineRenderer remaining;

            public CollisionOverlayRenderer(Transform parent)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    throw new InvalidOperationException("Sprites/Default shader is required for the collision overlay.");
                }

                root = new GameObject("Movement Collision Overlay");
                root.transform.SetParent(parent, false);
                material = new Material(shader) { hideFlags = HideFlags.DontSave };
                circle = CreateLine("Swept Circle", Color.cyan, CirclePointCount);
                sweep = CreateLine("Requested Sweep", Color.cyan, 2);
                normal = CreateLine("Impact Normal", Color.yellow, 2);
                remaining = CreateLine("Remaining Slide", Color.magenta, 2);
            }

            public void SetVisible(bool visible)
            {
                root.SetActive(visible);
            }

            public void Render(bool hasImpact, in CollisionDiagnostic impact, float radius)
            {
                circle.enabled = hasImpact;
                sweep.enabled = hasImpact;
                normal.enabled = hasImpact;
                remaining.enabled = hasImpact;
                if (!hasImpact)
                {
                    return;
                }

                const float height = 0.18f;
                for (int index = 0; index < CirclePointCount; index++)
                {
                    float angle = index * math.PI * 2f / (CirclePointCount - 1);
                    float2 point = impact.SweepStart + new float2(math.cos(angle), math.sin(angle)) * radius;
                    circle.SetPosition(index, new Vector3(point.x, height, point.y));
                }

                SetSegment(
                    sweep,
                    impact.SweepStart,
                    impact.SweepStart + impact.RequestedDisplacement,
                    height);
                SetSegment(
                    normal,
                    impact.ContactPosition,
                    impact.ContactPosition + impact.Normal,
                    height + 0.02f);
                SetSegment(
                    remaining,
                    impact.ContactPosition,
                    impact.ContactPosition + impact.RemainingDisplacement,
                    height + 0.04f);
            }

            public void Dispose()
            {
                if (root != null)
                {
                    UnityEngine.Object.Destroy(root);
                }

                if (material != null)
                {
                    UnityEngine.Object.Destroy(material);
                }
            }

            private LineRenderer CreateLine(string name, Color color, int positionCount)
            {
                GameObject lineObject = new GameObject(name);
                lineObject.transform.SetParent(root.transform, false);
                LineRenderer line = lineObject.AddComponent<LineRenderer>();
                line.sharedMaterial = material;
                line.useWorldSpace = true;
                line.startColor = color;
                line.endColor = color;
                line.startWidth = 0.045f;
                line.endWidth = 0.045f;
                line.positionCount = positionCount;
                line.numCapVertices = 2;
                return line;
            }

            private static void SetSegment(LineRenderer line, float2 start, float2 end, float height)
            {
                line.SetPosition(0, new Vector3(start.x, height, start.y));
                line.SetPosition(1, new Vector3(end.x, height, end.y));
            }
        }
    }
}
