use crate::msdf_impl::glyph_data::GlyphData;

// TODO: Make this FFI Safe
#[repr(C)]
pub struct GlyphPackage {
    pub units_per_em : u32,
    pub glyph_data: Vec<GlyphData>
}

