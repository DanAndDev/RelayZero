using System;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    public readonly struct PlanarCircleSweepHit
    {
        internal PlanarCircleSweepHit(
            float time,
            float2 position,
            float2 normal,
            bool startedOverlapping,
            float penetrationDepth)
        {
            Time = time;
            Position = position;
            Normal = normal;
            StartedOverlapping = startedOverlapping;
            PenetrationDepth = penetrationDepth;
        }

        public float Time { get; }

        public float2 Position { get; }

        public float2 Normal { get; }

        public bool StartedOverlapping { get; }

        public float PenetrationDepth { get; }
    }

    public static class PlanarCircleCollision
    {
        internal const float GeometryEpsilon = 0.000001f;
        private const float GeometryEpsilonSquared = GeometryEpsilon * GeometryEpsilon;
        private const float TimeEpsilon = 0.000001f;
        private const double DiscriminantRelativeEpsilon = 0.000000000001d;

        public static bool TrySweepSegmentCapsule(
            float2 start,
            float2 displacement,
            float2 segmentStart,
            float2 segmentEnd,
            float radius,
            out PlanarCircleSweepHit hit)
        {
            ValidateInputs(start, displacement, segmentStart, segmentEnd, radius);

            float2 segment = segmentEnd - segmentStart;
            float segmentLengthSquared = math.lengthsq(segment);
            float movementLengthSquared = math.lengthsq(displacement);
            float radiusSquared = radius * radius;
            if (!math.isfinite(segmentLengthSquared) ||
                !math.isfinite(movementLengthSquared) ||
                !math.isfinite(radiusSquared))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(displacement),
                    "Collision inputs exceed the supported finite geometry range.");
            }

            float2 closestAtStart = ClosestPointOnSegment(
                start,
                segmentStart,
                segment,
                segmentLengthSquared);
            float2 startOffset = start - closestAtStart;
            float startDistanceSquared = math.lengthsq(startOffset);
            if (!math.isfinite(startDistanceSquared))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(start),
                    "Collision inputs exceed the supported finite geometry range.");
            }

            float startDistance = math.sqrt(math.max(0f, startDistanceSquared));
            float penetrationDepth = radius - startDistance;
            if (penetrationDepth > GeometryEpsilon)
            {
                float2 overlapNormal = NormalizeOrFallback(startOffset, segment, displacement);
                hit = new PlanarCircleSweepHit(
                    0f,
                    start,
                    overlapNormal,
                    true,
                    penetrationDepth);
                return true;
            }

            if (movementLengthSquared <= GeometryEpsilonSquared)
            {
                hit = default;
                return false;
            }

            bool found = false;
            float earliestTime = float.PositiveInfinity;
            hit = default;

            if (segmentLengthSquared > GeometryEpsilonSquared)
            {
                float segmentLength = math.sqrt(segmentLengthSquared);
                float2 tangent = segment / segmentLength;
                float2 firstNormal = new float2(tangent.y, -tangent.x);
                TryStripSide(
                    start,
                    displacement,
                    segmentStart,
                    tangent,
                    segmentLength,
                    firstNormal,
                    radius,
                    ref found,
                    ref earliestTime,
                    ref hit);
                TryStripSide(
                    start,
                    displacement,
                    segmentStart,
                    tangent,
                    segmentLength,
                    -firstNormal,
                    radius,
                    ref found,
                    ref earliestTime,
                    ref hit);
            }

            TryEndpoint(
                start,
                displacement,
                segmentStart,
                radius,
                ref found,
                ref earliestTime,
                ref hit);

            if (segmentLengthSquared > GeometryEpsilonSquared)
            {
                TryEndpoint(
                    start,
                    displacement,
                    segmentEnd,
                    radius,
                    ref found,
                    ref earliestTime,
                    ref hit);
            }

            return found;
        }

        private static void TryStripSide(
            float2 start,
            float2 displacement,
            float2 segmentStart,
            float2 tangent,
            float segmentLength,
            float2 normal,
            float radius,
            ref bool found,
            ref float earliestTime,
            ref PlanarCircleSweepHit hit)
        {
            float normalDisplacement = math.dot(displacement, normal);
            if (normalDisplacement >= 0f)
            {
                return;
            }

            float signedDistance = math.dot(start - segmentStart, normal);
            float candidateTime = (radius - signedDistance) / normalDisplacement;
            if (!TryClampTime(ref candidateTime))
            {
                return;
            }

            float2 candidatePosition = start + displacement * candidateTime;
            float projection = math.dot(candidatePosition - segmentStart, tangent);
            if (projection <= GeometryEpsilon || projection >= segmentLength - GeometryEpsilon)
            {
                return;
            }

            Consider(
                candidateTime,
                candidatePosition,
                normal,
                ref found,
                ref earliestTime,
                ref hit);
        }

        private static void TryEndpoint(
            float2 start,
            float2 displacement,
            float2 center,
            float radius,
            ref bool found,
            ref float earliestTime,
            ref PlanarCircleSweepHit hit)
        {
            float2 relative = start - center;
            if (!math.all(math.isfinite(relative)))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(start),
                    "Collision inputs exceed the supported finite geometry range.");
            }

            double relativeX = relative.x;
            double relativeY = relative.y;
            double displacementX = displacement.x;
            double displacementY = displacement.y;
            double quadraticA = displacementX * displacementX + displacementY * displacementY;
            double quadraticB = 2d * (relativeX * displacementX + relativeY * displacementY);
            double quadraticC = relativeX * relativeX + relativeY * relativeY - (double)radius * radius;
            double product = 4d * quadraticA * quadraticC;
            double discriminant = quadraticB * quadraticB - product;
            double discriminantScale = Math.Abs(quadraticB * quadraticB) + Math.Abs(product) + 1d;
            if (discriminant < -DiscriminantRelativeEpsilon * discriminantScale)
            {
                return;
            }

            discriminant = Math.Max(0d, discriminant);
            float candidateTime = (float)((-quadraticB - Math.Sqrt(discriminant)) / (2d * quadraticA));
            if (!TryClampTime(ref candidateTime))
            {
                return;
            }

            float2 candidatePosition = start + displacement * candidateTime;
            float2 normal = NormalizeOrFallback(candidatePosition - center, float2.zero, displacement);
            if (candidateTime <= TimeEpsilon && math.dot(displacement, normal) >= 0f)
            {
                return;
            }

            Consider(
                candidateTime,
                candidatePosition,
                normal,
                ref found,
                ref earliestTime,
                ref hit);
        }

        private static void Consider(
            float candidateTime,
            float2 candidatePosition,
            float2 candidateNormal,
            ref bool found,
            ref float earliestTime,
            ref PlanarCircleSweepHit hit)
        {
            if (found && candidateTime >= earliestTime)
            {
                return;
            }

            found = true;
            earliestTime = candidateTime;
            hit = new PlanarCircleSweepHit(
                candidateTime,
                candidatePosition,
                candidateNormal,
                false,
                0f);
        }

        private static bool TryClampTime(ref float time)
        {
            if (!math.isfinite(time) || time < -TimeEpsilon || time > 1f + TimeEpsilon)
            {
                return false;
            }

            time = math.clamp(time, 0f, 1f);
            return true;
        }

        private static float2 ClosestPointOnSegment(
            float2 point,
            float2 segmentStart,
            float2 segment,
            float segmentLengthSquared)
        {
            if (segmentLengthSquared <= GeometryEpsilonSquared)
            {
                return segmentStart;
            }

            float projection = math.clamp(
                math.dot(point - segmentStart, segment) / segmentLengthSquared,
                0f,
                1f);
            return segmentStart + segment * projection;
        }

        private static float2 NormalizeOrFallback(float2 value, float2 segment, float2 displacement)
        {
            float lengthSquared = math.lengthsq(value);
            if (math.isfinite(lengthSquared) && lengthSquared > GeometryEpsilonSquared)
            {
                return value * math.rsqrt(lengthSquared);
            }

            float displacementLengthSquared = math.lengthsq(displacement);
            if (math.isfinite(displacementLengthSquared) && displacementLengthSquared > GeometryEpsilonSquared)
            {
                return -displacement * math.rsqrt(displacementLengthSquared);
            }

            float segmentLengthSquared = math.lengthsq(segment);
            if (math.isfinite(segmentLengthSquared) && segmentLengthSquared > GeometryEpsilonSquared)
            {
                float2 tangent = segment * math.rsqrt(segmentLengthSquared);
                float2 normal = new float2(tangent.y, -tangent.x);
                if (normal.x < 0f || (normal.x == 0f && normal.y < 0f))
                {
                    normal = -normal;
                }

                return normal;
            }

            return new float2(1f, 0f);
        }

        private static void ValidateInputs(
            float2 start,
            float2 displacement,
            float2 segmentStart,
            float2 segmentEnd,
            float radius)
        {
            if (!math.all(math.isfinite(start)))
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Sweep start must be finite.");
            }

            if (!math.all(math.isfinite(displacement)))
            {
                throw new ArgumentOutOfRangeException(nameof(displacement), "Sweep displacement must be finite.");
            }

            if (!math.all(math.isfinite(segmentStart)))
            {
                throw new ArgumentOutOfRangeException(nameof(segmentStart), "Segment start must be finite.");
            }

            if (!math.all(math.isfinite(segmentEnd)))
            {
                throw new ArgumentOutOfRangeException(nameof(segmentEnd), "Segment end must be finite.");
            }

            if (!math.isfinite(radius) || radius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(radius), radius, "Capsule radius must be finite and positive.");
            }

            float2 intendedEnd = start + displacement;
            float2 segment = segmentEnd - segmentStart;
            if (!math.all(math.isfinite(intendedEnd)) || !math.all(math.isfinite(segment)))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(displacement),
                    "Collision inputs exceed the supported finite geometry range.");
            }
        }
    }
}
