use bitflags::bitflags;

#[repr(C)]
pub enum ColorType {
    Simple,
    InkTrap,
    Distance,
}

bitflags! {
    #[repr(C)]
    #[derive(Debug, Clone, Copy, PartialEq, Eq, PartialOrd, Ord, Hash)]
    pub struct UVSpace : u32 {
        const Default = 0;
        const OneMinusU = 1 << 0;
        const OneMinusV = 1 << 1;
    }
}