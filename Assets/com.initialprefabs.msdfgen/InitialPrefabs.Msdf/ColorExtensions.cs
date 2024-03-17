using UnityEngine;

namespace InitialPrefabs.Msdf {
    public static class ColorExtensions {

        public static Color32 Initialize(this Color32 _, Color color1) => color1;

        public static Color32 Initialize(this Color32 color, byte r, byte gba) {
            color.r = r;
            color.g = color.b = color.a = gba;
            return color;
        }

        public static Color32 Initialize(this Color32 color, byte r, byte g, byte ba) {
            color.r = r;
            color.g = g;
            color.b = color.a = ba;
            return color;
        }

        public static Color32 Initialize(this Color32 color, byte r, byte g, byte b, byte a) {
            color.r = r;
            color.g = g;
            color.b = b;
            color.a = a;
            return color;
        }
    }
}
