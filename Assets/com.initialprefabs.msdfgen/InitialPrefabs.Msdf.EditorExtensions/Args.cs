using System;

namespace InitialPrefabs.Msdf.EditorExtensions {

    /// <summary>
    /// Stub to allow Args to be serializable from MsdfAtlas.cs
    /// </summary>
    [Serializable]
    internal partial struct Args {

        public static Args CreateDefault() {
            return new Args {
                uniform_scale = 1.0f / 64.0f,
                padding = 10,
                max_atlas_width = 512,
                range = 4.0f,
                uv_space = UVSpace.OneMinusV,
                color_type = ColorType.Simple,
                degrees = 15
            };
        }
    }
}
