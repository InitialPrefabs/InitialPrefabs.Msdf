use msdf_impl::{
    args::Args,
    byte_buffer::ByteBuffer,
    font_data::FontData,
    glyph_data::GlyphData,
    utils::{convert_u16_to_os_string, convert_u16_to_string},
    Builder,
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

    Builder::from_font_path(&font_path, chars, &args)
        .prepare_workload(args.thread_count as usize)
        .build_atlas(atlas_path_buffer)
        .package_font_data()
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
    const FONT_PATH: &str = "testing-resources/Roboto-Medium.ttf";
    const DEFAULT_CHAR_SET: &str = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ ";
    use image::DynamicImage;

    use crate::msdf_impl::{
        args::Args, enums::UVSpace, font_data::FontData, get_next_power_of_2,
        glyph_data::GlyphData, Builder,
    };
    use std::{ffi::OsStr, fs::remove_file, path::Path};

    #[test]
    fn get_raw_file_works() {
        let args = Args::default()
            .with_uniform_scale(1.0 / 32.0)
            .with_range(640.0)
            .with_padding(10)
            .with_scaled_texture(true)
            .with_uv_space(UVSpace::OneMinusV);

        let p = OsStr::new(FONT_PATH);
        let builder = Builder::from_font_path(p, "ABC".to_string(), &args);
        assert_eq!(builder.atlas_offsets.len(), 3);
        assert_eq!(builder.glyph_buffer.capacity(), 3);
        assert_eq!(builder.glyph_images.capacity(), 3);
        assert_eq!(builder.thread_metadata.capacity(), 8);
    }

    #[test]
    fn get_raw_file_fails() {
        let args = Args::default()
            .with_uniform_scale(1.0 / 32.0)
            .with_range(640.0)
            .with_padding(10)
            .with_scaled_texture(true)
            .with_uv_space(UVSpace::OneMinusV);

        let builder = Builder::from_font_path(OsStr::new(""), "ABC".to_string(), &args);
        assert!(builder.glyph_buffer.is_empty());
        assert!(builder.atlas_offsets.is_empty());
        assert_eq!(builder.thread_metadata.capacity(), 8);
        assert!(builder.glyph_images.is_empty());
    }

    #[test]
    fn checking_workload() {
        let args = Args::default()
            .with_uniform_scale(1.0 / 32.0)
            .with_range(640.0)
            .with_padding(10)
            .with_scaled_texture(true)
            .with_uv_space(UVSpace::OneMinusV);

        let p = OsStr::new(FONT_PATH);
        let mut builder = Builder::from_font_path(p, "ABCDEFGHIJ".to_string(), &args);

        // Check if we only push 1 thread
        builder.prepare_workload(1);
        assert!(builder.thread_metadata.len() == 1);
        let metadata = builder.thread_metadata.first().unwrap();

        assert_eq!(metadata.start, 0);
        assert_eq!(metadata.work_unit, 10);

        // Check if we push 3 threads
        builder.prepare_workload(3);
        assert_eq!(
            builder.thread_metadata.len(),
            3,
            "Should have prepared 3 metadata"
        );
        for i in 0..2 {
            let metadata = &builder.thread_metadata[i];
            assert_eq!(metadata.work_unit, 3, "The unit of work should only be 3");
            let expected = i as u32 * 3;
            assert_eq!(
                metadata.start,
                expected,
                "This thread should start with element: {} / {}",
                expected,
                builder.thread_metadata.len()
            );
        }

        let last = builder.thread_metadata.last().unwrap();
        assert_eq!(last.work_unit, 4, "The last work unit should be 4");
        assert_eq!(last.start, 6, "This thread should start with element 6");

        // Check what happens if we push too many threads
        builder.prepare_workload(10);
        assert_eq!(builder.thread_metadata.len(), 8);

        for i in 0..7 {
            let metadata = &builder.thread_metadata[i];
            assert_eq!(
                metadata.work_unit, 1,
                "Each thread should only process 1 element."
            );
            assert_eq!(
                metadata.start, i as u32,
                "Each thread is responsible for it's current element's index."
            );
        }

        let last = builder.thread_metadata.last().unwrap();
        assert_eq!(
            last.work_unit, 3,
            "The last thread should process the total amt of work - max_capacity."
        );
        assert_eq!(last.start, 7, "The last thread should at 7.");
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
    fn powers_of_2() {
        let expected = get_next_power_of_2(8);
        assert_eq!(
            expected, 16,
            "Did not return the next power of 2 despite being a power of 2"
        );

        let expected = get_next_power_of_2(30);
        assert_eq!(
            expected, 32,
            "Did not return the next power of 2 that encapsulated the number"
        );
    }

    fn is_power_of_2(unit: u32) -> bool {
        unit & (unit - 1) == 0
    }

    fn remove_file_and_wait(path: &Path) {
        let r = remove_file(path);
        assert!(r.is_ok());
    }

    unsafe fn common_setup(
        chars_to_generate: &str,
        atlas_path: &Path,
        args: Args,
        open_file: bool,
    ) -> (FontData, Option<DynamicImage>) {
        let utf16: Vec<u16> = chars_to_generate.encode_utf16().collect();
        let s = String::from_utf16(&utf16).unwrap();
        let font_path = OsStr::new(FONT_PATH);

        let font_data = Builder::from_font_path(font_path, s.to_string(), &args)
            .prepare_workload(args.thread_count as usize)
            .build_atlas(atlas_path)
            .package_font_data();

        assert!(
            atlas_path.try_exists().unwrap(),
            "The atlas was not written to the desired path"
        );

        assert!(font_data.line_height > 0, "Line height was not set.");
        assert!(
            font_data.ascender > 0,
            "Ascender was not set or returned a negative value."
        );
        assert!(
            font_data.descender < 0,
            "Descender was not set or returned a positive value."
        );
        assert!(
            !font_data.glyph_data.is_null(),
            "The pointer was not set or dropped."
        );

        if open_file {
            let opened_img = image::open(atlas_path);
            assert!(opened_img.is_ok(), "Image was corrupted!");

            (font_data, Some(opened_img.unwrap()))
        } else {
            (font_data, None)
        }
    }

    #[test]
    fn generates_atlas_at_scale_resized_height() {
        unsafe {
            let args = Args::default()
                .with_uniform_scale(1.0 / 32.0)
                .with_range(640.0)
                .with_padding(10)
                .with_scaled_texture(true)
                .with_uv_space(UVSpace::OneMinusV)
                .with_scaled_texture(true);

            let atlas_path = Path::new("atlas0.png");
            let (font_data, actual_img) = common_setup(DEFAULT_CHAR_SET, atlas_path, args, true);

            let glyph_data = *font_data.glyph_data;
            assert_eq!(
                glyph_data.element_len(),
                53,
                "Failed to generate all of the glyph data."
            );

            let actual_img = actual_img.unwrap();

            assert_eq!(
                actual_img.width(),
                512,
                "The image scaled when it should not have."
            );
            assert_eq!(
                actual_img.height(),
                256,
                "The image scaled too much or did not expand to the nearest power of 2."
            );
            remove_file_and_wait(atlas_path);
        }
    }

    #[test]
    fn generates_atlas_at_scale_not_resized() {
        unsafe {
            let args = Args::default()
                .with_uniform_scale(1.0 / 32.0)
                .with_range(640.0)
                .with_padding(10)
                .with_scaled_texture(false)
                .with_uv_space(UVSpace::OneMinusV);

            let atlas_path = Path::new("atlas1.png");
            let (font_data, actual_img) = common_setup(DEFAULT_CHAR_SET, atlas_path, args, true);

            let glyph_data = *font_data.glyph_data;

            assert_eq!(
                glyph_data.element_len(),
                53,
                "Failed to generate all of the glyph data."
            );

            let actual_img = actual_img.unwrap();

            assert_eq!(
                actual_img.width(),
                512,
                "The image scaled when it should not have."
            );
            assert!(
                actual_img.height() < 256 && !is_power_of_2(actual_img.height()),
                "The image scaled to the nearest power of 2 when it should not have."
            );
            remove_file_and_wait(atlas_path);
        }
    }

    #[test]
    fn generates_atlas_at_size() {
        unsafe {
            let args = Args::default()
                .with_uniform_scale(1.0)
                .with_padding(10)
                .with_uv_space(UVSpace::OneMinusV)
                .with_range(640.0)
                .with_angle(180.0)
                .with_max_atlas(512);

            let atlas_path = Path::new("atlas2.png");
            let (font_data, _) = common_setup(DEFAULT_CHAR_SET, atlas_path, args, false);

            assert!(
                atlas_path.exists(),
                "The atlas was not written to the desired path"
            );
            assert!(font_data.line_height > 0, "Line height was not set.");
            assert!(
                font_data.ascender > 0,
                "Ascender was not set or returned a negative value."
            );
            assert!(
                font_data.descender < 0,
                "Descender was not set or returned a positive value."
            );
            assert!(
                !font_data.glyph_data.is_null(),
                "The pointer was not set or dropped."
            );

            remove_file_and_wait(atlas_path);
        }
    }
}
