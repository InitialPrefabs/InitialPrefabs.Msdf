using Unity.Collections.LowLevel.Unsafe;

namespace InitialPrefabs.Msdf {

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

