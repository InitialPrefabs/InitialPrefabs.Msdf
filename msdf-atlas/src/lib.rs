use msdf_impl::{args::Args, byte_buffer::ByteBuffer, get_font_metrics, get_raw_font};
use std::{ffi::c_char, u16};

mod msdf_impl;

#[no_mangle]
pub unsafe extern "C" fn get_glyph_data(
    font_path: &str,
    str: *mut c_char,
    args: Args,
) -> (u16, *mut ByteBuffer) {
    let raw_font_data = get_raw_font(font_path).unwrap();
    unsafe { 
        get_font_metrics(&raw_font_data, str, args)
    }
}

#[cfg(test)]
mod tests {
    use crate::msdf_impl::{
        args::Args, get_font_metrics, get_raw_font, glyph_data::GlyphData, uv_space::UVSpace,
    };
    use core::panic;
    use std::ffi::CString;

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
            let raw_font_data = get_raw_font("Roboto-Medium.ttf").unwrap();
            let c_string = CString::new("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ")
                .unwrap()
                .into_raw();
            let args = Args::default()
                .with_angle(1.0 / 16.0)
                .with_uniform_scale(1.0 / 32.0)
                .with_padding(10)
                .with_uv_space(UVSpace::OneMinusV);
            get_font_metrics(&raw_font_data, c_string, args);
        }
    }
}
