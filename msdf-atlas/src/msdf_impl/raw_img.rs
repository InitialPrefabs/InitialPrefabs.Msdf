use std::marker::PhantomData;
use std::mem;
use std::slice::{self, from_raw_parts};

use log::info;

#[allow(dead_code)]
type Rgbu8 = [u8; 3];

const RGB_CHANNELS: u32 = 3;

#[allow(dead_code)]
pub struct RawImage<'a> {
    _phantom_data: PhantomData<&'a mut Rgbu8>,
    data: *mut Rgbu8,
    pub width: u32,
    pub height: u32,
}

#[allow(dead_code)]
impl<'a> RawImage<'a> {
    pub fn new(data: &'a mut [Rgbu8], width: u32, height: u32) -> Self {
        Self {
            _phantom_data: PhantomData,
            data: data.as_mut_ptr(),
            width,
            height,
        }
    }

    pub fn treat_as_byte_array(&self, f: &dyn Fn(&[u8])) {
        unsafe {
            let total_size =
                (self.width * self.height * RGB_CHANNELS) as usize * mem::size_of::<u8>();

            let base_ptr = self.data as *mut u8;
            let raw_parts = from_raw_parts(base_ptr, total_size);
            f(raw_parts);
        }
    }

    #[inline(always)]
    fn convert_xy_to_index(&self, x: u32, y: u32) -> usize {
        (y * self.width + x) as usize
    }

    fn convert_to_u8(&self) -> &[u8] {
        unsafe {
            let byte_ptr = self.data as *mut u8;
            let len = (self.width * self.height * RGB_CHANNELS) as usize;
            slice::from_raw_parts(byte_ptr, len)
        }
    }
}

pub struct RawImageView<'a> {
    img: &'a RawImage<'a>,
    offset_x: u32,
    offset_y: u32,
    pub width: u32,
    pub height: u32,
}

unsafe impl Send for RawImageView<'_> {}
// unsafe impl Sync for RawImageView<'_> {}

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

    pub fn for_each_mut(&mut self, f: &dyn Fn(u32, u32, &mut Rgbu8)) {
        for y in 0..self.height {
            for x in 0..self.width {
                let p = self.get_pixel_mut_unchecked(x, y);
                f(x, y, p);
            }
        }
    }

    pub fn for_each(&mut self, f: &dyn Fn(u32, u32, &Rgbu8)) {
        for y in 0..self.height {
            for x in 0..self.width {
                let p = self.get_pixel_mut_unchecked(x, y);
                f(x, y, p);
            }
        }
    }

    #[inline(always)]
    pub fn get_pixel_mut_unchecked(&mut self, x: u32, y: u32) -> &mut Rgbu8 {
        let index = self
            .img
            .convert_xy_to_index(x + self.offset_x, y + self.offset_y);
        unsafe {
            let p = self.img.data.add(index);
            if p.is_null() {
                panic!("The coordinates: {}, {} does not point to a valid memory address", x, y);
            }
            &mut *p
        }
    }

    #[inline(always)]
    pub fn set_pixel_unchecked(&mut self, x: u32, y: u32, color: Rgbu8) {
        let index = self
            .img
            .convert_xy_to_index(x + self.offset_x, y + self.offset_y);
        unsafe { self.img.data.add(index).write(color) };
    }
}

#[cfg(test)]
mod tests {
    use std::sync::{Arc, Mutex};

    use image::{ImageBuffer, Rgb};
    use rayon::ThreadPoolBuilder;

    use super::{RawImage, RawImageView};

    #[test]
    fn raw_image_view_writes_to_same_img_multithread_no_mutex() {
        let mut pixels: Vec<[u8; 3]> = vec![[255, 255, 255]; 100];
        let img = RawImage::new(&mut pixels, 10, 10);
        let pool = ThreadPoolBuilder::new().num_threads(2).build().unwrap();

        let mut v1 = RawImageView::new(&img, 0, 0, 5, 10);
        let mut v2 = RawImageView::new(&img, 5, 0, 5, 10);

        let mut images = Vec::with_capacity(2);
        images.push(Arc::new(Mutex::new(v1)));
        images.push(Arc::new(Mutex::new(v2)));

        let arc_images = Arc::new(&images);

        let red = [255, 0, 0];
        let blue = [0, 0, 255];
        pool.scope(|s| {
            for i in 0..2 {
                let v = arc_images.clone();
                s.spawn(move |_| {
                    let color = if i == 0 { red } else { blue };
                    let m = &mut v[i].lock().unwrap();
                    m.for_each_mut(&|x, y, p| {
                        *p = color;
                    });
                });
            }
        });

        for (i, img) in images.iter().enumerate() {
            let v = &mut img.lock().unwrap();
            let color = if i == 0 { red } else { blue };
            let _ = &v.for_each(&|_, _, p| {
                for (accessor, c) in p.iter().enumerate() {
                    assert!(color[accessor] - c == 0);
                }
            });
        }

        img.treat_as_byte_array(&|bytes| {
            let atlas: ImageBuffer<Rgb<u8>, &[u8]> =
                ImageBuffer::from_raw(10, 10, bytes)
                    .expect("Failed to create the image");
            atlas.save("test.png").expect("Failed to save img");
        });
    }

    #[test]
    fn raw_image_view_writes_to_same_img_multithread_mutex() {
        let mut pixels: Vec<[u8; 3]> = vec![[255, 255, 255]; 100];
        let img = RawImage::new(&mut pixels, 10, 10);
        let pool = ThreadPoolBuilder::new().num_threads(2).build().unwrap();

        let v1 = Arc::new(Mutex::new(RawImageView::new(&img, 0, 0, 5, 10)));
        let v2 = Arc::new(Mutex::new(RawImageView::new(&img, 5, 0, 5, 10)));
        let group = Arc::new([Arc::clone(&v1), Arc::clone(&v2)]);

        let red = [255, 0, 0];
        let blue = [0, 0, 255];
        pool.scope(|s| {
            s.spawn(|_| {
                let mut v1 = group[0].lock().unwrap();
                let _ = &v1.for_each_mut(&|_, _, p| {
                    *p = red;
                });
            });

            s.spawn(|_| {
                let mut v2 = group[1].lock().unwrap();
                let _ = &v2.for_each_mut(&|_, _, p| {
                    *p = blue;
                });
            });
        });

        let mut v1 = RawImageView::new(&img, 0, 0, 5, 10);
        let mut v2 = RawImageView::new(&img, 5, 0, 5, 10);

        v1.for_each(&|_, _, p| {
            for (i, c) in p.iter().enumerate() {
                assert!(red[i] - c == 0);
            }
        });

        v2.for_each(&|_, _, p| {
            for (i, c) in p.iter().enumerate() {
                assert!(blue[i] - c == 0);
            }
        });
    }
}
