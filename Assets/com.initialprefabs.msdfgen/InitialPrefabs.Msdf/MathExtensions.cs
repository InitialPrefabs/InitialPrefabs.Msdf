using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace InitialPrefabs.Msdf {
    public static class MathExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 GetOrthogonal(this float2 v, bool polarity, bool allowZero) {
            float len = math.length(v);
            if (len <= 0) {
                return polarity ? new float2(0, allowZero ? 0 : 1) : new float2(0, -(allowZero ? 0 : 1));
            }
            return (polarity ? new float2(-v.y, v.x) : new float2(v.y, -v.x)) / len;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PointBounds(this float2 p, ref float l, ref float b, ref float r, ref float t) {
            if (p.x < l) l = p.x;
            if (p.y < b) b = p.y;
            if (p.x > r) r = p.x;
            if (p.y > t) t = p.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NonZeroSign(this float d) => Math.Sign(d) == 0 ? 1 : Math.Sign(d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cross(this float2 a, float2 b) => a.x * b.y - a.y * b.x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Median(float a, float b, float c) {
            return math.max(math.min(a, b), Math.Min(Math.Max(a, b), c));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Median(int a, int b, int c) {
            return math.max(math.min(a, b), Math.Min(Math.Max(a, b), c));
        }
    }
}
