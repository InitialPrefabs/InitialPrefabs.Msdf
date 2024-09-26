use csbindgen::Builder;

fn main() {
    Builder::default()
        .input_extern_file("src/lib.rs")
        .input_extern_file("src/msdf_impl/args.rs")
        .input_extern_file("src/msdf_impl/byte_buffer.rs")
        .input_extern_file("src/msdf_impl/glyph_data.rs")
        .input_extern_file("src/msdf_impl/uv_space.rs")
        .input_extern_file("src/msdf_impl/font_data.rs")
        .csharp_namespace("InitialPrefabs.Msdf.EditorExtensions")
        .csharp_dll_name("msdf_atlas")
        .generate_csharp_file("MsdfAtlas.cs")
        .unwrap();
}
