using UnityEngine;
using UnityEngine.Serialization;

namespace InitialPrefabs.Msdf.Runtime {
#if MSDF_BINARY
    [PreferBinarySerialization]
#endif
    [CreateAssetMenu(menuName = "InitialPrefabs/MSDF/SerializedFontData")]
    public class SerializedFontData : ScriptableObject {
        [FormerlySerializedAs("FontData")]
        public RuntimeFaceData FaceData;
        public RuntimeGlyphData[] Glyphs;
    }
}
