using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace InitialPrefabs.Msdf.Runtime {

    [Serializable]
    public struct RuntimeFaceData {
        public int LineHeight;
        public uint UnitsPerEm;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float CalculateScale(float pointSize) {
            return pointSize * Screen.dpi / UnitsPerEm;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float ScaleLineHeight(float scale) {
            return scale * LineHeight;
        }
    }
}
