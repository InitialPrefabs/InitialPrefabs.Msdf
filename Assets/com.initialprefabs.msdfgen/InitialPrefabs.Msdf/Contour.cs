using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace InitialPrefabs.Msdf {

    public class Contour {
        public readonly List<ISegment> Edges;

        public int Winding {
            get {
                throw new NotImplementedException();
            }
        }

        public Contour() {
            Edges = new List<ISegment>();
        }

        public void GetBounds(ref float4 bounds) {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Shoelace(float2 a, float2 b) {
            return (b.x - a.x) * (a.y + b.y);
        }
    }
}
