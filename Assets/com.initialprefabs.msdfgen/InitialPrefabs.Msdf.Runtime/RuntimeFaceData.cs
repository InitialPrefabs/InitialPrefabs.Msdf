using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace InitialPrefabs.Msdf.Runtime {

    [Serializable]
    public struct RuntimeFaceData {
        public float AscentLine;
        public float DescentLine;
        public int LineHeight;
        public uint UnitsPerEm;

        [Obsolete]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float Scale(float pointSize, float dimension) {
            return dimension / UnitsPerEm * pointSize;
        }

        [Obsolete]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float2 Scale(float pointSize, float2 dimension) {
            return dimension / UnitsPerEm * pointSize;
        }

        [Obsolete]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float ScaleWithDPI(float pointSize, float dimension) {
            return Scale(pointSize, dimension) * Screen.dpi / 72;
        }

        [Obsolete]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float2 ScaleWithDPI(float pointSize, float2 dimension) {
            return Scale(pointSize, dimension) * Screen.dpi / 72;
        }

        [Obsolete]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float ScaleLineHeight(float scale) {
            return scale * LineHeight;
        }
    }
}
