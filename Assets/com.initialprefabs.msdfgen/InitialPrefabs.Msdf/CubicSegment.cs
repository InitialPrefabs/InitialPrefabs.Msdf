using System;
using Unity.Mathematics;

namespace InitialPrefabs.Msdf {

    public struct CubicSegment : ISegment, IDivider<CubicSegment>, ICopy<CubicSegment> {

        public ref float2 P0 => ref pts[0];
        public ref float2 P1 => ref pts[1];
        public ref float2 P2 => ref pts[2];
        public ref float2 P3 => ref pts[3];

        private float2x4 pts;

        public EdgeColor Color { get; set; }

        public CubicSegment(float2 p0, float2 p1, float2 p2, float2 p3, EdgeColor color) {
            pts = new float2x4(p0, p1, p2, p3);
            Color = color;
        }

        public CubicSegment Clone() => this;

        public void GetBounds(ref float4 points) {
            P0.PointBounds(ref points.x, ref points.y, ref points.z, ref points.w);
            P3.PointBounds(ref points.x, ref points.y, ref points.z, ref points.w);

            float2 a0 = P1 - P0;
            float2 a1 = 2 * (P2 - P1 - a0);
            float2 a2 = P3 - 3 * P2 + 3 * P1 - P0;

            float2 roots = new float2();
            Int32 solutions = roots.SolveQuadratic(a2.x, a1.x, a0.x);

            for (int i = 0; i < solutions; i++) {
                if (roots[i] > 0 && roots[i] < 1) {
                    GetPoint(roots[i]).PointBounds(ref points.x, ref points.y, ref points.z, ref points.w);
                }
            }

            solutions = roots.SolveQuadratic(a2.y, a1.y, a0.y);
            for (int i = 0; i < solutions; i++) {
                if (roots[i] > 0 && roots[i] < 1) {
                    GetPoint(roots[i]).PointBounds(ref points.x, ref points.y, ref points.z, ref points.w);
                }
            }
        }

        public float2 GetDirection(float t) {
            float2 tangent = math.lerp(
                math.lerp(P1 - P0, P2 - P1, t),
                math.lerp(P2 - P1, P3 - P2, t),
                t
            );

            if (math.all(tangent <= new float2(math.EPSILON))) {
                if (t == 0) return P2 - P0;
                if (t == 1) return P3 - P1;
            }

            return tangent;
        }

        public float2 GetPoint(float t) {
            float2 p12 = math.lerp(P1, P2, t);
            return math.lerp(
                math.lerp(math.lerp(P0, P1, t), P1, t),
                math.lerp(p12, math.lerp(P2, P3, t), t),
                t
            );
        }

        public SignedDistance GetSignedDistance(float2 origin, out float t) {
            float2 qa = P0 - origin;
            float2 ab = P1 - P0;
            float2 br = P2 - P1 - ab;
            float2 _as = (P3 - P2) - (P2 - P1) - br;

            float2 epDir = GetDirection(0);
            Single minDistance = MathExtensions.Cross(epDir, qa).NonZeroSign() * math.length(qa);
            t = -math.dot(qa, epDir) / math.dot(epDir, epDir);

            {
                epDir = GetDirection(1);
                Single distance = MathExtensions.Cross(epDir, P3 - origin).NonZeroSign() * math.length(P3 - origin);

                if (Math.Abs(distance) < Math.Abs(minDistance)) {
                    minDistance = distance;
                    t = math.dot(origin + epDir - P3, epDir) / math.dot(epDir, epDir);
                }
            }

            for (int i = 0; i < 4; i++) {
                Single _t = (float)i / 4;
                int step = 0;
                while (true) {
                    float2 qpt = GetPoint(_t) - origin;
                    Single distance = MathExtensions.Cross(GetDirection(_t), qpt).NonZeroSign() * math.length(qpt);

                    if (math.abs(distance) < math.abs(minDistance)) {
                        minDistance = distance;
                        t = _t;
                    }

                    if (step == 4) break;

                    float2 d1 = (3 * _as * (float)(t * t)) + (6 * br * (float)t) + (3 * ab);
                    float2 d2 = 6 * _as * (float)t + 6 * br;
                    _t -= math.dot(qpt, d1) / (math.dot(d1, d1) + math.dot(qpt, d2));
                    if (t < 0 || t > 1) break;
                    step++;
                }
            }

            if (t >= 0 && t <= 1) {
                return new SignedDistance(minDistance, 0);
            }
            if (t < 0.5) {
                return new SignedDistance(
                    minDistance,
                    math.abs(math.dot(math.normalize(GetDirection(0)), math.normalize(qa)))
                );
            } else {
                return new SignedDistance(
                    minDistance,
                    math.abs(math.dot(math.normalize(GetDirection(1)), math.normalize(P3 - origin)))
                );
            }
        }

        public void MoveEndPoint(float2 dst) {
            P2 += dst - P3;
            P3 = dst;
        }

        public void MoveStartPoint(float2 dst) {
            P1 += dst - P0;
            P0 = dst;
        }

        public void SplitInThirds(out CubicSegment p1, out CubicSegment p2, out CubicSegment p3) {
            p1 = new CubicSegment(
                P0,
                P0.Equals(P1) ? P0 : math.lerp(P0, P1, 1 / 3f),
                math.lerp(
                    math.lerp(P0, P1, 1 / 3f),
                    math.lerp(P1, P2, 1 / 3f),
                    1 / 3f
                ),
                GetPoint(1 / 3f),
                Color
            );
            p2 = new CubicSegment(
                GetPoint(1 / 3f),
                math.lerp(
                    math.lerp(
                        math.lerp(P0, P1, 1 / 3f),
                        math.lerp(P1, P2, 1 / 3f),
                        1 / 3f
                    ),
                    math.lerp(
                        math.lerp(P1, P2, 1 / 3f),
                        math.lerp(P2, P3, 1 / 3f),
                        1 / 3f
                    ),
                    2 / 3f
                ),
                math.lerp(
                    math.lerp(
                        math.lerp(P0, P1, 2 / 3f),
                        math.lerp(P1, P2, 2 / 3f),
                        2 / 3f
                    ),
                    math.lerp(
                        math.lerp(P1, P2, 2 / 3f),
                        math.lerp(P2, P3, 2 / 3f),
                        2 / 3f
                    ),
                    1 / 3f
                ),
                GetPoint(2 / 3f),
                Color
            );
            p3 = new CubicSegment(
                GetPoint(2 / 3f),
                math.lerp(
                    math.lerp(P1, P2, 2 / 3f),
                    math.lerp(P2, P3, 2 / 3f),
                    2 / 3f
                ),
                P2.Equals(P3) ? P3 : math.lerp(P2, P3, 2 / 3f),
                P3,
                Color
            );
        }
    }
}
