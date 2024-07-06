use image::{DynamicImage, GenericImageView, ImageBuffer, Rgb};
use log::{info, LevelFilter};
use mint::Vector2;
use msdf::{GlyphLoader, Projection, SDFTrait};
use regex::bytes::Regex;
use simple_logging::log_to_file;
use std::collections::HashMap;
use std::ffi::{c_char, CStr};
use std::io::Error;
use std::result::Result;
use std::str::Chars;
use std::{fs::File, io::Read};
use ttf_parser::{Face, GlyphId, Rect};

use crate::msdf_impl::glyph_data::GlyphData;

mod glyph_data;

pub struct Args {
    /// Stores the angle in degrees for coloring the shape
    angle: f32,
    /// Scale of the generated glyphs. Recommended to use powers of 1 / 2^n.
    uniform_scale: f32,
    padding: u32,
    max_atlas_width: u32,
    // TODO: Add the path for where we want to store the atlas
}

impl Args {
    /// Generates arguments with default settings with angle of
    /// 3 degrees and no adjustments to the scale.
    pub fn default() -> Self {
        Self {
            angle: 3.0,
            uniform_scale: 1.0,
            padding: 0,
            max_atlas_width: 512,
        }
    }

    /// Generates new arguments with angle and scale.
    ///
    /// # Arguments
    ///
    /// * `angle` - The angle used to color the MSDF
    /// * `uniform_scale` - The scale to adjust the generated glyphs by
    /// * `padding` - The amount of padding in between each glyph in the atlas
    /// * `max_atlas_width` - The max width of the atlas
    pub fn new(angle: f32, uniform_scale: f32, padding: u32, max_atlas_width: u32) -> Args {
        Self {
            angle,
            uniform_scale,
            padding,
            max_atlas_width,
        }
    }

    /// Builder to adjust the angle separately.
    ///
    /// # Arguments
    ///
    /// * `angle` - The angle in degrees.
    pub fn with_angle(mut self, angle: f32) -> Args {
        self.angle = angle;
        self
    }

    /// Builder to adjust the scale of the generated glyphs
    ///
    /// # Arguments
    ///
    /// * `uniform_scale` - Scale of the generated glyphs. Recommended to use powers of 1 / 2^n.
    pub fn with_uniform_scale(mut self, uniform_scale: f32) -> Args {
        self.uniform_scale = uniform_scale;
        self
    }

    /// Builder to adjust the padding between the glyphs
    ///
    /// # Arguments
    ///
    /// * `padding` - The amount of space between each glyph in the atlas.
    pub fn with_padding(mut self, padding: u32) -> Args {
        self.padding = padding;
        self
    }

    /// Builder to just the max atlas width.
    ///
    /// # Arguments
    ///
    /// * `max_atlas_width` - The max width of the atlas
    pub fn with_max_atlas(mut self, max_atlas_width: u32) -> Args {
        self.max_atlas_width = max_atlas_width;
        self
    }

    #[inline(always)]
    pub fn scale_dimension(&self, unit: i16) -> i32 {
        self.add_padding((unit as f32 * self.uniform_scale).round() as i32)
    }

    #[inline(always)]
    pub fn add_padding(&self, scaled_unit: i32) -> i32 {
        scaled_unit + (self.padding / 2) as i32
    }

    pub fn get_scale(&self) -> Vector2<f64> {
        let scale = self.uniform_scale as f64;
        Vector2 { x: scale, y: scale }
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
        self.rect.width() as i32 * self.rect.height() as i32
    }

    pub fn get_translation(&self, scale: f64) -> Vector2<f64> {
        Vector2 {
            x: (-1.0 * self.rect.x_min as f64) as f64,
            y: (-1.0 * self.rect.y_min as f64) as f64,
        }
    }

    pub fn get_glyph_dimensions(&self, uniform_scale: f64) -> (i32, i32) {
        let width = (self.rect.width() as f64 * uniform_scale).round() as i32;
        let height = (self.rect.height() as f64 * uniform_scale).round() as i32;
        (width, height)
    }
}

fn sort_by_area(rects: &mut Vec<GlyphBoundingBoxData>, face: &Face, chars: Chars) {
    // let mut row_map: MultiMap<i16, GlyphBoundingBoxData> = MultiMap::new();
    let mut row_map: HashMap<i16, Vec<GlyphBoundingBoxData>> = HashMap::new();
    let mut unique_keys: Vec<i16> = Vec::new();
    for c in chars {
        let glyph_index = face.glyph_index(c).unwrap();
        let bounding_box = face.glyph_bounding_box(glyph_index).unwrap();

        let height = bounding_box.height();

        if !row_map.contains_key(&height) {
            let mut values: Vec<GlyphBoundingBoxData> = Vec::with_capacity(10);
            values.push(GlyphBoundingBoxData::new(c, glyph_index, bounding_box));
            row_map.insert(height, values);
            unique_keys.push(height);
        } else {
            let values = row_map.get_mut(&height).unwrap();
            values.push(GlyphBoundingBoxData::new(c, glyph_index, bounding_box));
        }
    }
    // Sort from highest to lowest along the unique heights
    unique_keys.sort_unstable_by(|lhs, rhs| rhs.partial_cmp(lhs).unwrap());

    // Iterate through and sort each vector
    for height in unique_keys {
        let glyph_data = row_map.get_mut(&height).unwrap();
        // Now we have to sort from biggest to smallest for each glyph
        glyph_data.sort_unstable_by(|lhs, rhs| rhs.area().cmp(&lhs.area()));

        // Now we add to our rects practically nuking the values
        rects.append(glyph_data);
    }
}

