use std::{ffi::OsString, os::windows::ffi::OsStringExt};

/// Converts a u16 buffer to an OsString
///
/// # Arguments
///
/// * `ptr` - A pointer to a continuous array of u16
pub fn convert_u16_to_os_string(ptr: *const u16) -> OsString {
    unsafe {
        let mut len = 0;
        while *ptr.add(len) != 0 {
            len += 1;
        }

        let slice = std::slice::from_raw_parts(ptr, len);
        OsString::from_wide(slice)
    }
}

/// Convert a u16 buffer to a UTF8 encoded string
///
/// # Arguments
///
/// * `ptr` - A pointer to a continuous array of u16
pub fn convert_u16_to_string(ptr: *const u16) -> String {
    unsafe {
        let mut len = 0;
        while *ptr.add(len) != 0 {
            len += 1;
        }

        let slice = std::slice::from_raw_parts(ptr, len);
        String::from_utf16(slice).unwrap()
    }
}
