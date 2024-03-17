using InitialPrefabs.Msdf.Collections;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace InitialPrefabs.Msdf {

    public unsafe static partial class MSDF {

        internal struct Clash {
            public int X;
            public int Y;
        }

        internal struct EdgePoint {
            public SignedDistance MinDistance;
            public ISegment NearEdge;
            public float NearParam;
        }

        public ref struct SDFParams {

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float2 DetermineInitialPoint() => (new float2(xy) + 0.5f) / (Scale - Translate);

            private readonly int2 xy;
            public readonly float Range;
            public readonly float2 Scale;
            public float2 Translate;

            public SDFParams(int x, int y, float range, float2 scale, float2 translate) {
                xy = new int2(x, y);
                Range = range;
                Scale = scale;
                Translate = translate;
            }
        }

        public static bool PixelClash(Color a, Color b, float threshold) {
            bool aIn = ((a.r > .5f) ? 1 : 0) + ((a.g > .5f) ? 1 : 0) + ((a.b > .5f) ? 1 : 0) >= 2;
            bool bIn = ((b.r > .5f) ? 1 : 0) + ((b.g > .5f) ? 1 : 0) + ((b.b > .5f) ? 1 : 0) >= 2;
            if (aIn != bIn) return false;

            if ((a.r > .5f && a.g > .5f && a.b > .5f) ||
                (a.r < .5f && a.g < .5f && a.b < .5f) ||
                (b.r > .5f && b.g > .5f && b.b > .5f) ||
                (b.r < .5f && b.g < .5f && b.b < .5f)) {
                return false;
            }

            float aa, ab, ba, bb, ac, bc;

            if ((a.r > .5f) != (b.r > .5f) &&
                (a.r < .5f) != (b.r < .5f)) {
                aa = a.r;
                ba = b.r;
                if ((a.g > .5f) != (b.g > .5f) &&
                    (a.g < .5f) != (b.g < .5f)) {
                    ab = a.g;
                    bb = b.g;
                    ac = a.b;
                    bc = b.b;
                } else if ((a.b > .5f) != (b.b > .5f) &&
                    (a.b < .5f) != (b.b < .5f)) {
                    ab = a.b;
                    bb = b.b;
                    ac = a.g;
                    bc = b.g;
                } else
                    return false;
            } else if ((a.g > .5f) != (b.g > .5f) &&
                (a.g < .5f) != (b.g < .5f) &&
                (a.b > .5f) != (b.b > .5f) &&
                (a.b < .5f) != (b.b < .5f)) {
                aa = a.g;
                ba = b.g;
                ab = a.b;
                bb = b.b;
                ac = a.r;
                bc = b.r;
            } else {
                return false;
            }
            return (math.abs(aa - ba) >= threshold) && (math.abs(ab - bb) >= threshold) && math.abs(ac - .5f) >= math.abs(bc - .5f);
        }

        public static bool PixelClash(Color32 a, Color32 b, float threshold) {
            Color af = a;
            Color bf = b;
            return PixelClash(af, bf, threshold);
        }

        public static void CorrectErrors(Bitmap<Color> output, RectInt region, float2 threshold) {
            List<Clash> clashes = new List<Clash>();
            int w = output.Width;
            int h = output.Height;

            int xStart = math.min(math.max(0, region.Left), output.Width);
            int yStart = math.min(math.max(0, region.Top), output.Height);
            int xEnd = math.min(math.max(0, region.Right), output.Width);
            int yEnd = math.min(math.max(0, region.Bottom), output.Height);

            for (int y = yStart; y < yEnd; y++) {
                for (int x = xStart; x < xEnd; x++) {
                    if ((x > 0 && PixelClash(output[x, y], output[x - 1, y], threshold.x)) ||
                        (x < w - 1 && PixelClash(output[x, y], output[x + 1, y], threshold.x)) ||
                        (y > 0 && PixelClash(output[x, y], output[x, y - 1], threshold.y)) ||
                        (y < h - 1 && PixelClash(output[x, y], output[x, y + 1], threshold.y))) {

                        clashes.Add(new Clash { X = x, Y = y });
                    }
                }
            }

            for (int i = 0; i < clashes.Count; i++) {
                Color pixel = output[clashes[i].X, clashes[i].Y];
                float med = MathExtensions.Median(pixel.r, pixel.g, pixel.b);
                pixel.r = med;
                pixel.g = med;
                pixel.b = med;
                output[clashes[i].X, clashes[i].Y] = pixel;
            }
        }

        public static void CorrectErrors(Bitmap<Color32> output, RectInt region, float2 threshold) {
            List<Clash> clashes = new List<Clash>();
            int w = output.Width;
            int h = output.Height;

            int xStart = math.min(math.max(0, region.Left), output.Width);
            int yStart = math.min(math.max(0, region.Top), output.Height);
            int xEnd = math.min(math.max(0, region.Right), output.Width);
            int yEnd = math.min(math.max(0, region.Bottom), output.Height);

            for (int y = yStart; y < yEnd; y++) {
                for (int x = xStart; x < xEnd; x++) {
                    if ((x > 0 && PixelClash(output[x, y], output[x - 1, y], threshold.x)) ||
                        (x < w - 1 && PixelClash(output[x, y], output[x + 1, y], threshold.x)) ||
                        (y > 0 && PixelClash(output[x, y], output[x, y - 1], threshold.y)) ||
                        (y < h - 1 && PixelClash(output[x, y], output[x, y + 1], threshold.y))) {

                        clashes.Add(new Clash { X = x, Y = y });
                    }
                }
            }

            for (int i = 0; i < clashes.Count; i++) {
                Clash clash = clashes[i];
                Color32 pixel = output[clash.X, clash.Y];
                int med = MathExtensions.Median(pixel.r, pixel.g, pixel.b);
                pixel.r = (byte)med;
                pixel.g = (byte)med;
                pixel.b = (byte)med;
                output[clash.X, clash.Y] = pixel;
            }
        }
    }
}
