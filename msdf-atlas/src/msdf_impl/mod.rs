use image::{DynamicImage, ImageBuffer, Rgb};
use log::{info, LevelFilter};
use mint::Vector2;
use msdf::{ColoredShape, GlyphLoader, Projection, SDFTrait};
use regex::bytes::Regex;
use simple_logging::log_to_file;
use std::any::Any;
use std::f64;
use std::ffi::{c_char, CStr};
use std::io::Error;
use std::result::Result;
use std::str::Chars;
use std::sync::Arc;
use std::{fs::File, io::Read};
use ttf_parser::{Face, GlyphId, Rect};

use crate::msdf_impl::glyph_data::GlyphData;

pub mod glyph_data;

pub struct Args {
    /// Stores the angle in degrees for coloring the shape
    angle: f64,
    /// Scale of the generated glyphs. Recommended to use powers of 1 / 2^n.
    uniform_scale: f64,
}

impl Args {
    /// Generates arguments with default settings with angle of
    /// 3 degrees and no adjustments to the scale.
    pub fn default() -> Self {
        Self {
            angle: 3.0,
            uniform_scale: 1.0,
        }
    }

    /// Generates new arguments with angle and scale.
    ///
    /// # Arguments
    ///
    /// * `angle` - The angle used to color the MSDF
    /// * `uniform_scale` - The scale to adjust the generated glyphs by
    pub fn new(angle: f64, uniform_scale: f64) -> Args {
        Self {
            angle,
            uniform_scale,
        }
    }

    /// Builder to adjust the angle separately.
    ///
    /// # Arguments
    ///
    /// * `angle` - The angle in degrees.
    pub fn with_angle(mut self, angle: f64) -> Args {
        self.angle = angle;
        self
    }

    /// Builder to adjust the scale of the generated glyphs
    ///
    /// # Arguments
    ///
    /// * `uniform_scale` - Scale of the generated glyphs. Recommended to use powers of 1 / 2^n.
    pub fn with_uniform_scale(mut self, uniform_scale: f64) -> Args {
        self.uniform_scale = uniform_scale;
        self
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

fn scale_bounding_box(r: &mut Rect, scale_factor: i16) {
    r.x_min /= scale_factor;
    r.x_max /= scale_factor;
    r.y_min /= scale_factor;
    r.y_max /= scale_factor;
}

#[derive(Clone, Copy)]
struct GlyphBoundingBoxData {
    rect: Rect,
    unicode: char,
    glyph_index: GlyphId,
}

impl GlyphBoundingBoxData {
    pub fn new(unicode: char, glyph_index: GlyphId, r: Rect) -> GlyphBoundingBoxData {
        Self {
            rect: r,
            glyph_index: glyph_index,
            unicode: unicode,
        }
    }

    pub fn area(&self) -> i32 {
        println!("{}, {}", self.rect.width(), self.rect.height());
        self.rect.width() as i32 * self.rect.height() as i32
    }

    pub fn get_translation(&self, scale : f64) -> Vector2<f64> {
        Vector2 {
            x: (-1.0 * self.rect.x_min as f64) as f64,
            y: (-1.0 * self.rect.y_min as f64) as f64,
        }
    }

    pub fn get_glyph_dimensions(&self, uniform_scale: f64) -> (u32, u32) {
        let width =  (self.rect.width() as f64 * uniform_scale) as u32;
        let height = (self.rect.height() as f64 * uniform_scale) as u32;
        (width, height)
    }
}

fn sort_by_area(rects: &mut Vec<GlyphBoundingBoxData>, face: &Face, chars: Chars) {
    for c in chars {
        let glyph_index = face.glyph_index(c).unwrap();
        let bounding_box = face.glyph_bounding_box(glyph_index).unwrap();
        rects.push(GlyphBoundingBoxData::new(c, glyph_index, bounding_box));
    }
    rects.sort_unstable_by(|lhs, rhs| {
        lhs.area().cmp(&rhs.area())
    });
}

pub unsafe fn get_font_metrics(raw_font_data: &[u8], str: *mut c_char, args: Args) {
    let _ = log_to_file("font-metrics.log", LevelFilter::Info);
    let face = Face::parse(raw_font_data, 0).unwrap();

    let face_height = face.height();
    info!("Units per EM: {}", face.units_per_em());
    info!("Face Height: {}", face_height);

    let c_string = CStr::from_ptr(str).to_str().unwrap();
    let count = c_string.len() as usize;
    let chars = c_string.chars();

    let msdf_config = Default::default();

    // Preallocated the glyph_faces
    let mut glyph_faces: Vec<GlyphBoundingBoxData> = Vec::with_capacity(count);

    // Store the sizes and sort it.
    // let clear = 0 as f32;
    // let mut atlas = ImageBuffer::from_pixel(512 * count, 512, Rgb([clear, clear, clear]));

    sort_by_area(&mut glyph_faces, &face, chars);
    let mut index = 0;
    for v in glyph_faces {
        let glyph_index = v.glyph_index;
        // let bearing_x = face.glyph_hor_advance(glyph_index).unwrap() / 64;
        // let bearing_y = (v.rect.y_max - v.rect.y_min) / 64;
        // let width = v.rect.width();
        // let height = v.rect.height();

        let shape = face.load_shape(glyph_index).unwrap();
        let colored_shape = shape.color_edges_simple(3.0);

        let scale = Vector2 {
            x: args.uniform_scale,
            y: args.uniform_scale,
        };
        let translation = v.get_translation(64.0);
        let projection = Projection { scale, translation };

        let (glyph_width, glyph_height) = v.get_glyph_dimensions(args.uniform_scale);

        let msdf_data = colored_shape.generate_msdf(
            glyph_width as u32,
            glyph_height as u32,
            4.0,
            &projection,
            &msdf_config,
        );

        let glyph_image = msdf_data.to_image();

        // info!("{}", glyph.to_string());
        // TODO: Generate the atlas.
        _ = DynamicImage::from(glyph_image)
            .into_rgba8()
            .save(format!("{}{}.png", v.unicode, index));
        index += 1;
   }
}
