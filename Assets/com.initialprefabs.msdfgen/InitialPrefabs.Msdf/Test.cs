using System.Text;
using UnityEditor;

namespace InitialPrefabs.Msdf {

    public class Test {

        [MenuItem("Tools/InitialPrefabs/Generate Atlas")]
        public unsafe static void Generate() {
            string fontPath = "C:\\Users\\porri\\Documents\\Projects\\Unity\\InitialPrefabs.Msdf\\msdf-atlas\\Roboto-Medium.ttf".Trim();
            string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

            byte[] fontPathBytes = Encoding.UTF8.GetBytes(fontPath);
            byte[] charSampleBytes = Encoding.UTF8.GetBytes(chars);

            fixed (byte* fontPathPtr = fontPathBytes) {
                fixed (byte* charSamplesPtr = charSampleBytes) {
                    Data _ = NativeMethods.get_glyph_data(fontPathPtr, charSamplesPtr, new Args {
                        angle = 1.0f / 16f,
                        uniform_scale = 1f / 32f,
                        padding = 10,
                        uv_space = UVSpace.OneMinusV,
                        max_atlas_width = 512,
                        point_size = 24
                    });
                }
            }
        }
    }
}
