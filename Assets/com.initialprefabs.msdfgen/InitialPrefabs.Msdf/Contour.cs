using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace InitialPrefabs.Msdf {

    public class Contour {
        public readonly List<ISegment> Edges = new List<ISegment>();

        public int Winding {
            get {
                if (Edges.Count == 0) {
                    return 0;
                }
                float total = 0f;

                switch (Edges.Count) {
                    case 0:
                        return 0;
                    case 1: {
                            float2 a = Edges[0].GetPoint(0);
                            float2 b = Edges[0].GetPoint(1 / 3f);
                            float2 c = Edges[0].GetPoint(2 / 3f);

                            total += Shoelace(a, b);
                            total += Shoelace(b, c);
                            total += Shoelace(c, a);
                            break;
                        }
                    case 2: {
                            float2 a = Edges[0].GetPoint(0);
                            float2 b = Edges[0].GetPoint(0.5f);
                            float2 c = Edges[1].GetPoint(0);
                            float2 d = Edges[1].GetPoint(0.5f);

                            total += Shoelace(a, b);
                            total += Shoelace(b, c);
                            total += Shoelace(c, d);
                            total += Shoelace(d, a);
                            break;
                        }
                    default: {
                            float2 prev = Edges[^1].GetPoint(0);
                            for (int i = 0; i < Edges.Count; i++) {
                                float2 current = Edges[i].GetPoint(0);
                                total += Shoelace(prev, current);
                                prev = current;
                            }
                            break;
                        }

                }
                return (int)math.sign(total);
            }
        }

        public void GetBounds(ref float4 bounds) {
            for (int i = 0; i < Edges.Count; i++) {
                Edges[i].GetBounds(ref bounds);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Shoelace(float2 a, float2 b) {
            return (b.x - a.x) * (a.y + b.y);
        }
    }
}
