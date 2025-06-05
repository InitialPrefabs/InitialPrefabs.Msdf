use enums::ColorType;
use font_data::FontData;
use image::{ImageBuffer, Rgb, Rgba};
use log::{debug, LevelFilter};
use mint::Vector2;
use msdf::{GlyphLoader, MSDFConfig, Projection, SDFTrait};
use raw_img::{RawImage, RawImageView};
use rayon::ThreadPoolBuilder;
use simple_logging::log_to_file;
use std::collections::HashMap;
use std::ffi::OsStr;
use std::path::Path;
use std::str::Chars;
use std::sync::{Arc, Mutex};
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
pub mod raw_img;
pub mod utils;

#[cfg(test)]
use once_cell::sync::Lazy;

#[cfg(test)]
#[allow(dead_code)]
static CHAR_BUFFER: Lazy<Mutex<Vec<char>>> = Lazy::new(|| {
    let char_buffer: Vec<char> = Vec::with_capacity(10);
    Mutex::new(char_buffer)
});

#[cfg(test)]
#[allow(dead_code)]
fn track_char(c: char) {
    use log::error;

    let lock = CHAR_BUFFER.lock();
    match lock {
        Ok(mut char_buffer) => char_buffer.push(c),
        Err(_) => {
            error!("Cannot unlock the static CHAR_BUFFER!");
        }
    }
}

#[cfg(not(test))]
fn track_char(_: char) {}

#[cfg(test)]
#[allow(dead_code)]
fn flush_chars() {
    use log::error;

    let lock = CHAR_BUFFER.lock();
    match lock {
        Ok(mut char_buffer) => {
            let s: String = char_buffer.iter().collect();
            debug!("Chars in buffer so far: {}", s);
            char_buffer.clear();
        }
        Err(_) => {
            error!("Cannot unlock the static CHAR_BUFFER!");
        }
    }
}

#[cfg(not(test))]
fn flush_chars() {}

#[cfg(test)]
fn config_log_file() {
    let _ = log_to_file("font-metrics.log", LevelFilter::Debug);
}

#[cfg(not(test))]
fn config_log_file() {
    let _ = log_to_file("font-metrics.log", LevelFilter::Error);
}

pub struct Builder {
    pub glyph_buffer: Vec<GlyphData>,
    pub atlas_offsets: Vec<(i32, i32)>,
    pub glyph_images: Vec<ImageBuffer<Rgba<f32>, Vec<f32>>>,
    pub thread_metadata: Vec<ThreadMetadata>,
    pub atlas_dimensions: (u32, u32),

    ascender: i32,
    descender: i32,
    line_height: i32,
    units_per_em: u32,
}

