mod msdf_impl;

#[cfg(test)]
mod tests {
    use core::panic;

    use crate::msdf_impl::load_font_file;

    #[test]
    fn load_file_works() {
        let result = load_font_file("UbuntuMonoNerdFontPropo-Regular.ttf");

        match result {
            Ok(content) => assert_ne!(content.len(), 0, "Failed to load"),
            Err(_) => panic!("Failed to find UbuntuMonoNerdFontPropo-Regular.ttf")
        }
    }

    #[test]

    fn load_file_does_not_work() {
        let result = load_font_file("");
        match result {
            Ok(content) => panic!("Failed to load"),
            Err(_) => println!("Successfully handled invalid file")
        }

    }
}
