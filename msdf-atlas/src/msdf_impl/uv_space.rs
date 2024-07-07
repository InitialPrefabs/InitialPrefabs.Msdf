#[derive(Copy, Clone)]
pub enum UVSpace {
    Default = 0,
    OneMinusU = 1 << 0,
    OneMinusV = 1 << 1,
}

impl UVSpace {
    pub fn bitwise_and(self, other: UVSpace) -> bool {
        (self as i32 & other as i32) > 0
    }
}
