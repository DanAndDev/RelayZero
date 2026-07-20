using System;
using RelayZero.Client.Prediction;
using RelayZero.Simulation;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RelayZero.Client.Presentation
{
    public enum LocalControlScheme : byte
    {
        KeyboardMouse,
        Gamepad,
    }

    /// <summary>
    /// Code-defined input adapter for the Switchyard movement view. All supported devices
    /// are normalized into the same simulation command representation.
    /// </summary>
    public sealed class LocalInputSampler : IDisposable
    {
        public const float DefaultGamepadAimDeadZone = 0.25f;

        private readonly InputAction move;
        private readonly InputAction pointerAim;
        private readonly InputAction stickAim;
        private readonly InputAction pulse;
        private readonly InputAction barrier;
        private readonly InputAction dash;
        private readonly InputAction interact;
        private readonly float gamepadAimDeadZone;

        private ActivityStamp activeSchemeActivity;
        private ulong nextActivityOrder;
        private bool isEnabled;
        private bool hasPointerAimSample;

        public LocalInputSampler(float gamepadAimDeadZone = DefaultGamepadAimDeadZone)
        {
            if (!float.IsFinite(gamepadAimDeadZone) || gamepadAimDeadZone < 0f || gamepadAimDeadZone >= 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(gamepadAimDeadZone));
            }

            this.gamepadAimDeadZone = gamepadAimDeadZone;

            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");

            pointerAim = new InputAction("Aim Pointer", InputActionType.PassThrough, "<Pointer>/position");
            stickAim = new InputAction("Aim Stick", InputActionType.Value, "<Gamepad>/rightStick");
            pulse = CreateButton("Pulse", "<Mouse>/leftButton", "<Gamepad>/rightTrigger");
            barrier = CreateButton("Barrier", "<Mouse>/rightButton", "<Gamepad>/leftTrigger");
            dash = CreateButton("Dash", "<Keyboard>/space", "<Gamepad>/rightShoulder");
            interact = CreateButton("Interact", "<Keyboard>/e", "<Gamepad>/buttonSouth");

            move.performed += OnMovePerformed;
            pointerAim.performed += OnPointerAimPerformed;
            stickAim.performed += OnStickAimPerformed;
            pulse.performed += OnButtonPerformed;
            barrier.performed += OnButtonPerformed;
            dash.performed += OnButtonPerformed;
            interact.performed += OnButtonPerformed;
        }

        public LocalControlScheme ActiveScheme { get; private set; }

        public void Enable()
        {
            if (isEnabled)
            {
                return;
            }

            move.Enable();
            pointerAim.Enable();
            stickAim.Enable();
            pulse.Enable();
            barrier.Enable();
            dash.Enable();
            interact.Enable();
            isEnabled = true;
        }

        public void Disable()
        {
            if (!isEnabled)
            {
                return;
            }

            move.Disable();
            pointerAim.Disable();
            stickAim.Disable();
            pulse.Disable();
            barrier.Disable();
            dash.Disable();
            interact.Disable();
            isEnabled = false;
        }

        public void SampleFrame(
            LocalCommandBuilder builder,
            Camera stableCamera,
            float2 playerPosition,
            bool gameplayBlocked)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (stableCamera == null)
            {
                throw new ArgumentNullException(nameof(stableCamera));
            }

            builder.SetGameplayBlocked(gameplayBlocked);

            Vector2 moveValue = move.ReadValue<Vector2>();
            builder.TrySetMove(new float2(moveValue.x, moveValue.y));

            Vector2 pointerValue = pointerAim.ReadValue<Vector2>();
            Vector2 stickValue = stickAim.ReadValue<Vector2>();
            if (ActiveScheme == LocalControlScheme.Gamepad &&
                StableAimProjection.TryProjectStick(
                    stableCamera,
                    stickValue,
                    gamepadAimDeadZone,
                    out float2 stickDirection))
            {
                builder.TrySetAim(stickDirection);
            }
            else if (hasPointerAimSample &&
                     ActiveScheme == LocalControlScheme.KeyboardMouse &&
                     StableAimProjection.TryProjectPointer(
                         stableCamera,
                         pointerValue,
                         playerPosition,
                         out float2 pointerDirection))
            {
                builder.TrySetAim(pointerDirection);
            }

            InputButtons pressed = InputButtons.None;
            InputButtons held = InputButtons.None;
            SampleButton(pulse, InputButtons.Pulse, false, ref pressed, ref held);
            SampleButton(barrier, InputButtons.Barrier, false, ref pressed, ref held);
            SampleButton(dash, InputButtons.Dash, false, ref pressed, ref held);
            SampleButton(interact, InputButtons.Interact, true, ref pressed, ref held);
            builder.LatchPressed(pressed);
            builder.SetHeldButtons(held);
        }

        public void Dispose()
        {
            Disable();
            move.performed -= OnMovePerformed;
            pointerAim.performed -= OnPointerAimPerformed;
            stickAim.performed -= OnStickAimPerformed;
            pulse.performed -= OnButtonPerformed;
            barrier.performed -= OnButtonPerformed;
            dash.performed -= OnButtonPerformed;
            interact.performed -= OnButtonPerformed;
            move.Dispose();
            pointerAim.Dispose();
            stickAim.Dispose();
            pulse.Dispose();
            barrier.Dispose();
            dash.Dispose();
            interact.Dispose();
        }

        private static InputAction CreateButton(string name, string keyboardMousePath, string gamepadPath)
        {
            InputAction action = new InputAction(name, InputActionType.Button);
            action.AddBinding(keyboardMousePath);
            action.AddBinding(gamepadPath);
            return action;
        }

        private static void SampleButton(
            InputAction action,
            InputButtons button,
            bool includeHeldState,
            ref InputButtons pressed,
            ref InputButtons held)
        {
            if (action.WasPressedThisFrame())
            {
                pressed |= button;
            }

            if (includeHeldState && action.IsPressed())
            {
                held |= button;
            }
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            Vector2 value = context.ReadValue<Vector2>();
            if (IsFinite(value) && IsMeaningful(value))
            {
                RecordActivity(context.control, context.time);
            }
        }

        private void OnPointerAimPerformed(InputAction.CallbackContext context)
        {
            if (!IsFinite(context.ReadValue<Vector2>()))
            {
                return;
            }

            hasPointerAimSample = true;
            RecordActivity(context.control, context.time);
        }

        private void OnStickAimPerformed(InputAction.CallbackContext context)
        {
            Vector2 value = context.ReadValue<Vector2>();
            if (IsFinite(value) && value.sqrMagnitude > gamepadAimDeadZone * gamepadAimDeadZone)
            {
                RecordActivity(context.control, context.time);
            }
        }

        private void OnButtonPerformed(InputAction.CallbackContext context)
        {
            RecordActivity(context.control, context.time);
        }

        private void RecordActivity(InputControl control, double time)
        {
            ActivityStamp activity = new ActivityStamp(time, unchecked(++nextActivityOrder));
            if (!activity.IsNewerThan(activeSchemeActivity))
            {
                return;
            }

            activeSchemeActivity = activity;
            ActiveScheme = IsGamepadControl(control)
                ? LocalControlScheme.Gamepad
                : LocalControlScheme.KeyboardMouse;
        }

        private static bool IsMeaningful(Vector2 value)
        {
            return value.sqrMagnitude > 0.000001f;
        }

        private static bool IsFinite(Vector2 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y);
        }

        private static bool IsGamepadControl(InputControl control)
        {
            return control != null && control.device is Gamepad;
        }

        private readonly struct ActivityStamp
        {
            public ActivityStamp(double time, ulong order)
            {
                Time = time;
                Order = order;
                IsValid = !double.IsNaN(time) && !double.IsInfinity(time);
            }

            public double Time { get; }

            public ulong Order { get; }

            public bool IsValid { get; }

            public bool IsNewerThan(ActivityStamp other)
            {
                return IsValid &&
                       (!other.IsValid || Time > other.Time || (Time == other.Time && Order > other.Order));
            }
        }
    }
}
