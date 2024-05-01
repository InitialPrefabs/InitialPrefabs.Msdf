mod msdf_impl;

#[cfg(test)]
mod tests {
    use core::panic;

    use crate::msdf_impl::get_raw_font;

    #[test]
    fn get_raw_file_works() {
        let result = get_raw_font("UbuntuMonoNerdFontPropo-Regular.ttf");

        match result {
            Ok(content) => assert_ne!(content.len(), 0, "Failed to load to UbuntuMonoNerdFontPropo-Regular.ttf"),
            Err(_) => panic!("Failed to find UbuntuMonoNerdFontPropo-Regular.ttf")
        }
    }

    #[test]
    #[should_panic]
    fn get_raw_file_fails() {
        let result = get_raw_font("");
        match result {
            Ok(_) => panic!("Failed to load"),
            Err(_) => println!("Successfully handled invalid file")
        }

    }
}
