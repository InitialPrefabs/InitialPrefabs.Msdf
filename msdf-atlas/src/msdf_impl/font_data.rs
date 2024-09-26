use super::byte_buffer::ByteBuffer;

#[repr(C)]
#[derive(Copy, Clone)]
pub struct FontData {
    // TODO: Add more metrics
    pub line_height: i32,
    pub units_per_em: u32,
    pub glyph_data: *mut ByteBuffer,
}
