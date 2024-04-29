use regex::bytes::Regex;
use std::io::Error;
use std::{fs::File, io::Read};
use std::result::Result;

// use ttf_parser::{Face, FaceParsingError};
/// [TODO:description]
///
/// # Arguments
///
/// * `file_path` - The absolute path to an otf or ttf file.
///
/// # Panics
///
/// If the file is not an otf or ttf font, then the method will early out and exit.
///
/// # Errors
///
/// If the file is corrupted, the file will not run.
#[allow(unused)]
pub fn load_font_file(file_path: &str) -> Result<Vec<u8>, Error> {
    let r = Regex::new(r"\.(otf|ttf)$").unwrap();
    if !r.is_match(file_path.as_bytes()) {
        panic!("The file is not an otf or ttf file!");
    }

    let mut file = File::options()
        .read(true)
        .write(false)
        .open(file_path)?;

    let mut buffer = Vec::new();
    file.read_to_end(&mut buffer)?;

    Ok(buffer)
}

// pub fn load_face(c: char) {
    // // Load a font from ttf data.
    // let face = Face::from_slice(data, index)
    // let glyph_index = face.glyph_index('W').unwrap();

    // // Load a glyph into a shape using a ttf glyph index.
    // let shape = face.load_shape(glyph_index).unwrap();

    // // Not a required step for SDF and Psuedo-SDF generation. Other coloring options exist.
    // let colored_shape = shape.color_edges_simple(3.0);

    // // Project glyph down by a factor of 64x.
    // let projection = Projection {
    //     scale: Vector2 {
    //         x: 1.0 / 64.0,
    //         y: 1.0 / 64.0,
    //     },
    //     translation: Vector2 { x: 0.0, y: 0.0 },
    // };

    // // Using default configuration.
    // let sdf_config = Default::default();
    // let msdf_config = Default::default();

    // // Generate all types of SDF. Plain SDFs and Psuedo-SDFs do not require edge coloring.
    // let sdf = colored_shape.generate_sdf(32, 32, 10.0 * 64.0, &projection, &sdf_config);
    // let psdf = colored_shape.generate_psuedo_sdf(32, 32, 10.0 * 64.0, &projection, &sdf_config);
    // let msdf = colored_shape.generate_msdf(32, 32, 10.0 * 64.0, &projection, &msdf_config);
    // let mtsdf = colored_shape.generate_mtsdf(32, 32, 10.0 * 64.0, &projection, &msdf_config);
// }
