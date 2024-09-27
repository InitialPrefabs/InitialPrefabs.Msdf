using InitialPrefabs.Msdf.Runtime;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace InitialPrefabs.Msdf.EditorExtensions {

    internal static class FontDataExtensions {
        public static float CalculateScale(this ref FontData fontData, float pointSize) {
            return pointSize * Screen.dpi / fontData.units_per_em;
        }

        public static RuntimeFaceData ToRuntimeFaceData(this ref FontData fontData) {
            return new RuntimeFaceData {
                LineHeight = fontData.line_height,
                UnitsPerEm = fontData.units_per_em,
                AscentLine = fontData.ascender,
                DescentLine = fontData.descender
            };
        }
    }

    internal static class ByteBufferExtensions {

        public static int ElementLen(this ref ByteBuffer byteBuffer) {
            return byteBuffer.length / byteBuffer.element_size;
        }

        public static unsafe GlyphData ElementAt(this ref ByteBuffer byteBuffer, int i) {
            var ptr = (ByteBuffer*)UnsafeUtility.AddressOf(ref byteBuffer);
            return NativeMethods.reinterpret_as_glyph_data(ptr, (ushort)i);
        }
    }
}