fn calculate_minimum_atlas_height(
    glyph_data: &Vec<GlyphBoundingBoxData>,
    args: &Args,
) -> (u32, Vec<i16>) {
    let mut atlas_width: i32 = 0;
    let mut atlas_height: i32 = 0;

    let mut line_count = 0;
    let mut line_heights: Vec<i16> = Vec::with_capacity(5);

    let max_width = args.max_atlas_width as i32;

    line_heights.push(glyph_data.first().unwrap().rect.height());

    for glyph in glyph_data {
        let current_height = glyph.rect.height();

        let width = args.scale_dimension(glyph.rect.width());

        let next_width = atlas_width + width;
        if next_width >= max_width {
            atlas_width = 0.into();
            atlas_height += args.scale_dimension(line_heights[line_count]);
            line_heights.push(current_height);
            line_count += 1;
        } else {
            atlas_width = next_width;
        }
    }

    // If we haven't finished the end of the line, we need to add the height from the last glyph.
    if atlas_width < max_width {
        let last_height = args.scale_dimension(line_heights[line_count]);
        atlas_height += last_height;
    }

    (atlas_height as u32, line_heights)
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

    let clear: Rgb<f32> = Rgb([0.0, 0.0, 0.0]);

    sort_by_area(&mut glyph_faces, &face, chars);

    let max_width = args.max_atlas_width;
    let (max_height, line_heights) = calculate_minimum_atlas_height(&glyph_faces, &args);
    info!("Max Width: {}, Max_Height: {}", max_width, max_height);

    // let mut index = 0;
    let mut x_offset: i32 = 0;
    let mut y_offset: i32 = 0;

    // Create our dynamic atlas buffer
    let mut atlas = ImageBuffer::from_pixel(max_width, max_height, clear);
    let scale = args.get_scale();

    let mut current_line_no = 0;

    for v in glyph_faces {
        let glyph_index = v.glyph_index;
        // let bearing_x = face.glyph_hor_advance(glyph_index).unwrap() / 64;
        // let bearing_y = (v.rect.y_max - v.rect.y_min) / 64;
        // let width = v.rect.width();
        // let height = v.rect.height();

        let shape = face.load_shape(glyph_index).unwrap();
        let colored_shape = shape.color_edges_simple(3.0);
        let translation = v.get_translation(64.0);
        let projection = Projection { scale, translation };

        let (glyph_width, glyph_height) = v.get_glyph_dimensions(args.uniform_scale.into());

        let msdf_data = colored_shape.generate_msdf(
            glyph_width as u32,
            glyph_height as u32,
            4.0,
            &projection,
            &msdf_config,
        );

        let glyph_image = msdf_data.to_image();
        let next = x_offset + args.add_padding(glyph_image.width() as i32);

        if next >= max_width as i32 {
            x_offset = 0;
            // Increment the y offset
            y_offset += args.scale_dimension(line_heights[current_line_no]);
            current_line_no += 1;
        }

        // Create the glyph and compute the uvs
        let glyph_data = GlyphData::from_char(v.unicode).with_uvs(
            Vector2 {
                x: x_offset,
                y: y_offset,
            },
            Vector2 {
                x: x_offset + glyph_image.width() as i32,
                y: y_offset + glyph_image.height() as i32,
            },
            Vector2 {
                x: max_width as i32,
                y: max_height as i32,
            },
        );
        info!("{}", glyph_data.to_string());

        // Now we have to copy the glyph image to a giant data buffer which is our atlas.
        for (x, y, pixel) in glyph_image.enumerate_pixels() {
            // We need to copy the pixel given the offset
            atlas.put_pixel(x + x_offset as u32, y + y_offset as u32, pixel.clone());
        }
        // TODO: Figure out the new UVs that are written in the atlas.
        x_offset += args.add_padding(glyph_image.width() as i32);


        // info!("{}", glyph.to_string());
        // TODO: Generate the atlas.
        // _ = DynamicImage::from(glyph_image)
        //     .into_rgba8()
        //     .save(format!("{}{}.png", v.unicode, index));
        // index += 1;
    }
    _ = DynamicImage::from(atlas).into_rgb8().save("atlas.png");
}
