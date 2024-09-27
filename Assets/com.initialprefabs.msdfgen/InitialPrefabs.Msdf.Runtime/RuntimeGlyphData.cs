using System;
using Unity.Mathematics;

namespace InitialPrefabs.Msdf.Runtime {

    [Serializable]
    public struct RuntimeGlyphData {

        public int Unicode;
#if UNITY_EDITOR
        public char Char;
#endif
        public float Advance;
        public float2 Metrics;
        public float2 Bearings;
        public float4 Uvs;

        public RuntimeGlyphData Scale(float ptSize, RuntimeFaceData runtimeFaceData) {
            var advance = runtimeFaceData.Scale(ptSize, Advance);
            var metrics = runtimeFaceData.Scale(ptSize, Metrics);
            var bearings = runtimeFaceData.Scale(ptSize, Bearings);

            return new RuntimeGlyphData {
                // Keep the unicode and uvs the same
                Unicode = Unicode,
#if UNITY_EDITOR
                Char = (char)Unicode,
#endif
                Uvs = Uvs,
                // Scale the advance
                Advance = advance,
                Metrics = metrics,
                Bearings = bearings
            };
        }

        public RuntimeGlyphData ScaleWithDPI(float ptSize, RuntimeFaceData runtimeFaceData) {
            var advance = runtimeFaceData.ScaleWithDPI(ptSize, Advance);
            var metrics = runtimeFaceData.ScaleWithDPI(ptSize, Metrics);
            var bearings = runtimeFaceData.ScaleWithDPI(ptSize, Bearings);

            return new RuntimeGlyphData {
                // Keep the unicode and uvs the same
                Unicode = Unicode,
#if UNITY_EDITOR
                Char = (char)Unicode,
#endif
                Uvs = Uvs,
                // Scale the advance
                Advance = advance,
                Metrics = metrics,
                Bearings = bearings
            };
        }
    }
}

