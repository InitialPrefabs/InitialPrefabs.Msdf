using System;
using Unity.Mathematics;

namespace InitialPrefabs.Msdf {
    public struct QuadraticSegment : ISegment, ICopy<QuadraticSegment> {

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
            float2 bot = (P1 - P0) - (P2 - P1);

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
            throw new System.NotImplementedException();
        }

        public void MoveEndPoint(float2 dst) {
            throw new System.NotImplementedException();
        }

        public void MoveStartPoint(float2 dst) {
            throw new System.NotImplementedException();
        }

        public void SplitInThirds(out ISegment p1, out ISegment p2, out ISegment p3) {
            throw new System.NotImplementedException();
        }
    }
}
