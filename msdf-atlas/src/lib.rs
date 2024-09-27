use msdf_impl::{
    args::Args,
    byte_buffer::ByteBuffer,
    font_data::FontData,
    get_font_metrics, get_raw_font_os_string,
    glyph_data::GlyphData,
    utils::{convert_u16_to_os_string, convert_u16_to_string},
};
use std::path::Path;

mod msdf_impl;

/// Returns packed glyph data parsed from msdf.
///
/// # Arguments
///
/// * `font_path` - The absolute path to the font
/// * `atlas_path` - The absolute path to the texture atlas to generate
/// * `chars_to_generate` - A UTF16 encoded series of characters to generate the characters for
/// * `args` - Parameters to set for the atlas generation
///
/// # Safety
///
/// This function relies on a C lib, msdfgen. Because of how we represent data, any bad data will
/// cause this function to panic and crash Unity.
#[no_mangle]
pub unsafe extern "C" fn get_glyph_data_utf16(
    font_path: *const u16,
    atlas_path: *const u16,
    chars_to_generate: *const u16,
    args: Args,
) -> FontData {
    let font_path = convert_u16_to_os_string(font_path);
    let atlas_path = convert_u16_to_string(atlas_path);
    let chars = convert_u16_to_string(chars_to_generate);

    let atlas_path_buffer = Path::new(&atlas_path);
    let raw_font_data = get_raw_font_os_string(font_path.as_os_str()).unwrap();
    get_font_metrics(&raw_font_data, atlas_path_buffer, chars, args)
}

/// Drops the byte_buffer safely from C#.
///
/// # Arguments
///
/// * `byte_buffer` - An allocated block of bytes
///
/// # Safety
///
/// Memory must be manually dropped from Rust as it was allocated. Do not call this function when
/// you need to access the data safely.
#[no_mangle]
pub unsafe extern "C" fn drop_byte_buffer(ptr: *mut ByteBuffer) {
    if !ptr.is_null() {
        let b = *ptr;
        b.destroy();
    }
}

/// Reinterprets an element in the ByteBuffer as a GlyphData.
///
/// # Arguments
///
/// * `byte_buffer` - The byte buffer to reinterpret as an array of GlyphData.
/// * `i` - The index to access
///
/// # Safety
///
/// Uses a rust function to convert an element in a continuous array as a GlyphData.
#[no_mangle]
pub unsafe extern "C" fn reinterpret_as_glyph_data(byte_buffer: &ByteBuffer, i: u16) -> GlyphData {
    byte_buffer.element_at::<GlyphData>(i as usize)
}

#[cfg(test)]
mod tests {
    use crate::msdf_impl::{
        args::Args, get_font_metrics, get_raw_font, glyph_data::GlyphData, uv_space::UVSpace,
    };
    use core::panic;
    use std::path::Path;

    #[test]
    fn get_raw_file_works() {
        let result = get_raw_font("UbuntuMonoNerdFontPropo-Regular.ttf");

        match result {
            Ok(content) => assert_ne!(
                content.len(),
                0,
                "Failed to load to UbuntuMonoNerdFontPropo-Regular.ttf"
            ),
            Err(_) => panic!("Failed to find UbuntuMonoNerdFontPropo-Regular.ttf"),
        }
    }

    #[test]
    #[should_panic]
    fn get_raw_file_fails() {
        let result = get_raw_font("");
        match result {
            Ok(_) => panic!("Failed to load"),
            Err(_) => println!("Successfully handled invalid file"),
        }
    }

    #[test]
    fn glyph_data_constructed() {
        let unicode_version = GlyphData::from_unicode(99);
        let char_version = GlyphData::from_char('c');
        assert!(
            unicode_version == char_version,
            "Failed to convert from char to i32"
        );
    }

    #[test]
    fn log_file() {
        unsafe {
            let raw_font_data = get_raw_font(
                "C:\\Users\\porri\\Documents\\Projects\\Unity\\InitialPrefabs.Msdf\\msdf-atlas\\Roboto-Medium.ttf").unwrap();

            let atlas_path = Path::new(
                "C:\\Users\\porri\\Documents\\Projects\\Unity\\InitialPrefabs.Msdf\\Assets\\com.initialprefabs.msdfgen\\Example\\atlas.png");

            let utf16: Vec<u16> = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
                .encode_utf16()
                .collect();

            let string = String::from_utf16(&utf16).unwrap();
            let args = Args::default()
                .with_uniform_scale(1.0 / 32.0)
                .with_padding(10)
                .with_uv_space(UVSpace::OneMinusV);

            get_font_metrics(&raw_font_data, atlas_path, string, args);
        }
    }
}
