use csbindgen::Builder;

fn main() {
    Builder::default()
        .input_extern_file("src/lib.rs")
        .input_extern_file("src/msdf_impl/args.rs")
        .input_extern_file("src/msdf_impl/byte_buffer.rs")
        .input_extern_file("src/msdf_impl/glyph_data.rs")
        .input_extern_file("src/msdf_impl/uv_space.rs")
        .csharp_dll_name("example")
        .generate_csharp_file("test.cs")
        .unwrap();
}
