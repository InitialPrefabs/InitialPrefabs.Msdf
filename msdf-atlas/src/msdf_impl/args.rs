use crate::msdf_impl::uv_space::UVSpace;
use mint::Vector2;

#[repr(C)]
pub struct Args {
    /// Stores the angle in degrees for coloring the shape
    pub angle: f32,
    /// Scale of the generated glyphs. Recommended to use powers of 1 / 2^n.
    pub uniform_scale: f32,
    pub padding: u32,
    pub max_atlas_width: u32,
    pub point_size: u32,
    // TODO: Add the path for where we want to store the atlas
    pub uv_space: UVSpace,
}

impl Args {
    /// Generates arguments with default settings with angle of
    /// 3 degrees and no adjustments to the scale.
    #[allow(dead_code)]
    pub fn default() -> Self {
        Self {
            angle: 3.0,
            uniform_scale: 1.0,
            padding: 0,
            max_atlas_width: 512,
            point_size: 12,
            uv_space: UVSpace::Default,
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

    /// Builder to adjust the angle separately.
    ///
    /// # Arguments
    ///
    /// * `angle` - The angle in degrees.
    #[allow(dead_code)]
    pub fn with_angle(mut self, angle: f32) -> Args {
        self.angle = angle;
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

    /// Builder to adjust the font reference.
    ///
    /// # Arguments
    ///
    /// * `point_size` - The size of the font
    #[allow(dead_code)]
    pub fn with_point_size(mut self, point_size: u32) -> Args {
        self.point_size = point_size;
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
    pub fn scale_dimension(&self, unit: i16) -> i32 {
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
}
