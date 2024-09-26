// <auto-generated>
// This code is generated by csbindgen.
// DON'T CHANGE THIS DIRECTLY.
// </auto-generated>
#pragma warning disable CS8500
#pragma warning disable CS8981
using System;
using System.Runtime.InteropServices;


namespace InitialPrefabs.Msdf
{
    internal static unsafe partial class NativeMethods
    {
        const string __DllName = "msdf_atlas";



        /// <summary>
        ///  Returns packed glyph data parsed from msdf.
        ///
        ///  # Arguments
        ///
        ///  * `font_path` - The absolute path to the font
        ///  * `atlas_path` - The absolute path to the texture atlas to generate
        ///  * `chars_to_generate` - A UTF16 encoded series of characters to generate the characters for
        ///  * `args` - Parameters to set for the atlas generation
        ///
        ///  # Safety
        ///
        ///  This function relies on a C lib, msdfgen. Because of how we represent data, any bad data will
        ///  cause this function to panic and crash Unity.
        /// </summary>
        [DllImport(__DllName, EntryPoint = "get_glyph_data_utf16", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern Data get_glyph_data_utf16(ushort* font_path, ushort* atlas_path, ushort* chars_to_generate, Args args);

        /// <summary>
        ///  Drops the byte_buffer safely from C#.
        ///
        ///  # Arguments
        ///
        ///  * `byte_buffer` - An allocated block of bytes
        ///
        ///  # Safety
        ///
        ///  Memory must be manually dropped from Rust as it was allocated. Do not call this function when
        ///  you need to access the data safely.
        /// </summary>
        [DllImport(__DllName, EntryPoint = "drop_byte_buffer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern void drop_byte_buffer(ByteBuffer* ptr);

        /// <summary>
        ///  Reinterprets an element in the ByteBuffer as a GlyphData.
        ///
        ///  # Arguments
        ///
        ///  * `byte_buffer` - The byte buffer to reinterpret as an array of GlyphData.
        ///  * `i` - The index to access
        ///
        ///  # Safety
        ///
        ///  Uses a rust function to convert an element in a continuous array as a GlyphData.
        /// </summary>
        [DllImport(__DllName, EntryPoint = "reinterpret_as_glyph_data", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern GlyphData reinterpret_as_glyph_data(ByteBuffer* byte_buffer, ushort i);


    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe partial struct Data
    {
        public uint units_per_em;
        public ByteBuffer* glyph_data;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe partial struct Args
    {
        public float angle;
        public float uniform_scale;
        public uint padding;
        public uint max_atlas_width;
        public uint point_size;
        public UVSpace uv_space;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe partial struct ByteBuffer
    {
        public byte* ptr;
        public int length;
        public int capacity;
        public int element_size;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe partial struct GlyphData
    {
        public int unicode;
        public float advance;
        public float metrics_x;
        public float metrics_y;
        public float bearings_x;
        public float bearings_y;
        public float uv_x;
        public float uv_y;
        public float uv_z;
        public float uv_w;
    }


    [Flags]
    internal enum UVSpace : uint
    {
        Default = 0,
        OneMinusU = 1 << 0,
        OneMinusV = 1 << 1,
    }


}