impl Builder {
    pub fn from_font_path(font_path: &OsStr, chars_to_generate: String, args: &Args) -> Self {
        config_log_file();
        let lossy_string = font_path.to_string_lossy();
        let chars = chars_to_generate.chars();
        let thread_metadata = Vec::with_capacity(8);

        let glyph_capacity = chars_to_generate.len();

        let mut atlas_offsets: Vec<(i32, i32)> = Vec::with_capacity(glyph_capacity);
        let mut glyph_images: Vec<ImageBuffer<Rgba<f32>, Vec<f32>>> =
            Vec::with_capacity(glyph_capacity);

        if lossy_string.ends_with(".otf") || lossy_string.ends_with(".ttf") {
            let mut buffer: Vec<u8> = Vec::new();
            let mut file = File::options()
                .read(true)
                .write(false)
                .open(font_path)
                .unwrap();
            let _ = file.read_to_end(&mut buffer);

            let face = Face::parse(&buffer, 0).unwrap();
            let ascender = face.ascender() as i32;
            let descender = face.descender() as i32;
            let line_height = ascender + descender;
            let units_per_em = face.units_per_em() as u32;

            let capacity = chars_to_generate.len();

            let mut glyph_bounding_boxes: Vec<GlyphBoundingBoxData> = Vec::with_capacity(capacity);
            let mut glyph_buffer: Vec<GlyphData> = Vec::with_capacity(capacity);

            store_and_sort_by_area(&mut glyph_bounding_boxes, &face, chars);

            let (max_width, max_height, line_heights) =
                calculate_minimum_atlas_dimensions(&glyph_bounding_boxes, args);

            let mut x_offset: i32 = 0;
            let mut y_offset: i32 = 0;
            let mut current_line_no = 0;

            // TODO: It's very possible to mutlthread this
            for glyph_bounding_box in &glyph_bounding_boxes {
                let glyph_index = glyph_bounding_box.glyph_index;

                let (scaled_glyph_width_padding, _) =
                    glyph_bounding_box.get_scaled_glyph_dimensions_with_padding(args);
                let (scaled_glyph_width, scaled_glyph_height) =
                    glyph_bounding_box.get_scaled_glyph_dimensions_no_padding(args);

                let horizontal_advance = face.glyph_hor_advance(glyph_index).unwrap_or(0);
                let bearing_x = face.glyph_hor_side_bearing(glyph_index).unwrap();
                let bearing_y = glyph_bounding_box.calculate_bearings_y(ascender);

                let (width, height) = glyph_bounding_box.get_metrics();

                let opt_shape = face.load_shape(glyph_index);
                if opt_shape.is_some() {
                    let shape = opt_shape.unwrap();
                    let radians = args.get_radians();

                    let colored_shape = match args.color_type {
                        ColorType::Simple => shape.color_edges_simple(radians),
                        ColorType::InkTrap => shape.color_edges_ink_trap(radians),
                        ColorType::Distance => shape.color_edges_by_distance(radians),
                    };

                    let translation = {
                        let this = &glyph_bounding_box;
                        Vector2 {
                            x: (-1.0 * this.rect.x_min as f64),
                            y: (-1.0 * this.rect.y_min as f64),
                        }
                    };
                    let projection = Projection {
                        scale: args.get_scale(),
                        translation,
                    };

                    let msdf_config: MSDFConfig = Default::default();
                    let msdf_data = colored_shape.generate_mtsdf(
                        scaled_glyph_width as u32,
                        scaled_glyph_height as u32,
                        args.range as f64,
                        &projection,
                        &msdf_config,
                    );

                    let glyph_image: ImageBuffer<Rgba<f32>, Vec<f32>> = msdf_data.to_image();
                    glyph_images.push(glyph_image);
                } else {
                    debug!(
                        "Skipped {} due to no shape being generated for msdf.",
                        glyph_bounding_box.unicode
                    );
                }

                let next_width = x_offset + scaled_glyph_width_padding;
                if next_width >= max_width as i32 {
                    y_offset += line_heights[current_line_no];
                    current_line_no += 1;
                    x_offset = 0;
                }

                let uv_start = Vector2 {
                    x: x_offset,
                    y: y_offset,
                };

                let uv_end = Vector2 {
                    x: x_offset + scaled_glyph_width,
                    y: y_offset + scaled_glyph_height,
                };

                let glyph_data = GlyphData::from_char(glyph_bounding_box.unicode)
                    .with_uvs(
                        uv_start,
                        uv_end,
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

                atlas_offsets.push((x_offset, y_offset));

                x_offset += scaled_glyph_width_padding;
            }

            let dim = (max_width, max_height);

            return Builder {
                glyph_buffer,
                atlas_offsets,
                glyph_images,
                thread_metadata,
                atlas_dimensions: dim,
                ascender,
                descender,
                line_height,
                units_per_em,
            };
        }

        Builder {
            glyph_buffer: Vec::new(),
            atlas_offsets,
            glyph_images,
            thread_metadata,
            atlas_dimensions: (0, 0),
            ascender: 0,
            descender: 0,
            line_height: 0,
            units_per_em: 0,
        }
    }

    pub fn prepare_workload(&mut self, thread_count: usize) -> &mut Builder {
        // TODO: Add a check if the thread_count > than the total # of glyph bounding boxes.
        let metadata = &mut self.thread_metadata;
        metadata.clear();

        let total = self.glyph_images.len();
        let thread_count = thread_count.min(metadata.capacity()) as u32;
        let minimum_slice_size = total as u32 / thread_count;

        let last = thread_count - 1;
        for i in 0..last {
            metadata.push(ThreadMetadata {
                start: minimum_slice_size * i,
                work_unit: minimum_slice_size,
            });
        }

        let last_thread = minimum_slice_size * last;
        metadata.push(ThreadMetadata {
            start: last_thread,
            work_unit: total as u32 - last_thread,
        });

        self
    }

    pub fn build_atlas(&mut self, path: &Path) -> &mut Builder {
        let thread_count = self.thread_metadata.len();
        let (max_width, max_height) = self.atlas_dimensions;

        let mut pixels: Vec<[u8; 4]> = vec![[0, 0, 0, 0]; (max_width * max_height) as usize];
        let raw_img = RawImage::new(&mut pixels, max_width, max_height);

        let mut raw_image_views: Vec<Arc<Mutex<RawImageView>>> =
            Vec::with_capacity(self.glyph_images.len());

        for metadata in &self.thread_metadata {
            let (start, end) = metadata.get_slice_offsets();

            let glyph_images: &[ImageBuffer<Rgba<f32>, Vec<f32>>] = &self.glyph_images[start..end];
            let atlas_offsets = &self.atlas_offsets[start..end];

            for (local_idx, (offset_x, offset_y)) in atlas_offsets.iter().enumerate() {
                let glyph_img = &glyph_images[local_idx];
                let raw_img_view = RawImageView::new(
                    &raw_img,
                    *offset_x as u32,
                    *offset_y as u32,
                    glyph_img.width(),
                    glyph_img.height(),
                );

                let arc_img_view = Arc::new(Mutex::new(raw_img_view));
                raw_image_views.push(arc_img_view);
            }
        }

        let pool = ThreadPoolBuilder::new()
            .num_threads(thread_count)
            .build()
            .unwrap();

        let shared_target_views: Arc<Vec<Arc<Mutex<RawImageView<'_>>>>> = Arc::new(raw_image_views);
        let shared_src_images: Arc<&Vec<ImageBuffer<Rgba<f32>, Vec<f32>>>> =
            Arc::new(&self.glyph_images);

        pool.scope(|s| {
            for thread_id in 0..thread_count {
                let metadata = &self.thread_metadata[thread_id];
                let thread_target_view = shared_target_views.clone();
                let thread_src_images = shared_src_images.clone();
                s.spawn(move |_| {
                    let (start, end) = metadata.get_slice_offsets();

                    for it in start..end {
                        let mut target_view = thread_target_view[it].lock().unwrap();
                        let src_image = &thread_src_images[it];
                        target_view.for_each_mut(&|x, y, p| {
                            let pixel = src_image.get_pixel(x, y);
                            *p = [
                                (pixel[0].clamp(0.0, 1.0) * 255.0) as u8,
                                (pixel[1].clamp(0.0, 1.0) * 255.0) as u8,
                                (pixel[2].clamp(0.0, 1.0) * 255.0) as u8,
                                (pixel[3].clamp(0.0, 1.0) * 255.0) as u8
                            ];
                        });
                    }
                });
            }
        });

        raw_img.process_as_byte_array(&|bytes| {
            let atlas: ImageBuffer<Rgba<u8>, &[u8]> =
                ImageBuffer::from_raw(max_width, max_height, bytes)
                    .expect("Failed to create the image");
            atlas.save(path).expect("Failed to save img");
        });

        self
    }

    /// Constructs a new font data to send through an FFI.
    pub fn package_font_data(&self) -> FontData {
        // TODO: Don't really need to copy, find a way to just conver the original glyph_buffer
        let mut new_glyph_data = self.glyph_buffer.to_vec();
        new_glyph_data.sort_unstable_by(|lhs, rhs| lhs.unicode.partial_cmp(&rhs.unicode).unwrap());
        let glyph_data = ByteBuffer::from_vec_struct(new_glyph_data);

        FontData {
            line_height: self.line_height,
            units_per_em: self.units_per_em,
            ascender: self.ascender,
            descender: self.descender,
            glyph_data: Box::into_raw(Box::new(glyph_data)),
        }
    }
}

/**
 * We know that font_size / fonts.units_per_em() will give us the scale.
 */

/// Stores the Glyph's bounding box, the associated unicode, and its ID.
#[derive(Clone, Copy)]
pub struct GlyphBoundingBoxData {
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

