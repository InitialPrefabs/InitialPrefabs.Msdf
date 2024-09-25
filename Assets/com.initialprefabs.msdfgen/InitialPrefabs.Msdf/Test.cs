using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;

namespace InitialPrefabs.Msdf {

    public class Test {

        public readonly struct Utf16 : IDisposable {

            public readonly IntPtr Ptr;

            public Utf16(string str) {
                int size = (str.Length + 1) * sizeof(char);
                IntPtr ptr = Marshal.AllocHGlobal(size);

                for (int i = 0; i < str.Length; i++) {
                    Marshal.WriteInt16(ptr, i * sizeof(char), str[i]);
                }

                // Add a null terminator to the end of the string
                Marshal.WriteInt16(ptr, str.Length * sizeof(char), 0);

                Ptr = ptr;
            }

            public readonly void Dispose() {
                Marshal.FreeHGlobal(Ptr);
            }

            public unsafe ushort* AsU16Ptr() {
                return (ushort*)Ptr.ToPointer();
            }
        }

        [MenuItem("Tools/InitialPrefabs/Generate Atlas")]
        public static unsafe void Generate() {
            string atlasPath = EditorUtility.SaveFilePanelInProject(
                "Save Atlas", "atlas", "png", "Save the atlas");

            if (string.IsNullOrEmpty(atlasPath)) {
                return;
            }

            using Utf16 fontPath = new Utf16(
                "C:\\Users\\porri\\Documents\\Projects\\Unity\\InitialPrefabs.Msdf\\msdf-atlas\\Roboto-Medium.ttf");
            using Utf16 chars = new Utf16("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
            using Utf16 absoluteAtlasPath = new Utf16(Application.dataPath + atlasPath["Assets".Length..]);

            NativeMethods.get_glyph_data_utf16(
                fontPath.AsU16Ptr(),
                absoluteAtlasPath.AsU16Ptr(),
                chars.AsU16Ptr(),
                new Args {
                    angle = 1.0f / 16f,
                    uniform_scale = 1f / 32f,
                    padding = 10,
                    uv_space = UVSpace.OneMinusV,
                    max_atlas_width = 512,
                    point_size = 24,
                });
        }
    }
}
