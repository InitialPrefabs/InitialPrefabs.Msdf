using System;
using Unity.Mathematics;

namespace InitialPrefabs.Msdf {

    public interface ICopy<T> where T : struct, ISegment {
        ICopy<T> Clone();
    }

    public interface ISegment {

        EdgeColor Color { get; set; }

        float2 GetPoint(float t);
        float2 GetDirection(float t);
        SignedDistance GetSignedDistance(float2 origin, out float t);

        /// <summary>
        /// Gets all points within a bound.
        /// </summary>
        /// <param name="points">Stores the left, bottom, right, top coordinates.</param>
        void GetBounds(ref float4 points);
        void MoveStartPoint(float2 dst);
        void MoveEndPoint(float2 dst);
        void SplitInThirds(ref SignedDistance sd, float2 origin, float t);
    }

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
            MathExtensions.PointBounds(P0, ref points.x, ref points.y, ref points.z, ref points.w);
            MathExtensions.PointBounds(P1, ref points.x, ref points.y, ref points.z, ref points.w);
        }

        public readonly float2 GetDirection(float t) => P1 - P0;

        public readonly float2 GetPoint(float t) => math.lerp(P0, P1, t);

        public readonly SignedDistance GetSignedDistance(float2 origin, out float t) {
            throw new NotImplementedException();
        }

        public void MoveEndPoint(float2 dst) {
            throw new NotImplementedException();
        }

        public void MoveStartPoint(float2 dst) {
            throw new NotImplementedException();
        }

        public void SplitInThirds(ref SignedDistance sd, float2 origin, System.Single t) {
            throw new System.NotImplementedException();
        }

        ICopy<LineSegment> ICopy<LineSegment>.Clone() {
            throw new System.NotImplementedException();
        }
    }

    public static partial class EdgeSegmentExtensions {

        public static void DistanceToPseudoDistance<T>(
            ref T segment,
            ref SignedDistance sd,
            float2 origin,
            float t) where T : struct, ISegment {

            if (t < 0) {
                var dir = math.normalize(segment.GetDirection(0));
                var aq = origin - segment.GetPoint(0);
                var ts = math.dot(aq, dir);

                if (ts < 0) {
                    var pseudoDistance = aq.Cross(dir);
                    if (math.abs(pseudoDistance) <= math.abs(sd.Distance)) {
                        sd = new SignedDistance {
                            Distance = pseudoDistance,
                            Dot = 0
                        };
                    }
                }
            } else if (t > 1) {
                var dir = math.normalize(segment.GetDirection(1));
                var bq = origin - segment.GetPoint(1);
                var ts = math.dot(bq, dir);

                if (ts > 0) {
                    var pseudoDistance = bq.Cross(dir);
                    if (math.abs(pseudoDistance) <= math.abs(sd.Distance)) {
                        sd = new SignedDistance {
                            Distance = pseudoDistance,
                            Dot = 0
                        };
                    }
                }
            }
        }
    }
}
