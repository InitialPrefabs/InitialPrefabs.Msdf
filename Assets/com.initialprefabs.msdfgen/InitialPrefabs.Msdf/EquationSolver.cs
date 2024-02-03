using System;
using Unity.Mathematics;

namespace InitialPrefabs.Msdf {

    public static class EquationSolver {

        public static int SolveQuadratic(ref float2 x, float a, float b, float c) {
            if (a == 0 || math.abs(b) > 1e12F * math.abs(a)) {
                if (b == 0) {
                    if (c == 0) {
                        return -1;
                    }
                    return 0;
                }
                x.x = -c / b;
                return 1;
            }

            var dsrc = b * b - 4 * a * c;
            if (dsrc > 0) {
                dsrc = math.sqrt(dsrc);
                x.x = (-b + dsrc) / (2 * a);
                x.y = (-b - dsrc) / (2 * a);
                return 2;
            } else if (dsrc == 0) {
                x.x = -b / (2 * a);
                return 1;
            }
            return 0;
        }

        public static int SolveCubicNormal(float3 x, float a, float b, float c) => throw new NotImplementedException();

        public static int SolveCubic(float3 x, float a, float b, float c, float d) => throw new NotImplementedException();
    }
}
