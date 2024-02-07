using Unity.Mathematics;

namespace InitialPrefabs.Msdf {
    public struct LineSegment : ISegment, ICopy<LineSegment> {
        public float2 P0;
        public float2 P1;

        public EdgeColor Color { get; set; }

        public LineSegment(float2 p0, float2 p1, EdgeColor color) {
            Color = color;
            P0 = p0;
            P1 = p1;
        }

        public readonly LineSegment Clone() => new LineSegment(P0, P1, Color);

        public readonly void GetBounds(ref float4 points) {
            P0.PointBounds(ref points.x, ref points.y, ref points.z, ref points.w);
            P1.PointBounds(ref points.x, ref points.y, ref points.z, ref points.w);
        }

        public readonly float2 GetDirection(float t) => P1 - P0;

        public readonly float2 GetPoint(float t) => math.lerp(P0, P1, t);

        public readonly SignedDistance GetSignedDistance(float2 origin, out float t) {
            var aq = origin - P0;
            var ab = P1 - P0;
            t = math.dot(aq, ab);
            var eq = (t > 0.5f ? P1 : P0) - origin;
            var endPtDistance = math.length(eq);

            if (t > 0 && t < 1) {
                var orthoDistance = math.dot(ab.GetOrthogonal(false, false), aq);
                if (math.abs(orthoDistance) < endPtDistance) {
                    return new SignedDistance {
                        Distance = orthoDistance,
                        Dot = 0
                    };
                }
            }
            return new SignedDistance {
                Distance = MathExtensions.Cross(aq, ab).NonZeroSign() * endPtDistance,
                Dot = math.abs(math.dot(math.normalize(ab), math.normalize(eq)))
            };
        }

        public void MoveStartPoint(float2 dst) => P0 = dst;

        public void MoveEndPoint(float2 dst) => P1 = dst;

        public void SplitInThirds(out ISegment p1, out ISegment p2, out ISegment p3) {
            p1 = new LineSegment(P0, P1, Color);
            p2 = new LineSegment(GetPoint(1 / 3f), GetPoint(2 / 3f), Color);
            p3 = new LineSegment(GetPoint(2 / 3f), P1, Color);
        }
    }
}
