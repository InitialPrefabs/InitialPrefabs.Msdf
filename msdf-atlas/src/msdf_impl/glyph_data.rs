use mint::Vector2;
use ttf_parser::Face;

use crate::msdf_impl::uv_space::UVSpace;

pub struct GlyphData {
    pub unicode: i32,
    pub advance: f32,
    // The width of the rectangle for the font
    metrics_x: f32,
    // The height of the rectangle for the font
    metrics_y: f32,
    bearings_x: f32,
    bearings_y: f32,
    // The following represent the min, max uvs within an atlas.
    // This is typically filled afterwards, because we are generating
    // the atlases ourselves
    uv_x: f32,
    uv_y: f32,
    uv_z: f32,
    uv_w: f32,
}

#[allow(dead_code)]
impl GlyphData {
    pub fn from_glyph(c: char, face: &Face) -> Self {
        let glyph_data = GlyphData::from_char(c);
        let glyph_id = face.glyph_index(c).unwrap();

        let bounding_box = face.glyph_bounding_box(glyph_id).unwrap();

        glyph_data
            .with_advance(face.glyph_hor_advance(glyph_id).unwrap())
            .with_metrics(bounding_box.width(), bounding_box.height())
            .with_bearings(face.glyph_hor_side_bearing(glyph_id).unwrap(), 0)
    }

    pub fn from_char(c: char) -> Self {
        Self {
            unicode: c as i32,
            advance: 0.0,
            metrics_x: 0.0,
            metrics_y: 0.0,
            bearings_x: 0.0,
            bearings_y: 0.0,
            uv_x: 0.0,
            uv_y: 0.0,
            uv_z: 0.0,
            uv_w: 0.0,
        }
    }

    pub fn from_unicode(unicode: i32) -> Self {
        Self {
            unicode,
            advance: 0.0,
            metrics_x: 0.0,
            metrics_y: 0.0,
            bearings_x: 0.0,
            bearings_y: 0.0,
            uv_x: 0.0,
            uv_y: 0.0,
            uv_z: 0.0,
            uv_w: 0.0,
        }
    }

    pub fn with_advance(mut self, advance: u16) -> GlyphData {
        self.advance = advance as f32;
        self
    }

    pub fn with_metrics(mut self, x: i16, y: i16) -> GlyphData {
        self.metrics_x = x as f32;
        self.metrics_y = y as f32;
        self
    }

    pub fn with_bearings(mut self, x: i16, y: i16) -> GlyphData {
        self.bearings_x = x as f32;
        self.bearings_y = y as f32;
        self
    }

    #[deprecated]
    pub fn with_min_uv(mut self, x: i16, y: i16) -> GlyphData {
        self.uv_x = x as f32;
        self.uv_y = y as f32;
        self
    }

    #[deprecated]
    pub fn with_max_uv(mut self, x: i16, y: i16) -> GlyphData {
        self.uv_z = x as f32;
        self.uv_w = y as f32;
        self
    }

    pub fn with_uvs(mut self, start: Vector2<i32>, end: Vector2<i32>, dimensions: Vector2<i32>, uv_space: UVSpace) -> GlyphData {
        let width = dimensions.x as f32;
        let height = dimensions.y as f32;

        let x_min = start.x as f32 / width;
        let y_min = start.y as f32 / height;

        let x_max = end.x as f32 / width;
        let y_max = end.y as f32 / height;

        self.uv_x = if uv_space.bitwise_and(UVSpace::OneMinusU) { 1.0 - x_min } else { x_min };
        self.uv_y = if uv_space.bitwise_and(UVSpace::OneMinusV) { 1.0 - y_min } else { y_min };

        self.uv_z = if uv_space.bitwise_and(UVSpace::OneMinusU) { 1.0 - x_max } else { x_max };
        self.uv_w = if uv_space.bitwise_and(UVSpace::OneMinusV) { 1.0 - y_max } else { y_max };

        self
    }

    /// Returns the size of the recntangle for the character in the font
    pub fn metrics(&self) -> (f32, f32) {
        (self.metrics_x, self.metrics_y)
    }

    pub fn bearings(&self) -> (f32, f32) {
        (self.bearings_x, self.bearings_y)
    }

    /// Returns the uv locations of the letter in the texture.
    pub fn uvs(&self) -> (f32, f32, f32, f32) {
        (self.uv_x, self.uv_y, self.uv_z, self.uv_w)
    }
}

impl ToString for GlyphData {
    fn to_string(&self) -> String {
        format!("Unicode: {}, Char: {} | Metrics: ({}, {}) | Bearings: ({}, {}) | Advance: {} | BoundingBox: ({}, {}), ({}, {})", 
            self.unicode, 
            char::from_u32(self.unicode as u32).unwrap(),
            self.metrics_x, 
            self.metrics_y, 
            self.bearings_x, 
            self.bearings_y, 
            self.advance, 
            self.uv_x, 
            self.uv_y, 
            self.uv_z, 
            self.uv_w)
    }
}

impl PartialEq for GlyphData {
    fn eq(&self, other: &Self) -> bool {
        let (metrics_x, metrics_y) = other.metrics();
        let (min_x, min_y, max_x, max_y) = other.uvs();
        let (bearings_x, bearings_y) = other.bearings();

        self.unicode == other.unicode
            && self.advance == other.advance
            && metrics_x == metrics_x
            && metrics_y == metrics_y
            && min_x == other.uv_x
            && min_y == other.uv_y
            && max_x == other.uv_z
            && max_y == other.uv_w
            && bearings_x == other.bearings_x
            && bearings_y == other.bearings_y
    }
}

