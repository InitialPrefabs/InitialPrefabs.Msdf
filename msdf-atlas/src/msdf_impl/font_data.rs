use super::byte_buffer::ByteBuffer;

#[repr(C)]
#[derive(Clone)]
pub struct FontData {
    // TODO: Add more metrics
    pub line_height: i32,
    /// Treat units_per_em as the font size?
    pub units_per_em: u32,
    pub ascender: i32,
    pub descender: i32,
    pub glyph_data: *mut ByteBuffer,
}

impl Drop for FontData {
    fn drop(&mut self) {
        unsafe {
            if !self.glyph_data.is_null() {
                (*self.glyph_data).destroy();
            }
        }
    }
}
