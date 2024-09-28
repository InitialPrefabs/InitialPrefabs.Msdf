using UnityEngine;

namespace InitialPrefabs.Msdf.EditorExtensions {

    internal class GeneratorSettings : ScriptableObject {

        /// <summary>
        /// The generator args that will be passed to <see cref="NativeMethods.get_glyph_data_utf16(ushort*, ushort*, ushort*, Args)"/>,
        /// </summary>
        public Args GeneratorArgs;

        /// <summary>
        /// The Font to extract the glyphs from.
        /// </summary>
        public Font Font;

        /// <summary>
        /// Resources such as the texture and glyphs will need to be saved at some specified path stored here.
        /// </summary>
        public string ResourcePath;

        /// <summary>
        /// A series of characters to generate the atlas for.
        /// </summary>
        public string DefaultCharacters;
    }
}
