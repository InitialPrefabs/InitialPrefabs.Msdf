use log::{info, log, LevelFilter};
use msdf::GlyphLoader;
use regex::bytes::Regex;
use simple_logging::log_to_file;
use std::f64;
use std::ffi::{c_char, CStr, CString};
use std::io::Error;
use std::result::Result;
use std::str::FromStr;
use std::{fs::File, io::Read};
use ttf_parser::Face;

use crate::msdf_impl::glyph_data::GlyphData;

pub mod glyph_data;

pub struct Args {
    angle: f64,
}

impl Args {
    pub fn new() -> Self {
        Self { angle: 3.0 }
    }
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

pub unsafe fn get_font_metrics(raw_font_data: &[u8], str: *mut c_char, args: Args) {
    log_to_file("font-metrics.log", LevelFilter::Info);
    let face = Face::parse(raw_font_data, 0).unwrap();

    let face_height = face.height();
    info!("Units per EM: {}", face.units_per_em());
    info!("Face Height: {}", face_height);

    let chars = CStr::from_ptr(str).to_str().unwrap().chars();

    for c in chars {
        let glyph_id = face.glyph_index(c).unwrap();
        let bounding_box = face.glyph_bounding_box(glyph_id).unwrap();

        let bearing_x = face.glyph_hor_side_bearing(glyph_id).unwrap();
        let bearing_y_calc = bounding_box.y_max - bounding_box.y_min;

        let width = bounding_box.width() as f32;
        let height = bounding_box.height() as f32;

        // TODO: Figure out what the metrics and uvs are from the texture
        let glyph = GlyphData::from_char(c)
            .with_advance(face.glyph_hor_advance(glyph_id).unwrap() as f32)
            .with_min_uv(bounding_box.x_min, bounding_box.y_min)
            .with_max_uv(bounding_box.x_max, bounding_box.y_max)
            .with_metrics(width, height)
            .with_bearings(bearing_x, bearing_y_calc);
        info!("{}", glyph.to_string());
    }
}
