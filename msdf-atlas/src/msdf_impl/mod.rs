use font_data::FontData;
use image::{DynamicImage, ImageBuffer, Rgb};
use log::{debug, LevelFilter};
use mint::Vector2;
use msdf::{GlyphLoader, Projection, SDFTrait};
use regex::bytes::Regex;
use simple_logging::log_to_file;
use std::collections::HashMap;
use std::ffi::OsStr;
use std::io::Error;
use std::path::Path;
use std::result::Result;
use std::str::Chars;
use std::{fs::File, io::Read};
use ttf_parser::{Face, GlyphId, Rect};

use crate::msdf_impl::args::Args;
use crate::msdf_impl::glyph_data::GlyphData;

use self::byte_buffer::ByteBuffer;

pub mod args;
pub mod byte_buffer;
pub mod enums;
pub mod font_data;
pub mod glyph_data;
pub mod utils;

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
#[warn(deprecated)]
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

pub fn get_raw_font_os_string(file_path: &OsStr) -> Result<Vec<u8>, Error> {
    let lossy_string = file_path.to_string_lossy();

    if !lossy_string.ends_with(".otf") && !lossy_string.ends_with(".ttf") {
        panic!("The file, {} is not an otf or ttf file!", lossy_string);
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

    #[inline]
    pub fn get_glyph_dimensions(&self, args: &Args) -> (i32, i32) {
        let width = args.scale_dimension_with_padding(self.rect.width().into());
        let height = args.scale_dimension_with_padding(self.rect.height().into());
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
        let opt_glyph = face.glyph_index(c);
        if opt_glyph.is_none() {
            continue;
        }

        let glyph_index = opt_glyph.unwrap();

        let opt_bounding_box = face.glyph_bounding_box(glyph_index);
        if opt_bounding_box.is_none() {
            continue;
        }

        let bounding_box = opt_bounding_box.unwrap();
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

#[inline]
pub fn get_next_power_of_2(number: i32) -> i32 {
    // We know the # is a power of 2
    if number & (number - 1) == 0 {
        1 << ((number as f32).log(2.0) + 1.0) as i32
    } else {
        1 << ((number as f32).log(2.0).ceil() as i32)
    }
}

/// Accounts for if our glyph is much larger than our intended atlas width, we scale the atlas
/// width to the next biggest power of 2.
///
/// # Arguments
///
/// * `desired_width` - The desired width of the atlas
/// * `glyph_data` - The glyphs to look through
/// * `args` - Generator params
#[inline]
fn find_best_fit_width(
    desired_width: i32,
    glyph_data: &Vec<GlyphBoundingBoxData>,
    args: &Args,
) -> i32 {
    let mut atlas_width = desired_width;
    for glyph in glyph_data {
        let scaled_width = args.scale_dimension_with_padding(glyph.rect.width().into());

        if scaled_width >= atlas_width {
            atlas_width = get_next_power_of_2(atlas_width);
            debug!("Resized the atlas width from: {} -> {}", desired_width, atlas_width);
        }
    }
    atlas_width
}

/// Calculates the minimum atlas width and height.
///
/// # Arguments
///
/// * `glyph_data` - The generated glyphs to check against
/// * `args` - Generator parameters
#[inline]
fn calculate_minimum_atlas_dimensions(
    glyph_data: &Vec<GlyphBoundingBoxData>,
    args: &Args,
) -> (u32, u32, Vec<i32>) {
    let mut atlas_width: i32 = 0;
    let mut atlas_height: i32 = 0;

    let mut line_count = 0;
    let mut line_heights: Vec<i32> = Vec::with_capacity(5);

    let max_width = find_best_fit_width(args.max_atlas_width as i32, glyph_data, args);
    let first_height =
        args.scale_dimension_with_padding(glyph_data.first().unwrap().rect.height().into());

    line_heights.push(first_height);

    for glyph in glyph_data {
        let current_height = args.scale_dimension_with_padding(glyph.rect.height().into());
        let width = args.scale_dimension_with_padding(glyph.rect.width().into());

        let next_width = atlas_width + width;
        if next_width >= max_width {
            atlas_height += args.scale_dimension_with_padding(line_heights[line_count]);
            line_heights.push(current_height);
            line_count += 1;
            // We have to use the scaled width because it is added to the next line.
            atlas_width = width;
        } else {
            atlas_width = next_width;
        }
    }

    // If we haven't finished the end of the line, we need to add the height from the last glyph.
    if atlas_width < max_width {
        let last_height = args.scale_dimension_with_padding(line_heights[line_count]);
        atlas_height += last_height;
    }

    atlas_height = get_next_power_of_2(atlas_height);
    (max_width as u32, atlas_height as u32, line_heights)
}

pub unsafe fn get_font_metrics(
    raw_font_data: &[u8],
    atlas_path: &Path,
    chars_to_generate: String,
    args: Args,
) -> FontData {
    let _ = log_to_file("font-metrics.log", LevelFilter::Debug);
    let face = Face::parse(raw_font_data, 0).unwrap();
    let count = chars_to_generate.len();
    let chars = chars_to_generate.chars();

    let msdf_config = Default::default();

    // Preallocated the glyph_faces and glyph data
    let mut glyph_faces: Vec<GlyphBoundingBoxData> = Vec::with_capacity(count);
    let mut glyph_buffer: Vec<GlyphData> = Vec::with_capacity(count);

    let clear: Rgb<f32> = Rgb([0.0, 0.0, 0.0]);

    store_and_sort_by_area(&mut glyph_faces, &face, chars);

    let (max_width, max_height, line_heights) =
        calculate_minimum_atlas_dimensions(&glyph_faces, &args);
    debug!(
        "Max Width: {}, Max_Height: {}, Total Lines: {}",
        max_width,
        max_height,
        line_heights.len()
    );

    let mut x_offset: i32 = 0;
    let mut y_offset: i32 = 0;

    // Create our dynamic atlas buffer
    let mut atlas = ImageBuffer::from_pixel(max_width, max_height, clear);
    let scale = args.get_scale();

    let mut current_line_no = 0;

    let radians = args.get_radians();

    for glyph_face in glyph_faces {
        let glyph_index = glyph_face.glyph_index;
        let shape = face.load_shape(glyph_index).unwrap();

        let colored_shape = match args.color_type {
            enums::ColorType::Simple => shape.color_edges_simple(radians),
            enums::ColorType::InkTrap => shape.color_edges_ink_trap(radians),
            enums::ColorType::Distance => shape.color_edges_by_distance(radians),
        };

        let translation = {
            let this = &glyph_face;
            Vector2 {
                x: (-1.0 * this.rect.x_min as f64),
                y: (-1.0 * this.rect.y_min as f64),
            }
        };
        let projection = Projection { scale, translation };

        let (glyph_width, glyph_height) = glyph_face.get_glyph_dimensions(&args);

        let msdf_data = colored_shape.generate_msdf(
            glyph_width as u32,
            glyph_height as u32,
            args.range as f64,
            &projection,
            &msdf_config,
        );

        let glyph_image = msdf_data.to_image();
        let next_width =
            x_offset + args.scale_dimension_with_padding(glyph_face.rect.width().into());

        if next_width >= max_width as i32 {
            // Increment the y offset
            y_offset += args.scale_dimension_with_padding(line_heights[current_line_no]);
            current_line_no += 1;
            // We reset the x_offset because we have to go the next row in the atlas.
            x_offset = 0;
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

        glyph_buffer.push(glyph_data);

        // Now we have to copy the glyph image to a giant data buffer which is our atlas.
        for (x, y, pixel) in glyph_image.enumerate_pixels() {
            let (new_x, new_y) = (x + x_offset as u32, y + y_offset as u32);
            // We need to copy the pixel given the offset
            atlas.put_pixel(new_x, new_y, *pixel);
        }

        x_offset += args.add_padding(glyph_image.width() as i32);
    }

    glyph_buffer.sort_unstable_by(|lhs, rhs| lhs.unicode.partial_cmp(&rhs.unicode).unwrap());

    _ = DynamicImage::from(atlas).into_rgb8().save(atlas_path);
    debug!("Generated atlas to {}", atlas_path.to_str().unwrap());

    let byte_buffer = ByteBuffer::from_vec_struct(glyph_buffer);
    let ascender = face.ascender() as i32;
    let descender = face.descender() as i32;
    let line_height = ascender + descender;
    FontData {
        line_height,
        ascender,
        descender,
        units_per_em: face.units_per_em() as u32,
        glyph_data: Box::into_raw(Box::new(byte_buffer)),
    }
}
