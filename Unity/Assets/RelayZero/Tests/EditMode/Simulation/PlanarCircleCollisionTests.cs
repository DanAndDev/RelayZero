using System;
using System.Collections.Generic;
using NUnit.Framework;
using RelayZero.Simulation;
using Unity.Mathematics;

namespace RelayZero.Tests.EditMode.Simulation
{
    public sealed class PlanarCircleCollisionTests
    {
        [Test]
        public void HeadOnSweepHitsTheSegmentStripAtTheEarliestTime()
        {
            bool didHit = PlanarCircleCollision.TrySweepSegmentCapsule(
                new float2(-2f, 0f),
                new float2(3f, 0f),
                new float2(0f, -1f),
                new float2(0f, 1f),
                0.5f,
                out PlanarCircleSweepHit hit);

            Assert.That(didHit, Is.True);
            Assert.That(hit.Time, Is.EqualTo(0.5f).Within(0.00001f));
            Assert.That(hit.Position, Is.EqualTo(new float2(-0.5f, 0f)).Using(Float2Comparer));
            Assert.That(hit.Normal, Is.EqualTo(new float2(-1f, 0f)).Using(Float2Comparer));
            Assert.That(hit.StartedOverlapping, Is.False);
            Assert.That(hit.PenetrationDepth, Is.Zero);
        }

        [Test]
        public void TangentSweepReportsEndpointGrazingWithoutNonFiniteOutput()
        {
            bool didHit = PlanarCircleCollision.TrySweepSegmentCapsule(
                new float2(-1f, 1f),
                new float2(2f, 0f),
                new float2(0f, 0f),
                new float2(2f, 0f),
                1f,
                out PlanarCircleSweepHit hit);

            Assert.That(didHit, Is.True);
            Assert.That(hit.Time, Is.EqualTo(0.5f).Within(0.00001f));
            Assert.That(hit.Position, Is.EqualTo(new float2(0f, 1f)).Using(Float2Comparer));
            Assert.That(hit.Normal, Is.EqualTo(new float2(0f, 1f)).Using(Float2Comparer));
            Assert.That(math.all(math.isfinite(hit.Position)), Is.True);
            Assert.That(math.all(math.isfinite(hit.Normal)), Is.True);
        }

        [Test]
        public void RotatedTangentSweepDoesNotLoseEndpointContactToFloatCancellation()
        {
            bool didHit = PlanarCircleCollision.TrySweepSegmentCapsule(
                new float2(-0.0578452945f, 0.4490588307f),
                new float2(0.0999847725f, 0.00174524053f),
                float2.zero,
                float2.zero,
                0.45f,
                out PlanarCircleSweepHit hit);

            Assert.That(didHit, Is.True);
            Assert.That(hit.Time, Is.EqualTo(0.49896845f).Within(0.000001f));
            Assert.That(math.all(math.isfinite(hit.Position)), Is.True);
            Assert.That(math.all(math.isfinite(hit.Normal)), Is.True);
            Assert.That(math.length(hit.Normal), Is.EqualTo(1f).Within(0.00001f));
        }

        [Test]
        public void DiagonalSweepUsesTheRoundedEndpointNormal()
        {
            bool didHit = PlanarCircleCollision.TrySweepSegmentCapsule(
                new float2(-2f, -2f),
                new float2(3f, 3f),
                new float2(0f, 0f),
                new float2(2f, 0f),
                0.5f,
                out PlanarCircleSweepHit hit);

            float expectedCoordinate = -0.5f * math.rsqrt(2f);
            float expectedTime = (2f + expectedCoordinate) / 3f;
            Assert.That(didHit, Is.True);
            Assert.That(hit.Time, Is.EqualTo(expectedTime).Within(0.00001f));
            Assert.That(hit.Position, Is.EqualTo(new float2(expectedCoordinate, expectedCoordinate)).Using(Float2Comparer));
            Assert.That(hit.Normal, Is.EqualTo(new float2(-math.rsqrt(2f), -math.rsqrt(2f))).Using(Float2Comparer));
        }

        [Test]
        public void ZeroMovementOutsideTheCapsuleDoesNotHit()
        {
            bool didHit = PlanarCircleCollision.TrySweepSegmentCapsule(
                new float2(0f, 2f),
                float2.zero,
                new float2(-1f, 0f),
                new float2(1f, 0f),
                0.5f,
                out PlanarCircleSweepHit hit);

            Assert.That(didHit, Is.False);
            Assert.That(hit, Is.EqualTo(default(PlanarCircleSweepHit)));
        }

