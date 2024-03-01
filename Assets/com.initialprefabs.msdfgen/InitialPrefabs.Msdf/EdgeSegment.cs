using Unity.Mathematics;

namespace InitialPrefabs.Msdf {

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
        void SplitInThirds(out ISegment p1, out ISegment p2, out ISegment p3);
    }

    public static partial class EdgeSegmentExtensions {

        public static void DistanceToPseudoDistance<T>(
            ref T segment,
            ref SignedDistance sd,
            float2 origin,
            float t) where T : struct, ISegment {

            if (t < 0) {
                float2 dir = math.normalize(segment.GetDirection(0));
                float2 aq = origin - segment.GetPoint(0);
                float ts = math.dot(aq, dir);

                if (ts < 0) {
                    float pseudoDistance = aq.Cross(dir);
                    if (math.abs(pseudoDistance) <= math.abs(sd.Distance)) {
                        sd = new SignedDistance {
                            Distance = pseudoDistance,
                            Dot = 0
                        };
                    }
                }
            } else if (t > 1) {
                float2 dir = math.normalize(segment.GetDirection(1));
                float2 bq = origin - segment.GetPoint(1);
                float ts = math.dot(bq, dir);

                if (ts > 0) {
                    float pseudoDistance = bq.Cross(dir);
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
