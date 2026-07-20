using NUnit.Framework;
using RelayZero.Client.Presentation;
using Unity.Mathematics;
using UnityEngine;

namespace RelayZero.Tests.EditMode.Client
{
    public sealed class StableAimProjectionTests
    {
        [Test]
        public void TopDownStickProjectionUsesCameraRightAndUpOnTheArenaPlane()
        {
            GameObject cameraObject = new GameObject("Stable Aim Camera");
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                cameraObject.transform.SetPositionAndRotation(
                    new Vector3(0f, 10f, 0f),
                    Quaternion.Euler(90f, 0f, 0f));

                Assert.That(StableAimProjection.TryProjectStick(
                    camera,
                    new Vector2(0f, 0.1f),
                    0.25f,
                    out _), Is.False);
                Assert.That(StableAimProjection.TryProjectStick(
                    camera,
                    new Vector2(1f, 0f),
                    0.25f,
                    out float2 right), Is.True);
                Assert.That(right.x, Is.EqualTo(1f).Within(0.00001f));
                Assert.That(right.y, Is.Zero.Within(0.00001f));
                Assert.That(StableAimProjection.TryProjectStick(
                    camera,
                    new Vector2(0f, 1f),
                    0.25f,
                    out float2 up), Is.True);
                Assert.That(up.x, Is.Zero.Within(0.00001f));
                Assert.That(up.y, Is.EqualTo(1f).Within(0.00001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void PointerProjectionRejectsDegenerateAimWithoutInventingDirection()
        {
            GameObject cameraObject = new GameObject("Stable Aim Camera");
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.pixelRect = new Rect(0f, 0f, 200f, 100f);
                cameraObject.transform.SetPositionAndRotation(
                    new Vector3(0f, 10f, 0f),
                    Quaternion.Euler(90f, 0f, 0f));

                Assert.That(StableAimProjection.TryProjectPointer(
                    camera,
                    new Vector2(100f, 50f),
                    float2.zero,
                    out _), Is.False);
                Assert.That(StableAimProjection.TryProjectPointer(
                    camera,
                    new Vector2(150f, 50f),
                    float2.zero,
                    out float2 aim), Is.True);
                Assert.That(aim.x, Is.EqualTo(1f).Within(0.00001f));
                Assert.That(aim.y, Is.Zero.Within(0.00001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
