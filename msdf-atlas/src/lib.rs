mod msdf_impl;

#[cfg(test)]
mod tests {
    use crate::msdf_impl::{get_raw_font, glyph_data::GlyphData, get_font_metrics, Args};
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
            let raw_font_data = get_raw_font("Roboto-Thin.ttf").unwrap();
            let c_string = CString::new("AaGg").unwrap().into_raw();
            get_font_metrics(&raw_font_data, c_string, Args::new());
        }
    }
}
