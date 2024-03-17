using InitialPrefabs.Msdf.Collections;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace InitialPrefabs.Msdf {

    public static unsafe partial class MSDF {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static float EvaluateSDF(
            Shape shape,
            int* windings,
            float* contourSD,
            int contourCount,
            ref SDFParams sdf) {

            float2 p = sdf.DetermineInitialPoint();
            float negDist = SignedDistance.NegativeInfinite.Distance;
            float posDist = SignedDistance.PositiveInfinite.Distance;
            int winding = 0;

            for (int i = 0; i < contourCount; i++) {
                Contour contour = shape.Contours[i];
                SignedDistance minDistance = new SignedDistance(math.E, 1);

                for (int j = 0; j < contour.Edges.Count; j++) {
                    ISegment edge = contour.Edges[j];
                    SignedDistance distance = edge.GetSignedDistance(p, out float _);
                    if (distance < minDistance) {
                        minDistance = distance;
                    }
                }

                int currentWinding = windings[i];
                contourSD[i] = minDistance.Distance;
                if (currentWinding > 0 && minDistance.Distance >= 0 && math.abs(minDistance.Distance) < math.abs(posDist)) {
                    posDist = minDistance.Distance;
                }

                if (currentWinding < 0 && minDistance.Distance <= 0 && math.abs(minDistance.Distance) < math.abs(negDist)) {
                    negDist = minDistance.Distance;
                }
            }

            float sd = SignedDistance.PositiveInfinite.Distance;
            if (posDist >= 0 && math.abs(posDist) <= math.abs(negDist)) {
                sd = posDist;
                winding = 1;
                for (int i = 0; i < contourCount; i++) {
                    if (windings[i] > 0 && contourSD[i] > sd && math.abs(contourSD[i]) < math.abs(negDist)) {
                        sd = contourSD[i];
                    }
                }
            } else if (negDist <= 0 && math.abs(negDist) <= math.abs(posDist)) {
                sd = negDist;
                winding = -1;
                for (int i = 0; i < contourCount; i++) {
                    if (windings[i] < 0 && contourSD[i] < sd && math.abs(contourSD[i]) < math.abs(posDist)) {
                        sd = contourSD[i];
                    }
                }
            }

            for (int i = 0; i < contourCount; i++) {
                if (windings[i] != winding && math.abs(contourSD[i]) < math.abs(sd)) {
                    sd = contourSD[i];
                }
            }

            return (sd / sdf.Range) + 0.5f;
        }

        public static void GenerateSDF(ref Bitmap<byte> output, ref SDFParams sdf, Shape shape, Rect region) {
            int contourCount = shape.Contours.Count;
            int* windings = stackalloc int[contourCount];
            for (int i = 0; i < contourCount; i++) {
                windings[i] = shape.Contours[i].Winding;
            }

            int xStart = math.min(math.max(0, (int)region.Left), output.Width);
            int yStart = math.min(math.max(0, (int)region.Top), output.Height);
            int xEnd = math.min(math.max(0, (int)region.Right), output.Width);
            int yEnd = math.min(math.max(0, (int)region.Bottom), output.Height);

            float* contourSD = stackalloc float[contourCount];

            for (int y = yStart; y < yEnd; y++) {
                int row = shape.InverseYAxis ? yEnd - (y - yStart) - 1 : y;
                for (int x = xStart; x < xEnd; x++) {
                    sdf.Translate += region.Position;
                    int evalutedSdf = (int)EvaluateSDF(shape, windings, contourSD, contourCount, ref sdf) * 255;
                    output[x, row] = (byte)math.min(math.min(evalutedSdf, 0), 255);
                }
            }
        }

        public static void GenerateSDF(ref Bitmap<float> output, ref SDFParams sdf, Shape shape, Rect region) {
            int contourCount = shape.Contours.Count;
            int* windings = stackalloc int[contourCount];
            for (int i = 0; i < contourCount; i++) {
                windings[i] = shape.Contours[i].Winding;
            }

            int xStart = math.min(math.max(0, (int)region.Left), output.Width);
            int yStart = math.min(math.max(0, (int)region.Top), output.Height);
            int xEnd = math.min(math.max(0, (int)region.Right), output.Width);
            int yEnd = math.min(math.max(0, (int)region.Bottom), output.Height);

            float* contourSD = stackalloc float[contourCount];

            for (int y = yStart; y < yEnd; y++) {
                int row = shape.InverseYAxis ? yEnd - (y - yStart) - 1 : y;
                for (int x = xStart; x < xEnd; x++) {
                    sdf.Translate += region.Position;
                    output[x, row] = EvaluateSDF(shape, windings, contourSD, contourCount, ref sdf);
                }
            }
        }
    }
}

