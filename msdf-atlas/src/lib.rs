use log::{info, LevelFilter};
use msdf_impl::{args::Args, byte_buffer::ByteBuffer, get_font_metrics, get_raw_font_os_string};
use simple_logging::log_to_file;
use std::{ffi::OsString, os::windows::ffi::OsStringExt, path::Path};

mod msdf_impl;

#[repr(C)]
pub struct Data {
    units_per_em: u32,
    glyph_data: *mut ByteBuffer,
}

fn convert_u16_to_os_string(ptr: *const u16) -> OsString {
    unsafe {
        let mut len = 0;
        while *ptr.add(len) != 0 {
            len += 1;
        }

        let slice = std::slice::from_raw_parts(ptr, len);
        OsString::from_wide(slice)
    }
}

fn convert_u16_to_string(ptr: *const u16) -> String {
    unsafe {
        let mut len = 0;
        while *ptr.add(len) != 0 {
            len += 1;
        }

        let slice = std::slice::from_raw_parts(ptr, len);
        String::from_utf16(slice).unwrap()
    }
}

#[no_mangle]
pub unsafe extern "C" fn get_glyph_data_utf16(
    font_path: *const u16,
    atlas_path: *const u16,
    chars_to_generate: *const u16,
    args: Args,
) -> Data {
    let font_path = convert_u16_to_os_string(font_path);
    let atlas_path = convert_u16_to_string(atlas_path);
    let chars = convert_u16_to_string(chars_to_generate);

    let atlas_path_buffer = Path::new(&atlas_path);
    let raw_font_data = get_raw_font_os_string(font_path.as_os_str()).unwrap();
    let (units_per_em, glyph_data) = get_font_metrics(&raw_font_data, atlas_path_buffer, chars, args);
    Data {
        units_per_em,
        glyph_data,
    }
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
                .with_angle(1.0 / 16.0)
                .with_uniform_scale(1.0 / 32.0)
                .with_padding(10)
                .with_uv_space(UVSpace::OneMinusV);

            get_font_metrics(&raw_font_data, atlas_path, string, args);
        }
    }
}
