using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace InitialPrefabs.Msdf {

    public class Shape {
    }

    public static class MSDF {

        public static readonly EdgeColor[] SwitchColors = { EdgeColor.Cyan, EdgeColor.Magenta, EdgeColor.Yellow };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCorner(float2 aDir, float2 bDir, float crossThreshold) => 
            math.dot(aDir, bDir) <= 0 || math.abs(aDir.Cross(bDir)) > crossThreshold;

        public static void SwitchColor(ref EdgeColor color, ref ulong seed, EdgeColor banned) {
            EdgeColor combined = color & banned;

            if (combined == EdgeColor.Red || combined == EdgeColor.Green || combined == EdgeColor.Blue) {
                color = combined ^ EdgeColor.White;
                return;
            }

            if (color == EdgeColor.Black || color == EdgeColor.White) {
                color = SwitchColors[seed % 3];
                seed /= 3;
                return;
            }

            var shifted = (int)color << (int)(1 + (seed & 1));
            color = (EdgeColor)((shifted | shifted >> 3) & (int)EdgeColor.White);
            seed >>= 1;
        }
    }
}
