using Unity.Mathematics;

namespace InitialPrefabs.Msdf {

    public static class EdgeSegmentExtensions {

        public static void DistanceToPseudoDistance<T>(
            this T segment,
            ref SignedDistance sd,
            float2 origin,
            float t) where T : class, ISegment {

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
