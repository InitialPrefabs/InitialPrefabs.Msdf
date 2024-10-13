use bitflags::bitflags;
use std::fmt::Display;

const UV_SPACE_NAMES: [&str; 2] = ["1 - u", "1 - v"];

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

impl Display for UVSpace {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        let mut sb = String::with_capacity(64);

        let bits = self.bits();
        (0..2).for_each(|i| {
            let flag = (1 << i) as u32;
            if flag & bits > 0 {
                sb.push_str(UV_SPACE_NAMES[i]);
                sb.push_str(" | ");
            }
        });

        if sb.is_empty() {
            sb.push_str("Use Default");
        }

        write!(f, "{}", sb)
    }
}
