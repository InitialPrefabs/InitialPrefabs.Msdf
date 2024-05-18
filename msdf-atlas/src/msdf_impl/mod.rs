use image::{DynamicImage, ImageBuffer, Rgb};
use log::{info, LevelFilter};
use mint::Vector2;
use msdf::{GlyphLoader, Projection, SDFTrait};
use regex::bytes::Regex;
use simple_logging::log_to_file;
use std::f64;
use std::ffi::{c_char, CStr};
use std::io::Error;
use std::result::Result;
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

    let c_string = CStr::from_ptr(str).to_str().unwrap();
    let count = c_string.len() as u32;
    let chars = c_string.chars();

    let msdf_config = Default::default();

    let clear = 0 as f32;

    // let mut atlas = ImageBuffer::from_pixel(512 * count, 512, Rgb([clear, clear, clear]));

    // Create an image that represents the size
    let mut index = 0;
    for c in chars {
        let glyph_index = face.glyph_index(c).unwrap();
        let bounding_box = face.glyph_bounding_box(glyph_index).unwrap();

        let bearing_x = face.glyph_hor_side_bearing(glyph_index).unwrap();
        let bearing_y_calc = bounding_box.y_max - bounding_box.y_min;

        let width = bounding_box.width();
        let height = bounding_box.height();

        // TODO: Figure out what the metrics and uvs are from the texture
        let mut glyph = GlyphData::from_char(c)
            .with_advance(face.glyph_hor_advance(glyph_index).unwrap())
            .with_min_uv(bounding_box.x_min, bounding_box.y_min)
            .with_max_uv(bounding_box.x_max, bounding_box.y_max)
            .with_metrics(width, height)
            .with_bearings(bearing_x, bearing_y_calc);

        let shape = face.load_shape(glyph_index).unwrap();
        let colored_shape = shape.color_edges_simple(3.0);

        let scale = Vector2 {
            x: 1.0 / 64.0,
            y: 1.0 / 64.0,
        };

        let translation = Vector2 {
            x: (32.0 * 64.0 - (bounding_box.width() as f64)) / 2.0
                - (bounding_box.x_min as f64),
            y: (32.0 * 64.0 - (bounding_box.height() as f64)) / 2.0
                - (bounding_box.y_min as f64),
        };

        // Generate a texture that we can 

        // Determine how to add padding
        let projection = Projection { scale, translation };

        let glyph_image_buffer = colored_shape
            .generate_msdf(32, 32, 10.0 * 64.0, &projection, &msdf_config)
            .to_image();

        info!("{}", glyph.to_string());
        // TODO: Generate the atlas.
        _ = DynamicImage::from(glyph_image_buffer)
            .into_rgb8()
            .save(format!("{}{}.png", c, index));
        index += 1;
    }
    // DynamicImage::from(atlas).into_rgb8().save("atlas.png").unwrap();
}
