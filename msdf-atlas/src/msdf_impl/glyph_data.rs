pub struct GlyphData {
    pub unicode: i32,
    pub advance: f32,
    metrics_x: f32,
    metrics_y: f32,
    bearings_x: f32,
    bearings_y: f32,
    uv_x: f32,
    uv_y: f32,
    uv_z: f32,
    uv_w: f32,
}

#[allow(dead_code)]
impl GlyphData {
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

    pub fn with_advance(mut self, advance: f32) -> GlyphData {
        self.advance = advance;
        self
    }

    pub fn with_metrics(mut self, x: f32, y: f32) -> GlyphData {
        self.metrics_x = x;
        self.metrics_y = y;
        self
    }

    pub fn with_bearings(mut self, x: f32, y: f32) -> GlyphData {
        self.bearings_x = x;
        self.bearings_y = y;
        self
    }

    pub fn with_min_uv(mut self, x: f32, y: f32) -> GlyphData {
        self.uv_x = x;
        self.uv_y = y;
        self
    }

    pub fn with_max_uv(mut self, x: f32, y: f32) -> GlyphData {
        self.uv_z = x;
        self.uv_w = y;
        self
    }

    pub fn metrics(&self) -> (f32, f32) {
        (self.metrics_x, self.metrics_y)
    }

    pub fn bearings(&self) -> (f32, f32) {
        (self.bearings_x, self.bearings_y)
    }

    pub fn uvs(&self) -> (f32, f32, f32, f32) {
        (self.uv_x, self.uv_y, self.uv_z, self.uv_w)
    }
}
