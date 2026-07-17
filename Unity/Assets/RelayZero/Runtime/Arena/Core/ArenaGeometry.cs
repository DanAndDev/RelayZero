using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace RelayZero.Arena
{
    public readonly struct ArenaAabb
    {
        public ArenaAabb(float2 minimum, float2 maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public float2 Minimum { get; }

        public float2 Maximum { get; }

        public bool Contains(float2 point, float epsilon = 0f)
        {
            return point.x >= Minimum.x - epsilon &&
                   point.y >= Minimum.y - epsilon &&
                   point.x <= Maximum.x + epsilon &&
                   point.y <= Maximum.y + epsilon;
        }

        public ArenaAabb Expanded(float amount)
        {
            float2 expansion = new float2(amount, amount);
            return new ArenaAabb(Minimum - expansion, Maximum + expansion);
        }
    }

    public static class ArenaGeometry
    {
        public const float CanonicalPrecision = 0.0001f;
        public const float Epsilon = 0.00001f;

        public static float Canonicalize(float value)
        {
            if (!math.isfinite(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Arena geometry must be finite.");
            }

            double scaled = Math.Round(
                value / CanonicalPrecision,
                MidpointRounding.AwayFromZero);
            float canonical = (float)(scaled * CanonicalPrecision);
            return math.abs(canonical) < CanonicalPrecision * 0.5f ? 0f : canonical;
        }

        public static float2 Canonicalize(float2 value)
        {
            return new float2(Canonicalize(value.x), Canonicalize(value.y));
        }

        public static bool IsFinite(float2 value)
        {
            return math.all(math.isfinite(value));
        }

        public static float Cross(float2 a, float2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        public static float SignedArea(IReadOnlyList<float2> vertices)
        {
            if (vertices == null || vertices.Count < 3)
            {
                return 0f;
            }

            float twiceArea = 0f;
            for (int i = 0; i < vertices.Count; i++)
            {
                float2 current = vertices[i];
                float2 next = vertices[(i + 1) % vertices.Count];
                twiceArea += Cross(current, next);
            }

            return twiceArea * 0.5f;
        }

        public static bool IsConvex(IReadOnlyList<float2> vertices, float epsilon = Epsilon)
        {
            if (vertices == null || vertices.Count < 3)
            {
                return false;
            }

            float sign = 0f;
            for (int i = 0; i < vertices.Count; i++)
            {
                float2 a = vertices[i];
                float2 b = vertices[(i + 1) % vertices.Count];
                float2 c = vertices[(i + 2) % vertices.Count];
                if (!IsFinite(a) || !IsFinite(b) || !IsFinite(c))
                {
                    return false;
                }

                float cross = Cross(b - a, c - b);
                if (math.abs(cross) <= epsilon)
                {
                    continue;
                }

                float currentSign = math.sign(cross);
                if (sign == 0f)
                {
                    sign = currentSign;
                }
                else if (currentSign != sign)
                {
                    return false;
                }
            }

            return sign != 0f;
        }

        public static float2[] CanonicalizePolygon(IReadOnlyList<float2> vertices)
        {
            if (!IsConvex(vertices))
            {
                throw new ArgumentException("Polygon must be finite and convex.", nameof(vertices));
            }

            float2[] result = new float2[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                result[i] = Canonicalize(vertices[i]);
            }

            if (SignedArea(result) < 0f)
            {
                Array.Reverse(result);
            }

            int first = 0;
            for (int i = 1; i < result.Length; i++)
            {
                if (result[i].x < result[first].x ||
                    (result[i].x == result[first].x && result[i].y < result[first].y))
                {
                    first = i;
                }
            }

            if (first == 0)
            {
                return result;
            }

            float2[] rotated = new float2[result.Length];
            for (int i = 0; i < result.Length; i++)
            {
                rotated[i] = result[(first + i) % result.Length];
            }

            return rotated;
        }

        public static float2[] ComputeOutwardNormals(IReadOnlyList<float2> canonicalCounterClockwiseVertices)
        {
            if (canonicalCounterClockwiseVertices == null || canonicalCounterClockwiseVertices.Count < 3)
            {
                throw new ArgumentException("At least three polygon vertices are required.", nameof(canonicalCounterClockwiseVertices));
            }

            float2[] normals = new float2[canonicalCounterClockwiseVertices.Count];
            for (int i = 0; i < canonicalCounterClockwiseVertices.Count; i++)
            {
                float2 edge = canonicalCounterClockwiseVertices[(i + 1) % canonicalCounterClockwiseVertices.Count] -
                              canonicalCounterClockwiseVertices[i];
                float lengthSquared = math.lengthsq(edge);
                if (lengthSquared <= Epsilon * Epsilon)
                {
                    throw new ArgumentException("Polygon edges must have non-zero length.", nameof(canonicalCounterClockwiseVertices));
                }

                normals[i] = math.normalize(new float2(edge.y, -edge.x));
            }

            return normals;
        }

        public static ArenaAabb ComputeBounds(IReadOnlyList<float2> points)
        {
            if (points == null || points.Count == 0)
            {
                throw new ArgumentException("At least one point is required.", nameof(points));
            }

            float2 minimum = points[0];
            float2 maximum = points[0];
            for (int i = 1; i < points.Count; i++)
            {
                minimum = math.min(minimum, points[i]);
                maximum = math.max(maximum, points[i]);
            }

            return new ArenaAabb(Canonicalize(minimum), Canonicalize(maximum));
        }

        public static float DistanceSquaredToSegment(float2 point, float2 start, float2 end)
        {
            float2 segment = end - start;
            float lengthSquared = math.lengthsq(segment);
            if (lengthSquared <= Epsilon * Epsilon)
            {
                return math.lengthsq(point - start);
            }

            float t = math.clamp(math.dot(point - start, segment) / lengthSquared, 0f, 1f);
            float2 closest = start + segment * t;
            return math.lengthsq(point - closest);
        }

        public static bool PointInConvexPolygon(float2 point, IReadOnlyList<float2> vertices, float epsilon = Epsilon)
        {
            if (vertices == null || vertices.Count < 3)
            {
                return false;
            }

            float sign = 0f;
            for (int i = 0; i < vertices.Count; i++)
            {
                float cross = Cross(vertices[(i + 1) % vertices.Count] - vertices[i], point - vertices[i]);
                if (math.abs(cross) <= epsilon)
                {
                    continue;
                }

                float currentSign = math.sign(cross);
                if (sign == 0f)
                {
                    sign = currentSign;
                }
                else if (currentSign != sign)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool PointInPolygon(float2 point, IReadOnlyList<float2> vertices)
        {
            if (vertices == null || vertices.Count < 3)
            {
                return false;
            }

            bool inside = false;
            for (int i = 0, j = vertices.Count - 1; i < vertices.Count; j = i++)
            {
                float2 a = vertices[i];
                float2 b = vertices[j];
                bool crosses = (a.y > point.y) != (b.y > point.y) &&
                               point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
                if (crosses)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        public static bool SegmentsIntersect(
            float2 aStart,
            float2 aEnd,
            float2 bStart,
            float2 bEnd,
            float epsilon = Epsilon)
        {
            float2 r = aEnd - aStart;
            float2 s = bEnd - bStart;
            float denominator = Cross(r, s);
            float2 difference = bStart - aStart;
            float numeratorA = Cross(difference, s);
            float numeratorB = Cross(difference, r);

            if (math.abs(denominator) <= epsilon)
            {
                if (math.abs(numeratorA) > epsilon || math.abs(numeratorB) > epsilon)
                {
                    return false;
                }

                return DistanceSquaredToSegment(aStart, bStart, bEnd) <= epsilon * epsilon ||
                       DistanceSquaredToSegment(aEnd, bStart, bEnd) <= epsilon * epsilon ||
                       DistanceSquaredToSegment(bStart, aStart, aEnd) <= epsilon * epsilon ||
                       DistanceSquaredToSegment(bEnd, aStart, aEnd) <= epsilon * epsilon;
            }

            float t = numeratorA / denominator;
            float u = numeratorB / denominator;
            return t >= -epsilon && t <= 1f + epsilon && u >= -epsilon && u <= 1f + epsilon;
        }

        public static bool SegmentIntersectsConvexPolygon(
            float2 start,
            float2 end,
            IReadOnlyList<float2> vertices)
        {
            if (PointInConvexPolygon(start, vertices) || PointInConvexPolygon(end, vertices))
            {
                return true;
            }

            for (int i = 0; i < vertices.Count; i++)
            {
                if (SegmentsIntersect(start, end, vertices[i], vertices[(i + 1) % vertices.Count]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
