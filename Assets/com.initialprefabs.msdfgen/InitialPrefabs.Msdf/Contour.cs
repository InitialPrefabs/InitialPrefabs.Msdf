using System.Collections.Generic;
using Unity.Mathematics;

namespace InitialPrefabs.Msdf {
    public class Contour {
        public readonly List<ISegment> Edges;

        public Contour() {
            Edges = new List<ISegment>();
        }

        public void GetBounds(ref float4 bounds) {
            for (int i = 0; i < Edges.Count; i++) {
            }
        }
    }
}
