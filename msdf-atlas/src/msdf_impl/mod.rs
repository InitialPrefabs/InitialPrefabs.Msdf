use msdf::GlyphLoader;
use regex::bytes::Regex;
use std::f64;
use std::ffi::{c_char, CString};
use std::io::Error;
use std::result::Result;
use std::{fs::File, io::Read};
use ttf_parser::Face;

pub mod glyph_data;

pub struct Args {
    angle: f64
}

/// Loads an otf or ttf file.
///
/// # Arguments
///
/// * `file_path` - The absolute path to an otf or ttf file.
///
/// # Panics
///
/// If the file is not an otf or ttf font, then the method will early out and exit.
///
/// # Errors
///
/// If the file is corrupted, the file will not run.
#[allow(unused)]
pub fn get_raw_font(file_path: &str) -> Result<Vec<u8>, Error> {
    let r = Regex::new(r"\.(otf|ttf)$").unwrap();
    if !r.is_match(file_path.as_bytes()) {
        panic!("The file is not an otf or ttf file!");
    }

    let mut file = File::options().read(true).write(false).open(file_path)?;

    let mut buffer = Vec::new();
    file.read_to_end(&mut buffer)?;

    Ok(buffer)
}

pub unsafe fn load_font(raw_font_data: &[u8], str: *mut c_char, args: Args) {
    let face = Face::parse(raw_font_data, 0).unwrap();

    for c in CString::from_raw(str).to_str().unwrap().chars() {
        let glyph_id = face.glyph_index(c).unwrap();
        let bounding_box = face.glyph_bounding_box(glyph_id).unwrap();
        let shape = face.load_shape(glyph_id).unwrap();
    }
}
