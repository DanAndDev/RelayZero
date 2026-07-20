using System;
using Unity.Mathematics;
using UnityEngine;

namespace RelayZero.Client.Presentation
{
    public static class StableAimProjection
    {
        private const float DirectionEpsilonSquared = 0.000001f;
        private const float RayEpsilon = 0.000001f;

        public static bool TryProjectPointer(
            Camera stableCamera,
            Vector2 pointerPosition,
            float2 playerPosition,
            out float2 aimDirection)
        {
            if (stableCamera == null)
            {
                throw new ArgumentNullException(nameof(stableCamera));
            }

            if (!IsFinite(pointerPosition) || !math.all(math.isfinite(playerPosition)))
            {
                aimDirection = float2.zero;
                return false;
            }

            Ray ray = stableCamera.ScreenPointToRay(pointerPosition);
            if (!IsFinite(ray.origin) || !IsFinite(ray.direction) || math.abs(ray.direction.y) <= RayEpsilon)
            {
                aimDirection = float2.zero;
                return false;
            }

            float distance = -ray.origin.y / ray.direction.y;
            if (!math.isfinite(distance) || distance < 0f)
            {
                aimDirection = float2.zero;
                return false;
            }

            Vector3 hit = ray.origin + ray.direction * distance;
            return TryNormalize(new float2(hit.x, hit.z) - playerPosition, out aimDirection);
        }

        public static bool TryProjectStick(
            Camera stableCamera,
            Vector2 stick,
            float deadZone,
            out float2 aimDirection)
        {
            if (stableCamera == null)
            {
                throw new ArgumentNullException(nameof(stableCamera));
            }

            if (!IsFinite(stick) || !math.isfinite(deadZone) || deadZone < 0f || deadZone >= 1f ||
                stick.sqrMagnitude <= deadZone * deadZone)
            {
                aimDirection = float2.zero;
                return false;
            }

            Vector3 right = stableCamera.transform.right;
            Vector3 up = stableCamera.transform.up;
            float2 rightOnPlane = new float2(right.x, right.z);
            float2 upOnPlane = new float2(up.x, up.z);
            if (!TryNormalize(rightOnPlane, out rightOnPlane) || !TryNormalize(upOnPlane, out upOnPlane))
            {
                aimDirection = float2.zero;
                return false;
            }

            return TryNormalize(rightOnPlane * stick.x + upOnPlane * stick.y, out aimDirection);
        }

        private static bool TryNormalize(float2 value, out float2 normalized)
        {
            float lengthSquared = math.lengthsq(value);
            if (!math.all(math.isfinite(value)) || !math.isfinite(lengthSquared) ||
                lengthSquared <= DirectionEpsilonSquared)
            {
                normalized = float2.zero;
                return false;
            }

            normalized = value * math.rsqrt(lengthSquared);
            return true;
        }

        private static bool IsFinite(Vector2 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y);
        }

        private static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        }
    }
}
