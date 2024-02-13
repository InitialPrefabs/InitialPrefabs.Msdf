using System;
using Unity.Mathematics;

namespace InitialPrefabs.Msdf {

    public struct QuadraticSegment : ISegment, IDivider<QuadraticSegment>, ICopy<QuadraticSegment> {

        public ref float2 P0 => ref pts[0];
        public ref float2 P1 => ref pts[1];
        public ref float2 P2 => ref pts[2];

        private float2x3 pts;

        public EdgeColor Color { get; set; }

        public QuadraticSegment(float2 p0, float2 p1, float2 p2, EdgeColor color) {
            Color = color;
            pts = new float2x3(p0, p1, p2);
        }

        public QuadraticSegment(float2x3 pts, EdgeColor color) {
            Color = color;
            this.pts = pts;
        }

        public readonly QuadraticSegment Clone() => new QuadraticSegment(pts, Color);

        public void GetBounds(ref float4 points) {
            P0.PointBounds(ref points.x, ref points.y, ref points.z, ref points.w);
            P2.PointBounds(ref points.x, ref points.y, ref points.z, ref points.w);
            var bot = (P1 - P0) - (P2 - P1);

            if (bot.x != 0) {
                throw new NotImplementedException();
            }
            if (bot.y != 0) {
                throw new NotImplementedException();
            }
        }

        public float2 GetDirection(float t) => math.lerp(pts[1] - pts[0], pts[2] - pts[1], t);

        public float2 GetPoint(float t) => math.lerp(
                math.lerp(pts[0], pts[1], t),
                math.lerp(pts[1], pts[2], t),
                t
            );

        public SignedDistance GetSignedDistance(float2 origin, out float t) {
            var qa = P0 - origin;
            var ab = P1 - P0;
            var br = P0 + P2 - P1 - P1;
            var a = math.dot(br, br);
            var b = 3 * math.dot(ab, br);
            var c = 2 * math.dot(ab, ab) + math.dot(qa, br);
            var d = math.dot(qa, ab);

            var roots = new float3();
            var solutions = EquationSolver.SolveCubic(ref roots, a, b, c, d);

            var minDistance = MathExtensions.Cross(ab, qa).NonZeroSign() * math.length(qa);
            t = -math.dot(qa, ab) / math.dot(ab, ab);

            var distance = MathExtensions.Cross(P2 - P1, P2 - origin).NonZeroSign() * math.length(P2 - origin);
            if (math.abs(distance) < math.abs(minDistance)) {
                minDistance = distance;
                t = math.dot(origin - P1, P2 - P1) / math.dot(P2 - P1, P2 - P1);
            }

            for (int i = 0; i < solutions; i++) {
                if (roots[i] > 0 && roots[i] < 1) {
                    var endPoint = P0 + (2 * roots[i] * ab) + (roots[i] * roots[i] * br);

                    if (math.abs(distance) <= math.abs(minDistance)) {
                        minDistance = distance;
                        t = roots[i];
                    }
                }
            }

            if (t >= 0 && t <= 1) {
                return new SignedDistance(minDistance, 0);
            }

            if (t < 0.5f) {
                return new SignedDistance(minDistance, math.abs(math.dot(math.normalize(ab), math.normalize(qa))));
            } else {
                return new SignedDistance(minDistance, math.abs(math.dot(math.normalize(P2 - P1), math.normalize(P2 - origin))));
            }
        }

        public void MoveEndPoint(float2 dst) {
            var origEDir = P2 - P1;
            var origP1 = P1;

            P1 += MathExtensions.Cross(P2 - P1, dst - P2) / MathExtensions.Cross(P2 - P1, P0 - P1) * (P0 - P1);
            P2 = dst;
            if (math.dot(origEDir, P2 - P1) < 0) {
                P1 = origP1;
            }
        }

        public void MoveStartPoint(float2 dst) {
            var origSDir = P0 - P1;
            var origP1 = P1;

            P1 += MathExtensions.Cross(P0 - P1, dst - P0) / MathExtensions.Cross(P0 - P1, P2 - P1) * (P2 - P1);
            P2 = dst;
            if (math.dot(origSDir, P0 - P1) < 0) {
                P1 = origP1;
            }
        }

        public void SplitInThirds(out QuadraticSegment p1, out QuadraticSegment p2, out QuadraticSegment p3) {
            p1 = new QuadraticSegment(P0, math.lerp(P0, P1, 1 / 3f), GetPoint(1 / 3f), Color);
            p2 = new QuadraticSegment(GetPoint(1 / 3f), math.lerp(math.lerp(P0, P1, 5 / 9f), math.lerp(P1, P2, 4 / 9f), 0.5f), GetPoint(2 / 3f), Color);
            p3 = new QuadraticSegment(GetPoint(2 / 3f), math.lerp(P1, P2, 2 / 3f), P2, Color);
        }
    }
}
