use crate::msdf_impl::enums::{ColorType, UVSpace};
use mint::Vector2;

#[repr(C)]
pub struct Args {
    /// Stores the angle in degrees for coloring the shape
    /// Scale of the generated glyphs. Recommended to use powers of 1 / 2^n.
    pub uniform_scale: f32,
    pub padding: u32,
    pub max_atlas_width: u32,
    pub range: f32,
    pub uv_space: UVSpace,
    pub color_type: ColorType,
    pub degrees: f32,
}

impl Args {
    /// Generates arguments with default settings with angle of
    /// 3 degrees and no adjustments to the scale.
    #[allow(dead_code)]
    pub fn default() -> Self {
        Self {
            uniform_scale: 1.0 / 64.0,
            padding: 0,
            max_atlas_width: 512,
            range: 4.0,
            uv_space: UVSpace::Default,
            degrees: 3.0,
            color_type: ColorType::Simple,
        }
    }

    /// Defines the UV Space for the generate glyphs
    ///
    /// # Arguments
    ///
    /// * `uv_space` - The uv space to operate in
    #[allow(dead_code)]
    pub fn with_uv_space(mut self, uv_space: UVSpace) -> Args {
        self.uv_space = uv_space;
        self
    }

    /// Builder to adjust the scale of the generated glyphs
    ///
    /// # Arguments
    ///
    /// * `uniform_scale` - Scale of the generated glyphs. Recommended to use powers of 1 / 2^n.
    #[allow(dead_code)]
    pub fn with_uniform_scale(mut self, uniform_scale: f32) -> Args {
        self.uniform_scale = uniform_scale;
        self
    }

    /// Builder to adjust the padding between the glyphs
    ///
    /// # Arguments
    ///
    /// * `padding` - The amount of space between each glyph in the atlas.
    #[allow(dead_code)]
    pub fn with_padding(mut self, padding: u32) -> Args {
        self.padding = padding;
        self
    }

    /// The angle determines what is considered a corner when generating an MSDF texture.
    ///
    /// # Arguments
    ///
    /// * `degrees` - The angle that is considered an edge when generating the font texture.
    pub fn with_angle(mut self, degrees: f32) -> Args {
        self.degrees = degrees;
        self
    }

    /// The ColorType determines how MSDFGen will fill the distance between an edge for each
    /// channel.
    ///
    /// # Arguments
    ///
    /// * `color_type` - The type of coloring algorithm to use.
    pub fn with_color_type(mut self, color_type: ColorType) -> Args {
        self.color_type = color_type;
        self
    }

    /// Builder to just the max atlas width.
    ///
    /// # Arguments
    ///
    /// * `max_atlas_width` - The max width of the atlas
    #[allow(dead_code)]
    pub fn with_max_atlas(mut self, max_atlas_width: u32) -> Args {
        self.max_atlas_width = max_atlas_width;
        self
    }

    #[allow(dead_code)]
    #[inline(always)]
    pub fn scale_dimension_with_padding(&self, unit: i32) -> i32 {
        self.add_padding((unit as f32 * self.uniform_scale).round() as i32)
    }

    #[allow(dead_code)]
    #[inline(always)]
    pub fn add_padding(&self, scaled_unit: i32) -> i32 {
        scaled_unit + (self.padding / 2) as i32
    }

    #[allow(dead_code)]
    pub fn get_scale(&self) -> Vector2<f64> {
        let scale = self.uniform_scale as f64;
        Vector2 { x: scale, y: scale }
    }

    #[allow(dead_code)]
    pub fn get_radians(&self) -> f64 {
        self.degrees.to_radians() as f64
    }
}
