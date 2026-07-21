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
        ScriptedCoreLifecycle,
        ScriptedTargetScore,
        ScriptedTimerExpiry,
    }

    public enum TickBatchMode : byte
    {
        Realtime = 0,
        TenTicksPerFrame = 10,
        SixtyTicksPerFrame = 60,
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
        private Transform coreView = null!;

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
        private readonly MatchHudRenderer matchHudRenderer = new MatchHudRenderer();

        private MatchSimulation simulation = null!;
        private MatchConfig compiledConfig = null!;
        private ArenaBakeData bakedArena = null!;
        private BakedSpawn zeroSpawn = null!;
        private BakedSpawn oneSpawn = null!;
        private FixedTickRunner fixedTickRunner = null!;
        private LocalCommandBuilder commandBuilder = null!;
        private LocalInputSampler inputSampler = null!;
        private CollisionOverlayRenderer collisionOverlay = null!;
        private CoreLifecycleRenderer coreLifecycleRenderer = null!;
        private GUIStyle overlayStyle = null!;
        private int previousVSyncCount;
        private int previousTargetFrameRate;
        private float smoothedFramesPerSecond;
        private CollisionDiagnostic latchedImpact;
        private float latchedImpactSecondsRemaining;
        private int renderedFrameCollisionCount;
        private int renderedFrameDroppedDiagnosticCount;
        private bool playbackComplete;
        private bool scriptedManualDropIssued;
        private bool scriptedForcedDropIssued;
        private bool scriptedLifecycleComplete;
        private bool scriptedContestedPickupComplete;
        private bool scriptedManualDropComplete;
        private bool scriptedOpponentPickupComplete;
        private bool scriptedForcedDropComplete;
        private bool scriptedRegulationPickupComplete;
        private bool scriptedTimerDropIssued;
        private bool scriptedTargetScoreComplete;
        private bool scriptedTimerExpiryComplete;
        private TickBatchMode tickBatchMode;
        private float goPresentationSecondsRemaining;
        private float2 scriptedResetTarget;
        private CoreDiagnostic latchedCoreDiagnostic;
        private float latchedCoreDiagnosticSecondsRemaining;
        private bool initialized;

        public bool IsInitialized => initialized;

        public MovementDemoInputMode InputMode => inputMode;

        public bool CollisionOverlayEnabled => collisionOverlayEnabled;

        public MovementDemoFrameCap FrameCap => frameCap;

        public bool ScriptedLifecycleComplete => scriptedLifecycleComplete;

        public bool ScriptedContestedPickupComplete => scriptedContestedPickupComplete;

        public bool ScriptedManualDropComplete => scriptedManualDropComplete;

        public bool ScriptedOpponentPickupComplete => scriptedOpponentPickupComplete;

        public bool ScriptedForcedDropComplete => scriptedForcedDropComplete;

        public bool ScriptedTargetScoreComplete => scriptedTargetScoreComplete;

        public bool ScriptedTimerExpiryComplete => scriptedTimerExpiryComplete;

        public TickBatchMode TickBatch => tickBatchMode;

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

        public void ConfigureCoreView(Transform view)
        {
            coreView = view;
        }

        public void SetInputMode(MovementDemoInputMode nextMode)
        {
            if (inputMode == nextMode)
            {
                return;
            }

            inputMode = nextMode;
            tickBatchMode = TickBatchMode.Realtime;
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

        public void SetTickBatch(TickBatchMode nextBatch)
        {
            tickBatchMode = nextBatch;
            if (initialized)
            {
                fixedTickRunner = new FixedTickRunner();
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
            coreLifecycleRenderer?.Dispose();
            coreLifecycleRenderer = null!;
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
            if (inputMode != MovementDemoInputMode.Manual)
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
            latchedCoreDiagnosticSecondsRemaining = math.max(
                0f,
                latchedCoreDiagnosticSecondsRemaining - Time.unscaledDeltaTime);
            goPresentationSecondsRemaining = math.max(
                0f,
                goPresentationSecondsRemaining - Time.unscaledDeltaTime);
            if (tickBatchMode == TickBatchMode.Realtime)
            {
                fixedTickRunner.Advance(Time.unscaledDeltaTime, this);
            }
            else
            {
                int ticksThisFrame = (int)tickBatchMode;
                for (int index = 0; index < ticksThisFrame; index++)
                {
                    Step();
                }
            }

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


            coreLifecycleRenderer.Render(
                simulation.State.Core,
                simulation.State.GetPlayer(PlayerSlot.Zero),
                simulation.State.GetPlayer(PlayerSlot.One),
                simulation.Config.Core,
                simulation.Config.Regulation,
                simulation.State.Tick,
                bakedArena.CoreReset.Position);
        }

        private void LateUpdate()
        {
            if (!initialized)
            {
                return;
            }

            ApplyView(playerZeroView, simulation.State.GetPlayer(PlayerSlot.Zero));
            ApplyView(playerOneView, simulation.State.GetPlayer(PlayerSlot.One));
            ApplyCoreView();
        }

        public void Step()
        {
            if (inputMode == MovementDemoInputMode.FixedPlayback && playbackComplete)
            {
                return;
            }

            SimulationTick clientTick = simulation.State.Tick.Next();
            InputCommand sampledCommand = commandBuilder.Build(clientTick);
            InputCommand localCommand;
            InputCommand remoteCommand;
            if (inputMode == MovementDemoInputMode.FixedPlayback)
            {
                localCommand = CreateFixedPlaybackCommand(in sampledCommand);
                remoteCommand = new InputCommand(
                    clientTick,
                    new CommandSequence(clientTick.Value),
                    float2.zero);
            }
            else if (inputMode == MovementDemoInputMode.ScriptedCoreLifecycle)
            {
                TryQueueScriptedForcedDrop();
                localCommand = CreateScriptedCoreCommand(PlayerSlot.Zero, clientTick);
                remoteCommand = CreateScriptedCoreCommand(PlayerSlot.One, clientTick);
            }
            else if (inputMode == MovementDemoInputMode.ScriptedTargetScore ||
                     inputMode == MovementDemoInputMode.ScriptedTimerExpiry)
            {
                localCommand = CreateScriptedRegulationCommand(PlayerSlot.Zero, clientTick);
                remoteCommand = CreateScriptedRegulationCommand(PlayerSlot.One, clientTick);
            }
            else
            {
                localCommand = sampledCommand;
                remoteCommand = new InputCommand(
                    clientTick,
                    new CommandSequence(clientTick.Value),
                    float2.zero);
            }

            tickInputs.Set(PlayerSlot.Zero, in localCommand);
            tickInputs.Set(PlayerSlot.One, in remoteCommand);
            simulation.Step(tickInputs, eventBuffer);
            CaptureDiagnostics();

            if (inputMode == MovementDemoInputMode.FixedPlayback &&
                simulation.State.Tick.Value >= FixedPlaybackDurationTicks)
            {
                playbackComplete = true;
            }


            for (int index = 0; index < eventBuffer.Count; index++)
            {
                MatchEvent matchEvent = eventBuffer[index];
                if (matchEvent.Type == MatchEventType.CorePickedUp && matchEvent.HasPlayerSlot)
                {
                    if ((inputMode == MovementDemoInputMode.ScriptedTargetScore ||
                         inputMode == MovementDemoInputMode.ScriptedTimerExpiry) &&
                        matchEvent.PlayerSlot == PlayerSlot.Zero)
                    {
                        scriptedRegulationPickupComplete = true;
                    }
                    else if (inputMode == MovementDemoInputMode.ScriptedCoreLifecycle &&
                             !scriptedManualDropComplete && matchEvent.PlayerSlot == PlayerSlot.Zero)
                    {
                        scriptedContestedPickupComplete = true;
                    }
                    else if (scriptedManualDropComplete && matchEvent.PlayerSlot == PlayerSlot.One)
                    {
                        scriptedOpponentPickupComplete = true;
                    }
                }
                else if (matchEvent.Type == MatchEventType.CoreDropped && matchEvent.HasPlayerSlot)
                {
                    if (matchEvent.Value == (int)CoreDropReason.Manual &&
                        matchEvent.PlayerSlot == PlayerSlot.Zero)
                    {
                        scriptedManualDropComplete = true;
                    }
                    else if (matchEvent.Value == (int)CoreDropReason.Recovery &&
                             matchEvent.PlayerSlot == PlayerSlot.One &&
                             scriptedOpponentPickupComplete)
                    {
                        scriptedForcedDropComplete = true;
                    }
                }
                else if (matchEvent.Type == MatchEventType.CoreResetCompleted)
                {
                    scriptedLifecycleComplete = scriptedContestedPickupComplete &&
                        scriptedManualDropComplete &&
                        scriptedOpponentPickupComplete &&
                        scriptedForcedDropComplete;
                }
                else if (matchEvent.Type == MatchEventType.MatchPhaseChanged &&
                         matchEvent.Value == (int)MatchPhase.Regulation)
                {
                    goPresentationSecondsRemaining = 0.75f;
                }
            }

            MatchClockState clock = simulation.State.Clock;
            if (clock.Phase == MatchPhase.Results && clock.HasResult)
            {
                scriptedTargetScoreComplete = inputMode == MovementDemoInputMode.ScriptedTargetScore &&
                    clock.Result.Reason == MatchResultReason.TargetScore;
                scriptedTimerExpiryComplete = inputMode == MovementDemoInputMode.ScriptedTimerExpiry &&
                    clock.Result.Reason == MatchResultReason.RegulationTimer;
            }
        }

        private void OnGUI()
        {
            if (!initialized)
            {
                return;
            }

            PlayerState player = simulation.State.GetPlayer(PlayerSlot.Zero);
            CoreRuntimeState core = simulation.State.Core;
            matchHudRenderer.Draw(
                simulation.State,
                simulation.Config.Regulation,
                inputMode.ToString(),
                FormatTickBatch(tickBatchMode),
                goPresentationSecondsRemaining > 0f);
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
                "Switchyard Movement + Relay Core\n" +
                "Input: " + inputMode +
                ((playbackComplete || scriptedLifecycleComplete || scriptedTargetScoreComplete ||
                  scriptedTimerExpiryComplete) ? " (complete)" : string.Empty) +
                "  |  Overlay: " + (collisionOverlayEnabled ? "On" : "Off") + "\n" +
                "F2 target  |  F3 timer  |  F5 accelerate: " + FormatTickBatch(tickBatchMode) +
                "  |  F6 forced drop  |  F7 restart\n" +
                "F8 cap: " + FormatFrameCap(frameCap) +
                "  |  F9 input mode  |  F10 collision\n" +
                "Tick: " + simulation.State.Tick.Value + " @ 60 Hz  |  Render: " +
                smoothedFramesPerSecond.ToString("0.0", CultureInfo.InvariantCulture) + " FPS\n" +
                "Device: " + inputSampler.ActiveScheme + "  |  Position: " +
                player.Position.x.ToString("0.000", CultureInfo.InvariantCulture) + ", " +
                player.Position.y.ToString("0.000", CultureInfo.InvariantCulture) + "\n" +
                "Core: " + core.Mode + "  |  Owner: " + FormatOwner(in core) +
                "  |  Speed: " + math.length(core.Velocity).ToString("0.00", CultureInfo.InvariantCulture) +
                " m/s  |  Rest: " + core.RestTickCount + "/" + simulation.Config.Core.RestQualificationTicks + "\n" +
                "Phase: " + simulation.State.Clock.Phase + "  |  Regulation ticks: " +
                simulation.State.Clock.RegulationTicksRemaining + "  |  Score milli P0/P1: " +
                simulation.State.Score.PlayerZeroMilliPoints + "/" +
                simulation.State.Score.PlayerOneMilliPoints + "  |  Grace: " +
                core.GetPossessionGraceRemainingTicks(simulation.State.Tick) + "\n" +
                "Pickup locks P0/P1: " +
                core.GetPickupLockRemainingTicks(PlayerSlot.Zero, simulation.State.Tick) + "/" +
                core.GetPickupLockRemainingTicks(PlayerSlot.One, simulation.State.Tick) +
                " ticks  |  Invalid: " + core.InvalidTickCount + "/" +
                simulation.Config.Core.InvalidResetDelayTicks + "\n" +
                "Last core diagnostic: " +
                (latchedCoreDiagnosticSecondsRemaining > 0f
                    ? latchedCoreDiagnostic.Kind + " @ " + latchedCoreDiagnostic.Tick.Value
                    : "None") + "\n" +
                "Checksum: " + simulation.ComputeChecksum().ToString("X16", CultureInfo.InvariantCulture) +
                "  |  Frame collisions: " + renderedFrameCollisionCount +
                " (dropped " + renderedFrameDroppedDiagnosticCount + ")";
            GUI.Label(new Rect(16f, 88f, 900f, 290f), text, overlayStyle);
        }

        private void InitializeDemo()
        {
            if (matchConfigAsset == null || arenaBakeAsset == null || stableCamera == null ||
                playerZeroView == null || playerOneView == null || coreView == null)
            {
                throw new InvalidOperationException(
                    "Switchyard gameplay demo requires configuration, arena bake, stable camera, two player views, and a core view.");
            }

            compiledConfig = matchConfigAsset.Compile();
            bakedArena = arenaBakeAsset.CreateRuntimeData();
            zeroSpawn = FindSpawn(bakedArena, PlayerSlot.Zero);
            oneSpawn = FindSpawn(bakedArena, PlayerSlot.One);
            inputSampler = new LocalInputSampler(gamepadAimDeadZone);
            inputSampler.Enable();
            collisionOverlay = new CollisionOverlayRenderer(transform);
            collisionOverlay.SetVisible(collisionOverlayEnabled);
            coreLifecycleRenderer = new CoreLifecycleRenderer(transform);
            scriptedResetTarget = FindScriptedResetTarget(bakedArena);
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

            if (keyboard.f2Key.wasPressedThisFrame)
            {
                SetInputMode(MovementDemoInputMode.ScriptedTargetScore);
            }

            if (keyboard.f3Key.wasPressedThisFrame)
            {
                SetInputMode(MovementDemoInputMode.ScriptedTimerExpiry);
            }

            if (keyboard.f5Key.wasPressedThisFrame)
            {
                SetTickBatch(NextTickBatch(tickBatchMode));
            }

            if (keyboard.f6Key.wasPressedThisFrame)
            {
                TryQueueManualDiagnosticForcedDrop();
            }

            if (keyboard.f8Key.wasPressedThisFrame)
            {
                SetFrameCap(NextFrameCap(frameCap));
            }

            if (keyboard.f9Key.wasPressedThisFrame)
            {
                SetInputMode(NextInputMode(inputMode));
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

        private InputCommand CreateScriptedCoreCommand(PlayerSlot slot, SimulationTick clientTick)
        {
            CoreRuntimeState core = simulation.State.Core;
            PlayerState player = simulation.State.GetPlayer(slot);
            float2 move = float2.zero;
            float2 aim = player.FacingDirection;
            InputButtons pressed = InputButtons.None;

            if (clientTick.Value == 1u)
            {
                // Equal range and command time deliberately exercise the stable PlayerSlot tie-break.
                pressed = InputButtons.Interact;
                aim = slot == PlayerSlot.Zero ? new float2(-1f, 0f) : new float2(1f, 0f);
            }
            else if (slot == PlayerSlot.Zero)
            {
                if (core.IsCarriedBy(slot))
                {
                    if (clientTick.Value <= 16u)
                    {
                        move = new float2(-1f, 0f);
                        aim = move;
                    }
                    else
                    {
                        aim = new float2(-1f, 0f);
                    }

                    if (clientTick.Value >= 60u && !scriptedManualDropIssued)
                    {
                        pressed = InputButtons.Interact;
                        scriptedManualDropIssued = true;
                    }
                }
                else if (scriptedManualDropIssued && clientTick.Value <= 120u)
                {
                    move = new float2(0f, -1f);
                    aim = move;
                }
            }
            else if (core.IsCarriedBy(slot))
            {
                float2 toTarget = scriptedResetTarget - player.Position;
                if (math.lengthsq(toTarget) > 0.04f)
                {
                    move = math.normalizesafe(toTarget);
                    aim = move;
                }
            }
            else if ((core.Mode == CoreMode.Loose || core.Mode == CoreMode.DropLock) &&
                     scriptedManualDropIssued &&
                     !scriptedForcedDropIssued)
            {
                float2 toCore = core.Position - player.Position;
                if (math.lengthsq(toCore) > 0.01f)
                {
                    move = math.normalizesafe(toCore);
                    aim = move;
                }

                if (core.IsPickupEligible(slot, clientTick) &&
                    math.lengthsq(toCore) <= simulation.Config.Core.InteractionRangeSquared)
                {
                    pressed = InputButtons.Interact;
                }
            }

            return new InputCommand(
                clientTick,
                new CommandSequence(clientTick.Value),
                move,
                aim,
                pressed,
                InputButtons.None,
                SimulationTick.Zero,
                0u);
        }

        private InputCommand CreateScriptedRegulationCommand(PlayerSlot slot, SimulationTick clientTick)
        {
            MatchClockState clock = simulation.State.Clock;
            CoreRuntimeState core = simulation.State.Core;
            PlayerState player = simulation.State.GetPlayer(slot);
            InputButtons pressed = InputButtons.None;
            float2 aim = slot == PlayerSlot.Zero ? new float2(1f, 0f) : new float2(-1f, 0f);
            bool goTick = clock.Phase == MatchPhase.Countdown &&
                !clock.PhaseEndTick.IsNewerThan(clientTick);
            bool regulationAcceptsInput = clock.Phase == MatchPhase.Regulation || goTick;
            if (regulationAcceptsInput && !scriptedRegulationPickupComplete &&
                (core.Mode == CoreMode.Locked || core.Mode == CoreMode.Loose))
            {
                pressed = InputButtons.Interact;
            }
            else if (inputMode == MovementDemoInputMode.ScriptedTimerExpiry &&
                     slot == PlayerSlot.Zero &&
                     core.IsCarriedBy(PlayerSlot.Zero) &&
                     simulation.State.Score.PlayerZeroMilliPoints >= 5000 &&
                     !scriptedTimerDropIssued)
            {
                pressed = InputButtons.Interact;
                scriptedTimerDropIssued = true;
            }

            return new InputCommand(
                clientTick,
                new CommandSequence(clientTick.Value),
                float2.zero,
                aim,
                pressed,
                InputButtons.None,
                SimulationTick.Zero,
                0u);
        }

        private void TryQueueScriptedForcedDrop()
        {
            if (scriptedForcedDropIssued || !scriptedOpponentPickupComplete)
            {
                return;
            }

            CoreRuntimeState core = simulation.State.Core;
            if (!core.IsCarriedBy(PlayerSlot.One))
            {
                return;
            }

            PlayerState carrier = simulation.State.GetPlayer(PlayerSlot.One);
            if (math.distancesq(carrier.Position, scriptedResetTarget) > 0.04f)
            {
                return;
            }

            CoreForcedDropRequest request = new CoreForcedDropRequest(
                CoreDropReason.Recovery,
                float2.zero,
                simulation.State.Tick.Next().Value);
            scriptedForcedDropIssued = simulation.TryQueueForcedCoreDrop(in request);
        }

        private void TryQueueManualDiagnosticForcedDrop()
        {
            if (inputMode != MovementDemoInputMode.Manual)
            {
                return;
            }

            CoreRuntimeState core = simulation.State.Core;
            if (core.Mode != CoreMode.Carried || !core.HasOwner)
            {
                return;
            }

            PlayerState carrier = simulation.State.GetPlayer(core.Owner);
            CoreForcedDropRequest request = new CoreForcedDropRequest(
                CoreDropReason.Recovery,
                carrier.FacingDirection * 8f,
                simulation.State.Tick.Next().Value);
            simulation.TryQueueForcedCoreDrop(in request);
        }

        private void RestartSimulation()
        {
            MatchRoster roster = new MatchRoster(
                new PlayerId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
                new PlayerId(Guid.Parse("22222222-2222-2222-2222-222222222222")));
            bool lifecycleScript = inputMode == MovementDemoInputMode.ScriptedCoreLifecycle;
            bool regulationScript = inputMode == MovementDemoInputMode.ScriptedTargetScore ||
                inputMode == MovementDemoInputMode.ScriptedTimerExpiry;
            bool scripted = lifecycleScript || regulationScript;
            MatchStartMode startMode = lifecycleScript || inputMode == MovementDemoInputMode.FixedPlayback
                ? MatchStartMode.Regulation
                : MatchStartMode.Countdown;
            MatchInitialization initialization = new MatchInitialization(
                new MatchId(Guid.Parse("10000000-0000-0000-0000-000000000001")),
                0xC0FFEEul,
                roster,
                scripted ? new float2(-0.8f, 0f) : zeroSpawn.Position,
                scripted ? new float2(0.8f, 0f) : oneSpawn.Position,
                lifecycleScript
                    ? new float2(-1f, 0f)
                    : regulationScript ? new float2(1f, 0f) : zeroSpawn.FacingDirection,
                lifecycleScript
                    ? new float2(1f, 0f)
                    : regulationScript ? new float2(-1f, 0f) : oneSpawn.FacingDirection,
                startMode);

            MatchSimulation restartedSimulation = new MatchSimulation();
            restartedSimulation.Initialize(compiledConfig, bakedArena, in initialization);
            simulation = restartedSimulation;
            fixedTickRunner = new FixedTickRunner();
            commandBuilder = new LocalCommandBuilder(new CommandSequence(1u));
            commandBuilder.SetGameplayBlocked(inputMode != MovementDemoInputMode.Manual);
            latchedImpact = default;
            latchedImpactSecondsRemaining = 0f;
            renderedFrameCollisionCount = 0;
            renderedFrameDroppedDiagnosticCount = 0;
            playbackComplete = false;
            scriptedManualDropIssued = false;
            scriptedForcedDropIssued = false;
            scriptedLifecycleComplete = false;
            scriptedContestedPickupComplete = false;
            scriptedManualDropComplete = false;
            scriptedOpponentPickupComplete = false;
            scriptedForcedDropComplete = false;
            scriptedRegulationPickupComplete = false;
            scriptedTimerDropIssued = false;
            scriptedTargetScoreComplete = false;
            scriptedTimerExpiryComplete = false;
            goPresentationSecondsRemaining = 0f;
            latchedCoreDiagnostic = default;
            latchedCoreDiagnosticSecondsRemaining = 0f;
            smoothedFramesPerSecond = 0f;
            ApplyView(playerZeroView, simulation.State.GetPlayer(PlayerSlot.Zero));
            ApplyView(playerOneView, simulation.State.GetPlayer(PlayerSlot.One));
            ApplyCoreView();
        }

        private void CaptureDiagnostics()
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


            CoreDiagnosticBuffer coreDiagnostics = simulation.CoreDiagnostics;
            for (int index = 0; index < coreDiagnostics.Count; index++)
            {
                CoreDiagnostic candidate = coreDiagnostics[index];
                if (candidate.Kind == CoreDiagnosticKind.None)
                {
                    continue;
                }

                latchedCoreDiagnostic = candidate;
                latchedCoreDiagnosticSecondsRemaining = candidate.Kind == CoreDiagnosticKind.InvalidOrUnreachable
                    ? 0.2f
                    : 1.25f;
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

        private static float2 FindScriptedResetTarget(ArenaBakeData arena)
        {
            bool found = false;
            float2 target = float2.zero;
            for (int index = 0; index < arena.Circles.Count; index++)
            {
                BakedCircle circle = arena.Circles[index];
                if (circle.Kind != BakedElementKind.Terminal)
                {
                    continue;
                }

                if (!found || circle.Center.y > target.y)
                {
                    target = circle.Center;
                    found = true;
                }
            }

            if (!found)
            {
                throw new InvalidOperationException("The scripted core lifecycle requires a baked terminal target.");
            }

            return target;
        }

        private static void ApplyView(Transform view, PlayerState player)
        {
            Vector3 position = view.position;
            view.position = new Vector3(player.Position.x, position.y, player.Position.y);
            float2 facing = player.FacingDirection;
            view.rotation = Quaternion.LookRotation(new Vector3(facing.x, 0f, facing.y), Vector3.up);
        }

        private void ApplyCoreView()
        {
            CoreRuntimeState core = simulation.State.Core;
            bool visible = core.Mode != CoreMode.Resetting;
            if (coreView.gameObject.activeSelf != visible)
            {
                coreView.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            float2 planarPosition = core.Position;
            float height = 0.8f;
            if (core.Mode == CoreMode.Carried && core.HasOwner)
            {
                PlayerState carrier = simulation.State.GetPlayer(core.Owner);
                planarPosition = carrier.Position - carrier.FacingDirection * 0.35f;
                height = 1.25f;
            }

            coreView.position = new Vector3(planarPosition.x, height, planarPosition.y);
            float spinDegrees = core.Mode == CoreMode.Carried ? 55f : 110f;
            coreView.Rotate(Vector3.up, spinDegrees * Time.unscaledDeltaTime, Space.World);
        }

        private static MovementDemoInputMode NextInputMode(MovementDemoInputMode current)
        {
            switch (current)
            {
                case MovementDemoInputMode.Manual: return MovementDemoInputMode.ScriptedCoreLifecycle;
                case MovementDemoInputMode.ScriptedCoreLifecycle: return MovementDemoInputMode.FixedPlayback;
                default: return MovementDemoInputMode.Manual;
            }
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

        private static TickBatchMode NextTickBatch(TickBatchMode current)
        {
            switch (current)
            {
                case TickBatchMode.Realtime: return TickBatchMode.TenTicksPerFrame;
                case TickBatchMode.TenTicksPerFrame: return TickBatchMode.SixtyTicksPerFrame;
                default: return TickBatchMode.Realtime;
            }
        }

        private static string FormatFrameCap(MovementDemoFrameCap value)
        {
            return value == MovementDemoFrameCap.Unlimited
                ? "Unlimited"
                : ((int)value).ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatTickBatch(TickBatchMode value)
        {
            return value == TickBatchMode.Realtime
                ? "Realtime"
                : ((int)value).ToString(CultureInfo.InvariantCulture) + " ticks/render";
        }

        private static string FormatOwner(in CoreRuntimeState core)
        {
            return core.HasOwner
                ? "P" + core.Owner.Index.ToString(CultureInfo.InvariantCulture)
                : "None";
        }

        private sealed class CoreLifecycleRenderer : IDisposable
        {
            private const int CirclePointCount = 49;

            private readonly GameObject root;
            private readonly Material material;
            private readonly LineRenderer footprint;
            private readonly LineRenderer carrierRing;
            private readonly LineRenderer possessionGraceRing;
            private readonly LineRenderer velocity;
            private readonly LineRenderer resetPath;

            public CoreLifecycleRenderer(Transform parent)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    throw new InvalidOperationException("Sprites/Default shader is required for the core state overlay.");
                }

                root = new GameObject("Relay Core State Overlay");
                root.transform.SetParent(parent, false);
                material = new Material(shader) { hideFlags = HideFlags.DontSave };
                footprint = CreateLine("Core Collision Footprint", Color.white, CirclePointCount, 0.035f);
                carrierRing = CreateLine("Carrier Ownership Ring", Color.cyan, CirclePointCount, 0.06f);
                possessionGraceRing = CreateLine(
                    "Possession Grace Ring",
                    new Color(1f, 0.82f, 0.15f),
                    CirclePointCount,
                    0.09f);
                velocity = CreateLine("Core Velocity", Color.magenta, 2, 0.055f);
                resetPath = CreateLine("Core Reset Path", Color.yellow, 2, 0.08f);
            }

            public void Render(
                in CoreRuntimeState core,
                in PlayerState playerZero,
                in PlayerState playerOne,
                in CoreConfig config,
                in RegulationConfig regulationConfig,
                SimulationTick tick,
                float2 resetPosition)
            {
                bool resetting = core.Mode == CoreMode.Resetting;
                footprint.enabled = !resetting && core.Mode != CoreMode.Carried;
                carrierRing.enabled = core.Mode == CoreMode.Carried && core.HasOwner;
                uint graceRemaining = core.GetPossessionGraceRemainingTicks(tick);
                possessionGraceRing.enabled = carrierRing.enabled && graceRemaining > 0u;
                velocity.enabled = (core.Mode == CoreMode.Loose || core.Mode == CoreMode.DropLock) &&
                    math.lengthsq(core.Velocity) > 0.0001f;
                resetPath.enabled = resetting;

                if (footprint.enabled)
                {
                    Color stateColor = core.Mode == CoreMode.Locked
                        ? new Color(0.55f, 0.65f, 0.75f)
                        : core.Mode == CoreMode.DropLock
                            ? new Color(1f, 0.68f, 0.12f)
                            : Color.white;
                    SetCircle(footprint, core.Position, config.RadiusMeters, 0.16f, stateColor);
                }

                if (carrierRing.enabled)
                {
                    PlayerState carrier = core.Owner == PlayerSlot.Zero ? playerZero : playerOne;
                    Color ownerColor = core.Owner == PlayerSlot.Zero
                        ? new Color(0.15f, 0.85f, 1f)
                        : new Color(1f, 0.35f, 0.15f);
                    SetCircle(carrierRing, carrier.Position, 0.68f, 0.12f, ownerColor);
                    if (possessionGraceRing.enabled)
                    {
                        float progress = 1f - graceRemaining / (float)regulationConfig.PossessionGraceTicks;
                        SetArc(
                            possessionGraceRing,
                            carrier.Position,
                            0.79f,
                            0.14f,
                            math.saturate(progress));
                    }
                }

                if (velocity.enabled)
                {
                    SetSegment(velocity, core.Position, core.Position + core.Velocity * 0.2f, 0.2f);
                }

                if (resetting)
                {
                    SetSegment(resetPath, core.ResetOriginPosition, resetPosition, 0.28f);
                }
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

            private LineRenderer CreateLine(string name, Color color, int positionCount, float width)
            {
                GameObject lineObject = new GameObject(name);
                lineObject.transform.SetParent(root.transform, false);
                LineRenderer line = lineObject.AddComponent<LineRenderer>();
                line.sharedMaterial = material;
                line.useWorldSpace = true;
                line.startColor = color;
                line.endColor = color;
                line.startWidth = width;
                line.endWidth = width;
                line.positionCount = positionCount;
                line.numCapVertices = 2;
                return line;
            }

            private static void SetCircle(
                LineRenderer line,
                float2 center,
                float radius,
                float height,
                Color color)
            {
                line.startColor = color;
                line.endColor = color;
                for (int index = 0; index < CirclePointCount; index++)
                {
                    float angle = index * math.PI * 2f / (CirclePointCount - 1);
                    float2 point = center + new float2(math.cos(angle), math.sin(angle)) * radius;
                    line.SetPosition(index, new Vector3(point.x, height, point.y));
                }
            }

            private static void SetSegment(LineRenderer line, float2 start, float2 end, float height)
            {
                line.SetPosition(0, new Vector3(start.x, height, start.y));
                line.SetPosition(1, new Vector3(end.x, height, end.y));
            }

            private static void SetArc(
                LineRenderer line,
                float2 center,
                float radius,
                float height,
                float progress)
            {
                for (int index = 0; index < CirclePointCount; index++)
                {
                    float angle = index * math.PI * 2f * progress / (CirclePointCount - 1) - math.PI * 0.5f;
                    float2 point = center + new float2(math.cos(angle), math.sin(angle)) * radius;
                    line.SetPosition(index, new Vector3(point.x, height, point.y));
                }
            }
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
