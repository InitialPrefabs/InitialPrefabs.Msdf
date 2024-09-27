using System;
using System.Runtime.InteropServices;

namespace InitialPrefabs.Msdf.EditorExtensions {

    public readonly struct Utf16 : IDisposable {

        public readonly IntPtr Ptr;

        public Utf16(string str) {
            var size = (str.Length + 1) * sizeof(char);
            var ptr = Marshal.AllocHGlobal(size);

            for (var i = 0; i < str.Length; i++) {
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
}
