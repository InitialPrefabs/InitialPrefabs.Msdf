using UnityEngine;

namespace InitialPrefabs.Msdf.Runtime {

    [CreateAssetMenu(menuName = "InitialPrefabs/MSDF/SerializedFontData")]
    public class SerializedFontData : ScriptableObject {
        public RuntimeFaceData FontData;
        public RuntimeGlyphData[] Glyphs;
    }
}

