use std::mem::ManuallyDrop;

#[repr(C)]
#[derive(Copy, Clone)]
pub struct ByteBuffer {
    ptr: *mut u8,
    length: i32,
    capacity: i32,
    element_size: i32
}

impl ByteBuffer {
    pub fn len(&self) -> usize {
        self.length
            .try_into()
            .expect("buffer length negative or overflowed")
    }

    pub fn from_vec(bytes: Vec<u8>) -> Self {
        let length = i32::try_from(bytes.len()).expect("buffer length cannot fit into a i32.");
        let capacity =
            i32::try_from(bytes.capacity()).expect("buffer capacity cannot fit into a i32.");

        // keep memory until call delete
        let mut v = ManuallyDrop::new(bytes);

        Self {
            element_size: size_of::<u8>() as i32,
            ptr: v.as_mut_ptr(),
            length,
            capacity,
        }
    }

    pub fn from_vec_struct<T>(bytes: Vec<T>) -> Self 
        where T: Sized + PartialEq {
        let element_size = size_of::<T>() as i32;

        let length = (bytes.len() as i32) * element_size;
        let capacity = (bytes.capacity() as i32) * element_size;

        let mut v = ManuallyDrop::new(bytes);

        Self {
            element_size,
            ptr: v.as_mut_ptr() as *mut u8,
            length,
            capacity,
        }
    }

    /// Returns the length of this [`ByteBuffer`] as if it was treated like an array.
    pub fn element_len(&self) -> i32 {
        self.length / self.element_size
    }

    pub fn element_at<T>(&self, i: usize) -> T
        where T: Sized + Copy {
        let head = self.ptr as *const T;
        unsafe {
            *(head.add(i))
        }
    }

    pub fn destroy_into_vec(self) -> Vec<u8> {
        if self.ptr.is_null() {
            vec![]
        } else {
            let capacity: usize = self
                .capacity
                .try_into()
                .expect("buffer capacity negative or overflowed");
            let length: usize = self
                .length
                .try_into()
                .expect("buffer length negative or overflowed");

            unsafe { Vec::from_raw_parts(self.ptr, length, capacity) }
        }
    }

    pub fn destroy_into_vec_struct<T: Sized>(self) -> Vec<T> {
        if self.ptr.is_null() {
            vec![]
        } else {
            let element_size = size_of::<T>() as i32;
            let length = (self.length * element_size) as usize;
            let capacity = (self.capacity * element_size) as usize;

            unsafe { Vec::from_raw_parts(self.ptr as *mut T, length, capacity) }
        }
    }

    pub fn destroy(self) {
        drop(self.destroy_into_vec());
    }
}

#[cfg(test)]
mod tests {
    use super::ByteBuffer;

    #[derive(Clone, Copy, Debug)]
    struct Custom {
        pub i: i32,
    }

    impl PartialEq for Custom {
        fn eq(&self, other: &Self) -> bool {
            self.i == other.i
        }
    }

    #[test]
    fn byte_buffer_stores_struct() {
        let mut vec: Vec<Custom> = Vec::with_capacity(10);
        let mut const_vec: Vec<Custom> = Vec::with_capacity(10);
        for i in 0..vec.capacity() {
            let custom_struct = Custom {
                i: i as i32
            };

            vec.push(custom_struct);
            const_vec.push(custom_struct);
        }

        let vec_len = vec.len() as i32;
        assert_eq!(vec_len, vec.capacity() as i32);

        let byte_buffer = ByteBuffer::from_vec_struct(vec);
        assert_eq!(vec_len, byte_buffer.element_len());

        (0..vec_len as usize).for_each(|i| {
            let actual = byte_buffer.element_at::<Custom>(i);
            let expected = const_vec[i];
            assert_eq!(actual, expected);
        });
    }
}
