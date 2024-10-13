using InitialPrefabs.Msdf.Runtime;
using Unity.Mathematics;

namespace InitialPrefabs.Msdf.EditorExtensions {
    internal static class GlyphDataExtensions {
        public static RuntimeGlyphData ToRuntime(this ref GlyphData d) {
            return new RuntimeGlyphData {
                Unicode = d.unicode,
                Char = (char)d.unicode,
                Advance = d.advance,
                Bearings = new float2(d.bearings_x, d.bearings_y),
                Metrics = new float2(d.metrics_x, d.metrics_y),
                Uvs = new float4(d.uv_x, d.uv_y, d.uv_z, d.uv_w),
            };
        }
    }
}


