using NUnit.Framework;
using RelayZero.Client.Prediction;
using RelayZero.Client.Presentation;
using RelayZero.Foundation;
using RelayZero.Simulation;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RelayZero.Tests.EditMode.Client
{
    public sealed class LocalInputSamplerTests : InputTestFixture
    {
        private Keyboard keyboard = null!;
        private Mouse mouse = null!;
        private Gamepad gamepad = null!;
        private GameObject cameraObject = null!;
        private Camera stableCamera = null!;
        private LocalInputSampler sampler = null!;
        private LocalCommandBuilder builder = null!;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            gamepad = InputSystem.AddDevice<Gamepad>();

            cameraObject = new GameObject("Local Input Sampler Test Camera");
            stableCamera = cameraObject.AddComponent<Camera>();
            stableCamera.orthographic = true;
            stableCamera.orthographicSize = 5f;
            stableCamera.pixelRect = new Rect(0f, 0f, 200f, 100f);
            cameraObject.transform.SetPositionAndRotation(
                new Vector3(0f, 10f, 0f),
                Quaternion.Euler(90f, 0f, 0f));

            builder = new LocalCommandBuilder(new CommandSequence(1u));
            sampler = new LocalInputSampler();
            sampler.Enable();
        }

        public override void TearDown()
        {
            try
            {
                sampler?.Dispose();
                if (cameraObject != null)
                {
                    Object.DestroyImmediate(cameraObject);
                }
            }
            finally
            {
                base.TearDown();
            }
        }

        [Test]
        public void MostRecentAimDeviceWinsWhileOlderStickRemainsDeflected()
        {
            Set(gamepad.rightStick, Vector2.right, time: 1d);
            SampleFrame();
            InputCommand gamepadCommand = builder.Build(new SimulationTick(1u));

            Assert.That(sampler.ActiveScheme, Is.EqualTo(LocalControlScheme.Gamepad));
            AssertDirection(gamepadCommand.Aim, new float2(1f, 0f));

            Move(mouse.position, new Vector2(100f, 75f), time: 2d);
            SampleFrame();
            InputCommand mouseCommand = builder.Build(new SimulationTick(2u));

            Assert.That(sampler.ActiveScheme, Is.EqualTo(LocalControlScheme.KeyboardMouse));
            AssertDirection(mouseCommand.Aim, new float2(0f, 1f));

            Set(gamepad.rightStick, Vector2.down, time: 3d);
            SampleFrame();
            InputCommand returnedGamepadCommand = builder.Build(new SimulationTick(3u));

            Assert.That(sampler.ActiveScheme, Is.EqualTo(LocalControlScheme.Gamepad));
            AssertDirection(returnedGamepadCommand.Aim, new float2(0f, -1f));
        }

        [Test]
        public void KeyboardMovementTakesSchemeFromAnUnchangedGamepadAim()
        {
            Set(gamepad.rightStick, Vector2.right, time: 1d);
            SampleFrame();
            builder.Build(new SimulationTick(1u));

            Press(keyboard.wKey, time: 2d);
            SampleFrame();
            InputCommand keyboardCommand = builder.Build(new SimulationTick(2u));

            Assert.That(sampler.ActiveScheme, Is.EqualTo(LocalControlScheme.KeyboardMouse));
            Assert.That(keyboardCommand.Move, Is.EqualTo(new float2(0f, 1f)));
            AssertDirection(keyboardCommand.Aim, new float2(1f, 0f));

            Press(gamepad.buttonSouth, time: 3d);
            SampleFrame();
            InputCommand gamepadCommand = builder.Build(new SimulationTick(3u));

            Assert.That(sampler.ActiveScheme, Is.EqualTo(LocalControlScheme.Gamepad));
            Assert.That(
                gamepadCommand.PressedButtons & InputButtons.Interact,
                Is.EqualTo(InputButtons.Interact));
        }

        [Test]
        public void OnlyTerminalCapableInteractPopulatesHeldButtons()
        {
            Press(gamepad.rightTrigger, time: 1d, queueEventOnly: true);
            Press(gamepad.leftTrigger, time: 1d, queueEventOnly: true);
            Press(gamepad.rightShoulder, time: 1d, queueEventOnly: true);
            Press(gamepad.buttonSouth, time: 1d, queueEventOnly: true);
            InputSystem.Update();

            SampleFrame();
            InputCommand command = builder.Build(new SimulationTick(1u));

            InputButtons allActions = InputButtons.Pulse |
                                      InputButtons.Barrier |
                                      InputButtons.Dash |
                                      InputButtons.Interact;
            Assert.That(command.PressedButtons, Is.EqualTo(allActions));
            Assert.That(command.HeldButtons, Is.EqualTo(InputButtons.Interact),
                "Interact is both an edge for pickup/drop and held state for terminal channeling.");
        }

        [Test]
        public void PressAndReleaseBetweenSamplesIsLatchedForExactlyOneTick()
        {
            Press(mouse.leftButton, time: 1d, queueEventOnly: true);
            Release(mouse.leftButton, time: 1.01d, queueEventOnly: true);
            InputSystem.Update();

            SampleFrame();
            InputCommand first = builder.Build(new SimulationTick(1u));
            InputCommand second = builder.Build(new SimulationTick(2u));

            Assert.That(first.PressedButtons, Is.EqualTo(InputButtons.Pulse));
            Assert.That(first.HeldButtons, Is.EqualTo(InputButtons.None));
            Assert.That(second.PressedButtons, Is.EqualTo(InputButtons.None));
        }

        private void SampleFrame()
        {
            sampler.SampleFrame(builder, stableCamera, float2.zero, false);
        }

        private static void AssertDirection(float2 actual, float2 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.00001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.00001f));
        }
    }
}
