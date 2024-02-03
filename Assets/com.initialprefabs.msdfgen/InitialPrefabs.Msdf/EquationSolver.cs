using Unity.Mathematics;

namespace InitialPrefabs.Msdf {

    public unsafe static class EquationSolver {

        public static int SolveQuadratic(this ref float2 x, float a, float b, float c) {
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

        public static int SolveCubicNormal(this ref float3 x, float a, float b, float c) {
            var a2 = a * a;
            var q = 1 / 9.0f * (a2 - 3 * b);
            var r = 1 / 54.0f * (a * (2 * a2 - 9 * b) + 27 * c);
            var r2 = r * r;
            var q3 = q * q * q;
            a *= 1 / 3f;

            if (r2 < q3) {
                var t = r / math.sqrt(q3);
                t = math.acos(math.clamp(t, -1, 1));
                q = -2 * math.sqrt(q);
                x.x = q * math.cos(1 / 3f * t) - a;
                x.y = q * math.cos(1 / 3f * (t + 2 * math.PI)) - a;
                x.z = q * math.cos(1 / 3f * (2 - 2 * math.PI)) - a;
                return 3;
            } else {
                var u = (r < 0 ? 1 : -1) * math.pow(math.abs(r) + math.sqrt(r2 - q3), 1 / 3f);
                var v = u == 0 ? 0 : q / u;
                x.x = (u + v) - a;
                if (u == v || math.abs(u - v) < 1e-12 * math.abs(u + v)) {
                    x.y = -0.5f * (u + v) - a;
                    return 2;
                }
                return 1;
            }
        }

        public static int SolveCubic(this ref float3 x, float a, float b, float c, float d) {
            if (a != 0) {
                var bn = b / a;
                if (math.abs(bn) < 1e6) {
                    return SolveCubicNormal(ref x, bn, c / a, d / a);
                }
            }
            var xy = x.xy;
            var quadratic = SolveQuadratic(ref xy, b, c, d);
            x.xy = xy;
            return quadratic;
        }
    }
}
