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

        public RuntimeGlyphData Scale(float fontScale) {
            return new RuntimeGlyphData {
                // Keep the unicode and uvs the same
                Unicode = Unicode,
#if UNITY_EDITOR
                Char = (char)Unicode,
#endif
                Uvs = Uvs,
                // Scale the advance
                Advance = Advance * fontScale,
                Metrics = Metrics * fontScale,
                Bearings = Bearings * fontScale
            };
        }
    }
}

