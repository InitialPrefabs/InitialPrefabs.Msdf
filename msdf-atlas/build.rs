use csbindgen::Builder;

fn main() {
    Builder::default()
        .input_extern_file("src/lib.rs")
        .csharp_dll_name("example")
        .generate_csharp_file("test.cs")
        .unwrap();
}
