using System.Collections.Generic;
using Unity.Mathematics;

namespace InitialPrefabs.Msdf {
    public class Shape {
        public readonly List<Contour> Contours = new List<Contour>();
        public bool InverseYAxis;

        public bool Validate() {
            foreach (Contour contour in Contours) {
                if (contour.Edges.Count > 0) {
                    float2 corner = contour.Edges[contour.Edges.Count - 1].GetPoint(1);
                    foreach (ISegment edge in contour.Edges) {
                        if (edge == null || math.all(edge.GetPoint(0) != corner)) {
                            return false;
                        }
                        corner = edge.GetPoint(1);
                    }
                }
            }
            return true;
        }

        public void Normalize() {
            foreach (Contour contour in Contours) {
                if (contour.Edges.Count == 1) {
                    ISegment edge = contour.Edges[0];
                    if (edge is IDivider<ISegment> divider) {
                        divider.SplitInThirds(out ISegment e1, out ISegment e2, out ISegment e3);
                        contour.Edges.Clear();
                        contour.Edges.Add(e1);
                        contour.Edges.Add(e2);
                        contour.Edges.Add(e3);
                    }
                }
            }
        }

        public void GetBounds(ref float4 bounds) {
            foreach (Contour contour in Contours) {
                contour.GetBounds(ref bounds);
            }
        }
    }
}
