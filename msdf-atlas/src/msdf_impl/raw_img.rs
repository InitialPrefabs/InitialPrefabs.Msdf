use std::marker::PhantomData;
use std::slice;

#[allow(dead_code)]
type Rgb = [f32; 3];

#[allow(dead_code)]
pub struct RawImage<'a> {
    _phantom_data: PhantomData<&'a mut Rgb>,
    data: *mut Rgb,
    width: u32,
    height: u32,
}

#[allow(dead_code)]
impl<'a> RawImage<'a> {
    pub fn new(data: &'a mut [Rgb], width: u32, height: u32) -> Self {
        Self {
            _phantom_data: PhantomData,
            data: data.as_mut_ptr(),
            width,
            height,
        }
    }

    fn convert_xy_to_index(&self, x: u32, y: u32) -> usize {
        (y * self.width + x) as usize
    }

    fn convert_to_u8(&self) -> &[u8] {
        unsafe {
            const CHANNELS: u32 = 3;
            let byte_ptr = self.data as *mut u8;
            let len = (self.width * self.height * CHANNELS) as usize;
            slice::from_raw_parts(byte_ptr, len)
        }
    }
}

pub struct RawImageView<'a> {
    img: &'a RawImage<'a>,
    offset_x: u32,
    offset_y: u32,
    width: u32,
    height: u32,
}

unsafe impl Send for RawImageView<'_> {}

#[allow(dead_code)]
impl<'a> RawImageView<'a> {
    pub fn new(
        img: &'a RawImage<'a>,
        offset_x: u32,
        offset_y: u32,
        width: u32,
        height: u32,
    ) -> Self {
        Self {
            img,
            offset_x,
            offset_y,
            width,
            height,
        }
    }

    pub fn for_each_mut(&mut self, f: &dyn Fn(u32, u32, &mut Rgb)) {
        for y in 0..self.height {
            for x in 0..self.width {
                let p = self.get_pixel_mut_unchecked(x, y);
                f(x, y, p);
            }
        }
    }

    pub fn for_each(&mut self, f: &dyn Fn(u32, u32, &Rgb)) {
        for y in 0..self.height {
            for x in 0..self.width {
                let p = self.get_pixel_mut_unchecked(x, y);
                f(x, y, p);
            }
        }
    }

    #[inline(always)]
    pub fn get_pixel_mut_unchecked(&mut self, x: u32, y: u32) -> &mut Rgb {
        let index = self
            .img
            .convert_xy_to_index(x + self.offset_x, y + self.offset_y);
        unsafe {
            let p = self.img.data.add(index);
            &mut *p
        }
    }

    #[inline(always)]
    pub fn set_pixel_unchecked(&mut self, x: u32, y: u32, color: Rgb) {
        let index = self
            .img
            .convert_xy_to_index(x + self.offset_x, y + self.offset_y);
        unsafe { self.img.data.add(index).write(color) };
    }
}

#[cfg(test)]
mod tests {
    use rayon::ThreadPoolBuilder;

    use super::{RawImage, RawImageView};

    #[test]
    fn raw_image_view_writes_to_same_img_multithread() {
        let mut pixels = vec![[1.0, 1.0, 1.0]; 100];
        let img = RawImage::new(&mut pixels, 10, 10);
        let pool = ThreadPoolBuilder::new().num_threads(2).build().unwrap();

        let mut v1 = RawImageView::new(&img, 0, 0, 5, 10);
        let mut v2 = RawImageView::new(&img, 5, 0, 5, 10);

        let red = [1.0_f32, 0.0_f32, 0.0_f32];
        let blue = [0.0_f32, 1.0_f32, 0.0_f32];
        pool.scope(|s| {
            s.spawn(|_| {
                let _ = &v1.for_each_mut(&|_, _, p| {
                    *p = red;
                });
            });

            s.spawn(|_| {
                let _ = &v2.for_each_mut(&|_, _, p| {
                    *p = blue;
                });
            });
        });

        v1.for_each(&|_, _, p| {
            for (i, c) in p.iter().enumerate() {
                assert!((red[i] - c).abs() <= f32::EPSILON);
            }
        });

        v2.for_each(&|_, _, p| {
            for (i, c) in p.iter().enumerate() {
                assert!((blue[i] - c).abs() <= f32::EPSILON);
            }
        });
    }
}
