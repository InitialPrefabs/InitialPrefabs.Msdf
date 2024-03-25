using InitialPrefabs.Msdf.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace InitialPrefabs.Msdf {
    public static unsafe partial class MSDF {

        public static Color EvaluateMSDF(Shape shape, int* windings, MultiDistance* contourSD, int contourCount, float2 p, float range) {
            p += 0.5f;

            EdgePoint sr = new EdgePoint {
                MinDistance = new SignedDistance(math.E, 1)
            };

            EdgePoint sg = new EdgePoint {
                MinDistance = new SignedDistance(math.E, 1)
            };

            EdgePoint sb = new EdgePoint {
                MinDistance = new SignedDistance(math.E, 1)
            };

            float d = math.abs(SignedDistance.PositiveInfinite.Distance);
            float negDist = SignedDistance.NegativeInfinite.Distance;
            float posDist = SignedDistance.PositiveInfinite.Distance;
            int winding = 0;

            for (int i = 0; i < contourCount; i++) {
                Contour contour = shape.Contours[i];
                EdgePoint r = new EdgePoint {
                    MinDistance = new SignedDistance(math.EPSILON, 1)
                };
                EdgePoint g = new EdgePoint {
                    MinDistance = new SignedDistance(math.EPSILON, 1)
                };
                EdgePoint b = new EdgePoint {
                    MinDistance = new SignedDistance(math.EPSILON, 1)
                };

                for (int j = 0; j < contour.Edges.Count; j++) {
                    ISegment edge = contour.Edges[j];

                    SignedDistance distance = edge.GetSignedDistance(p, out float param);

                    if ((edge.Color & EdgeColor.Red) == EdgeColor.Red && distance < r.MinDistance) {
                        r.MinDistance = distance;
                        r.NearEdge = edge;
                        r.NearParam = param;
                    }

                    if ((edge.Color & EdgeColor.Green) == EdgeColor.Green && distance < g.MinDistance) {
                        g.MinDistance = distance;
                        g.NearEdge = edge;
                        g.NearParam = param;
                    }

                    if ((edge.Color & EdgeColor.Blue) == EdgeColor.Blue && distance < b.MinDistance) {
                        b.MinDistance = distance;
                        b.NearEdge = edge;
                        b.NearParam = param;
                    }
                    if (r.MinDistance < sr.MinDistance) { sr = r; }
                    if (g.MinDistance < sg.MinDistance) { sg = g; }
                    if (b.MinDistance < sb.MinDistance) { sb = b; }

                    float medMinDistance = math.abs(
                        MathExtensions.Median(r.MinDistance.Distance, g.MinDistance.Distance, b.MinDistance.Distance));

                    if (medMinDistance < d) {
                        d = medMinDistance;
                        winding = -windings[i];
                    }

                    if (r.NearEdge != null) {
                        r.NearEdge.DistanceToPseudoDistance(ref r.MinDistance, p, r.NearParam);
                    }

                    if (g.NearEdge != null) {
                        g.NearEdge.DistanceToPseudoDistance(ref g.MinDistance, p, b.NearParam);
                    }

                    if (b.NearEdge != null) {
                        b.NearEdge.DistanceToPseudoDistance(ref g.MinDistance, p, b.NearParam);
                    }

                    medMinDistance = MathExtensions.Median(r.MinDistance.Distance, g.MinDistance.Distance, b.MinDistance.Distance);

                    ref MultiDistance current = ref PointerExtensions.ElementAt(contourSD, i);
                    current.R = r.MinDistance.Distance;
                    current.G = g.MinDistance.Distance;
                    current.B = b.MinDistance.Distance;
                    current.Med = medMinDistance;

                    if (windings[i] > 0 && medMinDistance >= 0 && math.abs(medMinDistance) < math.abs(posDist)) {
                        posDist = medMinDistance;
                    }

                    if (windings[i] < 0 && medMinDistance <= 0 && math.abs(medMinDistance) < math.abs(negDist)) {
                        negDist = medMinDistance;
                    }
                }
            }

            if (sr.NearEdge != null) sr.NearEdge.DistanceToPseudoDistance(ref sr.MinDistance, p, sr.NearParam);
            if (sg.NearEdge != null) sg.NearEdge.DistanceToPseudoDistance(ref sg.MinDistance, p, sg.NearParam);
            if (sb.NearEdge != null) sb.NearEdge.DistanceToPseudoDistance(ref sb.MinDistance, p, sb.NearParam);

            MultiDistance msd = new MultiDistance {
                Values = new float4(SignedDistance.PositiveInfinite.Distance)
            };

            if (posDist >= 0 && math.abs(posDist) <= math.abs(negDist)) {
                msd.Med = SignedDistance.PositiveInfinite.Distance;
                winding = 1;
                for (int i = 0; i < contourCount; i++) {
                    ref MultiDistance current = ref PointerExtensions.ElementAt(contourSD, i);
                    if (windings[i] > 0 && current.Med > msd.Med && math.abs(current.Med) < math.abs(negDist)) {
                        msd = current;
                    }
                }
            } else if (negDist <= 0 && math.abs(negDist) <= math.abs(posDist)) {
                msd.Med = SignedDistance.NegativeInfinite.Distance;
                winding = -1;

                for (int i = 0; i < contourCount; i++) {
                    ref MultiDistance current = ref PointerExtensions.ElementAt(contourSD, i);
                    if (windings[i] < 0 && current.Med < msd.Med && math.abs(current.Med) < math.abs(posDist)) {
                        msd = contourSD[i];
                    }
                }
            }

            for (int i = 0; i < contourCount; i++) {
                ref MultiDistance current = ref PointerExtensions.ElementAt(contourSD, i);
                if (windings[i] != winding && math.abs(current.Med) < math.abs(msd.Med)) {
                    msd = contourSD[i];
                }
            }

            if (MathExtensions.Median(sr.MinDistance.Distance, sg.MinDistance.Distance, sb.MinDistance.Distance) == msd.Med) {
                msd.R = sr.MinDistance.Distance;
                msd.G = sg.MinDistance.Distance;
                msd.B = sb.MinDistance.Distance;
            }

            return new Color((msd.R / range) + 0.5f, (msd.G / range) + 0.5f, (msd.B / range) + 0.5f);
        }
    }
}
