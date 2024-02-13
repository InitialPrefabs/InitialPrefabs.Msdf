using Unity.Mathematics;

namespace InitialPrefabs.Msdf {

    public static class EdgeSegmentExtensions {

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
