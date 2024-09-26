using System;
using Unity.Mathematics;

namespace InitialPrefabs.Msdf.Runtime {
    [Serializable]
    public struct RuntimeGlyphData {
        public int Unicode;
        public char Char;
        public float Advance;
        public float2 Metrics;
        public float2 Bearings;
        public float4 Uvs;

        public RuntimeGlyphData Scale(float scale) {
            return new RuntimeGlyphData {
                // Keep the unicode and uvs the same
                Unicode = Unicode,
                Char = (char)Unicode,
                Uvs = Uvs,
                // Scale the advance
                Advance = Advance * scale,
                Metrics = Metrics * scale,
                Bearings = Metrics * scale,
            };
        }
    }
}

