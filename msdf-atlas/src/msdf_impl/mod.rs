use image::{DynamicImage, ImageBuffer, Rgb};
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

use crate::msdf_impl::args::Args;
use crate::msdf_impl::glyph_data::GlyphData;

use self::byte_buffer::ByteBuffer;

pub mod args;
pub mod byte_buffer;
pub mod glyph_data;
pub mod uv_space;

/**
 * We know that font_size / fonts.units_per_em() will give us the scale.
 */

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
        panic!("The file, {} is not an otf or ttf file!", file_path);
    }
    let mut file = File::options().read(true).write(false).open(file_path)?;
    let mut buffer = Vec::new();
    file.read_to_end(&mut buffer)?;
    Ok(buffer)
}

#[derive(Clone, Copy)]
struct GlyphBoundingBoxData {
    rect: Rect,
    unicode: char,
    glyph_index: GlyphId,
}

impl GlyphBoundingBoxData {
    pub fn new(unicode: char, glyph_index: GlyphId, rect: Rect) -> GlyphBoundingBoxData {
        Self {
            rect,
            glyph_index,
            unicode,
        }
    }

    pub fn area(&self) -> i32 {
        self.rect.width() as i32 * self.rect.height() as i32
    }

    pub fn get_glyph_dimensions(&self, uniform_scale: f64) -> (i32, i32) {
        let width = (self.rect.width() as f64 * uniform_scale).round() as i32;
        let height = (self.rect.height() as f64 * uniform_scale).round() as i32;
        (width, height)
    }

    pub fn calculate_bearings_y(&self) -> i16 {
        self.rect.y_max + self.rect.y_min
    }

    pub fn calculate_metrics(&self) -> (i16, i16) {
        (self.rect.width(), self.rect.height())
    }
}

fn store_and_sort_by_area(rects: &mut Vec<GlyphBoundingBoxData>, face: &Face, chars: Chars) {
    let mut row_map: HashMap<i16, Vec<GlyphBoundingBoxData>> = HashMap::new();
    let mut unique_keys: Vec<i16> = Vec::with_capacity(5);

    for c in chars {
        let glyph_index = face.glyph_index(c).unwrap();
        let bounding_box = face.glyph_bounding_box(glyph_index).unwrap();

        let height = bounding_box.height();

        if let std::collections::hash_map::Entry::Vacant(e) = row_map.entry(height) {
            let mut values: Vec<GlyphBoundingBoxData> = Vec::with_capacity(10);
            values.push(GlyphBoundingBoxData::new(c, glyph_index, bounding_box));
            e.insert(values);
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
        glyph_data.sort_unstable_by_key(|rhs| std::cmp::Reverse(rhs.area()));

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
            atlas_width = 0;
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

pub unsafe fn get_font_metrics(
    raw_font_data: &[u8],
    str: *mut c_char,
    args: Args,
) -> (u32, *mut ByteBuffer) {
    let _ = log_to_file("font-metrics.log", LevelFilter::Info);
    let face = Face::parse(raw_font_data, 0).unwrap();

    let c_string = CStr::from_ptr(str).to_str().unwrap();
    let count = c_string.len();
    let chars = c_string.chars();

    let msdf_config = Default::default();

    // Preallocated the glyph_faces
    let mut glyph_faces: Vec<GlyphBoundingBoxData> = Vec::with_capacity(count);
    // Preallocate the glyph data
    let mut glyph_buffer: Vec<GlyphData> = Vec::with_capacity(count);

    // font_size = (units_per_em / 1000) * desired_point_size;
    let desired_point_size = args.point_size as f32;
    let units_per_em = face.units_per_em() as f32;

    let clear: Rgb<f32> = Rgb([0.0, 0.0, 0.0]);
    let font_size = units_per_em / 1000.0 * desired_point_size;

    store_and_sort_by_area(&mut glyph_faces, &face, chars);

    let max_width = args.max_atlas_width;
    let (max_height, line_heights) = calculate_minimum_atlas_height(&glyph_faces, &args);
    info!("Max Width: {}, Max_Height: {}", max_width, max_height);

    let mut x_offset: i32 = 0;
    let mut y_offset: i32 = 0;

    // Create our dynamic atlas buffer
    let mut atlas = ImageBuffer::from_pixel(max_width, max_height, clear);
    let scale = args.get_scale();

    let mut current_line_no = 0;

    for glyph_face in glyph_faces {
        let glyph_index = glyph_face.glyph_index;
        let shape = face.load_shape(glyph_index).unwrap();
        let colored_shape = shape.color_edges_simple(3.0);
        let translation = {
            let this = &glyph_face;
            Vector2 {
                x: (-1.0 * this.rect.x_min as f64),
                y: (-1.0 * this.rect.y_min as f64),
            }
        };
        let projection = Projection { scale, translation };

        let (glyph_width, glyph_height) =
            glyph_face.get_glyph_dimensions(args.uniform_scale.into());

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

        // Calculate the face data
        let horizontal_advance = face.glyph_hor_advance(glyph_index).unwrap_or(0);
        let bearing_x = face.glyph_hor_side_bearing(glyph_index).unwrap();
        let bearing_y = glyph_face.calculate_bearings_y();

        let (width, height) = glyph_face.calculate_metrics();

        // Create the glyph and compute the uvs
        let glyph_data = GlyphData::from_char(glyph_face.unicode)
            .with_uvs(
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
                args.uv_space,
            )
            .with_advance(horizontal_advance)
            .with_bearings(bearing_x, bearing_y)
            .with_metrics(width, height);

        info!("{}", glyph_data.to_string());
        glyph_buffer.push(glyph_data);

        // Now we have to copy the glyph image to a giant data buffer which is our atlas.
        for (x, y, pixel) in glyph_image.enumerate_pixels() {
            // We need to copy the pixel given the offset
            atlas.put_pixel(x + x_offset as u32, y + y_offset as u32, *pixel);
        }
        // TODO: Figure out the new UVs that are written in the atlas.
        x_offset += args.add_padding(glyph_image.width() as i32);
    }
    _ = DynamicImage::from(atlas).into_rgb8().save("atlas.png");

    info!(
        "Units Per EM: {}, Total Glyphs: {}",
        face.units_per_em(),
        glyph_buffer.len()
    );

    let byte_buffer = ByteBuffer::from_vec_struct(glyph_buffer);
    (
        face.units_per_em() as u32,
        Box::into_raw(Box::new(byte_buffer)),
    )
}