    #[inline(always)]
    pub fn area(&self) -> i32 {
        self.rect.width() as i32 * self.rect.height() as i32
    }

    #[inline(always)]
    pub fn get_scaled_glyph_dimensions_with_padding(&self, args: &Args) -> (i32, i32) {
        let width = args.scale_dimension_with_padding(self.rect.width().into());
        let height = args.scale_dimension_with_padding(self.rect.height().into());
        (width, height)
    }

    #[inline(always)]
    pub fn get_scaled_glyph_dimensions_no_padding(&self, args: &Args) -> (i32, i32) {
        let width = (self.rect.width() as f32 * args.uniform_scale).round() as i32;
        let height = (self.rect.height() as f32 * args.uniform_scale).round() as i32;
        (width, height)
    }

    #[inline(always)]
    pub fn calculate_bearings_y(&self, ascender: i32) -> i16 {
        self.rect.y_max - ascender as i16
    }

    #[inline(always)]
    pub fn get_metrics(&self) -> (i16, i16) {
        (self.rect.width(), self.rect.height())
    }
}

pub fn store_and_sort_by_area(rects: &mut Vec<GlyphBoundingBoxData>, face: &Face, chars: Chars) {
    let mut row_map: HashMap<i16, Vec<GlyphBoundingBoxData>> = HashMap::new();
    let mut unique_keys: Vec<i16> = Vec::with_capacity(5);

    for c in chars {
        let opt_glyph = face.glyph_index(c);
        if opt_glyph.is_none() {
            debug!(
                "Skipped unicode, {}, because it does not exist in the font!",
                c
            );
            continue;
        }

        let glyph_index = opt_glyph.unwrap();

        // let opt_bounding_box = face.glyph_bounding_box(glyph_index);
        let horizontal_advance = face.glyph_hor_advance(glyph_index);
        if horizontal_advance.is_none() {
            debug!("Skipped: {}", c);
            continue;
        }

        let bounding_box = face.glyph_bounding_box(glyph_index).unwrap_or(Rect {
            x_min: 0,
            y_min: 0,
            x_max: 0,
            y_max: 0,
        });

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

    debug!("total glyphs stored: {}", rects.len());
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
            debug!(
                "Resized the atlas width from: {} -> {}",
                desired_width, atlas_width
            );
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
    debug!("Pushed new height: {}", first_height);

    for glyph in glyph_data {
        let (current_scaled_width, current_scaled_height) =
            glyph.get_scaled_glyph_dimensions_with_padding(args);

        let next_width = atlas_width + current_scaled_width;
        if next_width >= max_width {
            // We don't need to scale because we've already scaled the current_height
            atlas_height += line_heights[line_count];
            line_heights.push(current_scaled_height);
            debug!("Pushed new height: {}", current_scaled_height);
            line_count += 1;

            flush_chars();
            debug!(
                "Line Number: {} | Line Width: {}",
                line_count - 1,
                atlas_width
            );
            atlas_width = current_scaled_width;
        } else {
            atlas_width = next_width;
        }

        track_char(glyph.unicode);
    }

    // If we haven't finished the end of the line, we need to add the height from the last glyph.
    if atlas_width < max_width {
        // Do not scale the last height because it is already scaled
        let last_height = line_heights[line_count];
        debug!("Pushed last height: {}", last_height);
        flush_chars();
        atlas_height += last_height;
    }

    if args.scale_texture_to_po2 {
        debug!("Original Height: {}", atlas_height);
        atlas_height = get_next_power_of_2(atlas_height);
        debug!("New Height: {}", atlas_height);
    }
    (max_width as u32, atlas_height as u32, line_heights)
}

#[derive(Copy, Clone)]
pub struct ThreadMetadata {
    pub start: u32,
    pub work_unit: u32,
}

impl ThreadMetadata {
    #[inline(always)]
    fn get_slice_offsets(&self) -> (usize, usize) {
        (self.start as usize, (self.start + self.work_unit) as usize)
    }
}