        [Test]
        public void StartOverlapReturnsImmediateDepenetrationDataEvenWithoutMovement()
        {
            bool didHit = PlanarCircleCollision.TrySweepSegmentCapsule(
                new float2(1f, 0.25f),
                float2.zero,
                new float2(0f, 0f),
                new float2(2f, 0f),
                0.5f,
                out PlanarCircleSweepHit hit);

            Assert.That(didHit, Is.True);
            Assert.That(hit.Time, Is.Zero);
            Assert.That(hit.Position, Is.EqualTo(new float2(1f, 0.25f)).Using(Float2Comparer));
            Assert.That(hit.Normal, Is.EqualTo(new float2(0f, 1f)).Using(Float2Comparer));
            Assert.That(hit.StartedOverlapping, Is.True);
            Assert.That(hit.PenetrationDepth, Is.EqualTo(0.25f).Within(0.00001f));
        }

        [Test]
        public void LongSweepCannotTunnelThroughTheSegment()
        {
            bool didHit = PlanarCircleCollision.TrySweepSegmentCapsule(
                new float2(-10f, 0f),
                new float2(20f, 0f),
                new float2(0f, -2f),
                new float2(0f, 2f),
                0.5f,
                out PlanarCircleSweepHit hit);

            Assert.That(didHit, Is.True);
            Assert.That(hit.Time, Is.EqualTo(0.475f).Within(0.00001f));
            Assert.That(hit.Position, Is.EqualTo(new float2(-0.5f, 0f)).Using(Float2Comparer));
            Assert.That(hit.Normal, Is.EqualTo(new float2(-1f, 0f)).Using(Float2Comparer));
        }

        [Test]
        public void ReversingSegmentEndpointsPreservesTheSweepResult()
        {
            bool forwardHit = PlanarCircleCollision.TrySweepSegmentCapsule(
                new float2(-2f, 0.25f),
                new float2(3f, 0f),
                new float2(0f, -1f),
                new float2(0f, 1f),
                0.5f,
                out PlanarCircleSweepHit forward);
            bool reverseHit = PlanarCircleCollision.TrySweepSegmentCapsule(
                new float2(-2f, 0.25f),
                new float2(3f, 0f),
                new float2(0f, 1f),
                new float2(0f, -1f),
                0.5f,
                out PlanarCircleSweepHit reverse);

            Assert.That(forwardHit, Is.True);
            Assert.That(reverseHit, Is.True);
            Assert.That(reverse.Time, Is.EqualTo(forward.Time).Within(0.00001f));
            Assert.That(reverse.Position, Is.EqualTo(forward.Position).Using(Float2Comparer));
            Assert.That(reverse.Normal, Is.EqualTo(forward.Normal).Using(Float2Comparer));
        }

        [Test]
        public void ContactMovingAwayDoesNotCreateAStickyHit()
        {
            bool didHit = PlanarCircleCollision.TrySweepSegmentCapsule(
                new float2(-0.5f, 0f),
                new float2(-1f, 0f),
                new float2(0f, -1f),
                new float2(0f, 1f),
                0.5f,
                out _);

            Assert.That(didHit, Is.False);
        }

        [Test]
        public void NonFiniteInputsAreRejectedBeforeGeometryIsEvaluated()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PlanarCircleCollision.TrySweepSegmentCapsule(
                new float2(float.NaN, 0f),
                new float2(1f, 0f),
                float2.zero,
                new float2(0f, 1f),
                0.5f,
                out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => PlanarCircleCollision.TrySweepSegmentCapsule(
                float2.zero,
                new float2(float.PositiveInfinity, 0f),
                float2.zero,
                new float2(0f, 1f),
                0.5f,
                out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => PlanarCircleCollision.TrySweepSegmentCapsule(
                float2.zero,
                new float2(1f, 0f),
                new float2(0f, float.NegativeInfinity),
                new float2(0f, 1f),
                0.5f,
                out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => PlanarCircleCollision.TrySweepSegmentCapsule(
                float2.zero,
                new float2(1f, 0f),
                float2.zero,
                new float2(0f, 1f),
                float.PositiveInfinity,
                out _));
        }

        private static readonly IEqualityComparer<float2> Float2Comparer =
            new Float2WithinComparer(0.00001f);

        private sealed class Float2WithinComparer : IEqualityComparer<float2>
        {
            private readonly float tolerance;

            public Float2WithinComparer(float tolerance)
            {
                this.tolerance = tolerance;
            }

            public bool Equals(float2 left, float2 right)
            {
                return math.all(math.abs(left - right) <= tolerance);
            }

            public int GetHashCode(float2 value)
            {
                return value.GetHashCode();
            }
        }
    }
}
