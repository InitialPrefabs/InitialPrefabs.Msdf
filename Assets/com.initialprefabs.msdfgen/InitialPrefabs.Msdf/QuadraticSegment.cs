using Unity.Mathematics;

namespace InitialPrefabs.Msdf {

    public struct QuadraticSegment : ISegment, ICopy<QuadraticSegment>, IDivider<QuadraticSegment> {

        public readonly ref float2 P0 => ref pts[0];
        public readonly ref float2 P1 => ref pts[1];
        public readonly ref float2 P2 => ref pts[2];

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

        public readonly QuadraticSegment Clone() => this;

        public readonly void GetBounds(ref float4 points) {
            P0.PointBounds(ref points.x, ref points.y, ref points.z, ref points.w);
            P2.PointBounds(ref points.x, ref points.y, ref points.z, ref points.w);
            float2 bot = P1 - P0 - (P2 - P1);

            if (bot.x != 0) {
                float param = (P1.x - P0.x) / bot.x;
                if (param > 0 && param < 1) {
                    GetPoint(param).PointBounds(ref points.x, ref points.y, ref points.z, ref points.w);
                }
            }
            if (bot.y != 0) {
                float param = (P1.y - P0.y) / bot.y;
                if (param > 0 && param < 1) {
                    GetPoint(param).PointBounds(ref points.x, ref points.y, ref points.z, ref points.w);
                }
            }
        }

        public readonly float2 GetDirection(float t) => math.lerp(pts[1] - pts[0], pts[2] - pts[1], t);

        public readonly float2 GetPoint(float t) => math.lerp(
                math.lerp(pts[0], pts[1], t),
                math.lerp(pts[1], pts[2], t),
                t
            );

        public SignedDistance GetSignedDistance(float2 origin, out float t) {
            float2 qa = P0 - origin;
            float2 ab = P1 - P0;
            float2 br = P0 + P2 - P1 - P1;

            float a = math.dot(br, br);
            float b = 3 * math.dot(ab, br);
            float c = 2 * math.dot(ab, ab) + math.dot(qa, br);
            float d = math.dot(qa, ab);

            float3 roots = new float3();
            int solutions = EquationSolver.SolveCubic(ref roots, a, b, c, d);
            float minDistance = ab.Cross(qa).NonZeroSign() * math.length(qa);
            t = -math.dot(qa, ab) / math.dot(ab, ab);
            {
                float distance = (P2 - P1).Cross(P2 - origin).NonZeroSign() * math.length(P2 - origin);
                if (math.abs(distance) < math.abs(minDistance)) {
                    minDistance = distance;
                    t = math.dot(origin - P1, P2 - P1) / math.dot(P2 - P1, P2 - P1);
                }
            }

            for (int i = 0; i < solutions; i++) {
                if (roots[i] > 0 && roots[i] < 1) {
                    float2 endPoint = P0 + (2 * roots[i] * ab) + roots[i] * roots[i] * br;
                    float distance = (P2 - P0).Cross(endPoint - origin) * math.length(endPoint - origin);

                    if (math.abs(distance) <= math.abs(minDistance)) {
                        minDistance = distance;
                        t = roots[i];
                    }
                }
            }

            if (t >= 0 && t <= 1) {
                return new SignedDistance(minDistance, 0);
            } else if (t < 0.5f) {
                return new SignedDistance(minDistance, math.abs(math.dot(math.normalize(ab), math.normalize(qa))));
            } else {
                return new SignedDistance(minDistance, math.abs(math.dot(math.normalize(P2 - P1), math.normalize(P2 - origin))));
            }
        }

        public void MoveEndPoint(float2 dst) {
            float2 origEDir = P2 - P1;
            float2 origP1 = P1;

            P1 += (P2 - P1).Cross(dst - P2) / (P2 - P1).Cross(P0 - P1) * (P0 - P1);
            P2 = dst;

            if (math.dot(origEDir, P2 - P1) < 0) {
                P1 = origP1;
            }
        }

        public void MoveStartPoint(float2 dst) {
            float2 origSDir = P0 - P1;
            float2 origP1 = P1;

            P1 += (P0 - P1).Cross(dst - P0) / (P0 - P1).Cross(P2 - P1) * (P2 - P1);
            P0 = dst;

            if (math.dot(origSDir, P0 - P1) < 0) {
                P1 = origP1;
            }
        }

        public void SplitInThirds(out QuadraticSegment p1, out QuadraticSegment p2, out QuadraticSegment p3) {
            p1 = new QuadraticSegment(P0, math.lerp(P0, P1, 1 / 3f), GetPoint(1 / 3f), Color);
            p2 = new QuadraticSegment(GetPoint(1 / 3f), math.lerp(
                    math.lerp(P0, P1, 5 / 9f),
                    math.lerp(P1, P2, 4 / 9f),
                    0.5f
                ),
                GetPoint(2 / 3f),
                Color);
            p3 = new QuadraticSegment(GetPoint(2 / 3f), math.lerp(P1, P2, 2 / 3f), P2, Color);
        }
    }
}
