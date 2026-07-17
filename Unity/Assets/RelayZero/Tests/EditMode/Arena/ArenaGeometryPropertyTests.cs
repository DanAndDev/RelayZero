using System;
using NUnit.Framework;
using RelayZero.Arena;
using Unity.Mathematics;

namespace RelayZero.Tests.EditMode.Arena
{
    public sealed class ArenaGeometryPropertyTests
    {
        [Test]
        public void CanonicalPolygonIsInvariantToWindingAndStartingVertex()
        {
            float2[] first =
            {
                new float2(-2f, -1f),
                new float2(2f, -1f),
                new float2(2f, 1f),
                new float2(-2f, 1f),
            };
            float2[] rotatedAndReversed =
            {
                new float2(2f, 1f),
                new float2(2f, -1f),
                new float2(-2f, -1f),
                new float2(-2f, 1f),
            };

            Assert.That(ArenaGeometry.CanonicalizePolygon(rotatedAndReversed), Is.EqualTo(ArenaGeometry.CanonicalizePolygon(first)));
        }

        [Test]
        public void ComputedNormalsAreUnitLengthPerpendicularAndOutward()
        {
            float2[] polygon = ArenaGeometry.CanonicalizePolygon(new[]
            {
                new float2(-2f, -1f),
                new float2(2f, -1f),
                new float2(2f, 1f),
                new float2(-2f, 1f),
            });
            float2[] normals = ArenaGeometry.ComputeOutwardNormals(polygon);

            for (int index = 0; index < polygon.Length; index++)
            {
                float2 edge = polygon[(index + 1) % polygon.Length] - polygon[index];
                Assert.That(math.length(normals[index]), Is.EqualTo(1f).Within(0.00001f));
                Assert.That(math.dot(edge, normals[index]), Is.EqualTo(0f).Within(0.00001f));
                Assert.That(math.dot((polygon[index] + polygon[(index + 1) % polygon.Length]) * 0.5f, normals[index]), Is.GreaterThan(0f));
            }
        }

        [Test]
        public void SegmentOperationsAreOrderInvariantAcrossDeterministicSamples()
        {
            System.Random random = new System.Random(0x52A03);
            for (int index = 0; index < 512; index++)
            {
                float2 point = Next(random);
                float2 a = Next(random);
                float2 b = Next(random);
                float2 c = Next(random);
                float2 d = Next(random);

                Assert.That(
                    ArenaGeometry.DistanceSquaredToSegment(point, a, b),
                    Is.EqualTo(ArenaGeometry.DistanceSquaredToSegment(point, b, a)).Within(0.0001f));
                Assert.That(
                    ArenaGeometry.SegmentsIntersect(a, b, c, d),
                    Is.EqualTo(ArenaGeometry.SegmentsIntersect(c, d, a, b)));
            }
        }

        [Test]
        public void StableIdAndFloatCanonicalizationAreRepeatable()
        {
            ArenaElementId first = ArenaElementId.FromStableId("gate.alpha");
            ArenaElementId second = ArenaElementId.FromStableId("gate.alpha");

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first, Is.Not.EqualTo(ArenaElementId.None));
            Assert.That(ArenaGeometry.Canonicalize(-0.000001f), Is.EqualTo(0f));
            Assert.That(ArenaGeometry.Canonicalize(1.23456f), Is.EqualTo(1.2346f));
        }

        private static float2 Next(System.Random random)
        {
            return new float2(
                (float)(random.NextDouble() * 30d - 15d),
                (float)(random.NextDouble() * 22d - 11d));
        }
    }
}
