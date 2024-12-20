# InitialPrefabs.Msdf

This repo provides an integration with [MSDF](https://github.com/Chlumsky/msdfgen), created by Viktor Chlumský. The 
integration primary uses Rust with bindings generated for C#. An example of the MSDF Atlas can be seen below:

![atlas](https://github.com/InitialPrefabs/InitialPrefabs.Msdf/blob/main/Assets/com.initialprefabs.msdfgen/Example/FontAtlas/UbuntuMonoNerdFontMono-Regular_MSDFAtlas.png?raw=true)

---

It generates a font atlas like above with the proper UVs for you to do Font Rendering. Information such as
* Face Info
    * Ascender
    * Descender
    * Line height
    * Units per em
* Glyph Info
    * Bearings
    * Advance
    * Metrics / Size
    * Uvs

are provided through the `SerializedFontData`. By default, the `SerializedFontData` is sorted by unicode (smallest to largest). 
This allows you to use whatever search algorithm you want.

---

## What are the benefits of MSDF rendering?
![msdf vs sdf 1](https://github.com/InitialPrefabs/InitialPrefabs.Msdf/blob/main/msdf-comparison-to-sdftmp-1.png?raw=true)
![msdf vs sdf 2](https://github.com/InitialPrefabs/InitialPrefabs.Msdf/blob/main/msdf-comparison-to-sdftmp-2.png?raw=true)

MSDF rendering provides a smaller texture foot print for high quality fonts compared to SDF font rendering. Take a look at the images above
comparing how SDF handles sharp corners and with how MSDF handles sharp corners.

To get a better quality for font rendering with SDF, you would need to scale the texture up and provide more info. This is how TextMeshPro 
provides higher quality fonts. For more information, please read Viktor Chlumský's [master thesis](https://github.com/Chlumsky/msdfgen/files/3050967/thesis.pdf).

## Install
* OpenUPM integration is coming a bit later for now, add this through your package manager via git URL.

## Workflow
![workflow](https://github.com/InitialPrefabs/InitialPrefabs.Msdf/blob/main/editor-workflow.png)

To access the above menu, go to `Tools -> InitialPrefabs -> Atlas Generator` in the toolbar.

1. Select a directory using the **Select Export Directory**.
2. Select a font asset
3. Add in the default characters you want to include in the atlas
    - a. _Your font must support the characters you want to include. If the characters don't exist in the font, then glyph information cannot be extracted!_
4. (Optional) If you want to scale the texture to the nearest power of 2, click on **Scale Texture**. 
    - a. _**Please note** that scaling the texture may leave undesired empty space._
5. Select your **DownScale** option, the bigger the #, the smaller the glyphs will be in the atlas.
6. Select your **Max Atlas Width**, this supports 128, 256, 512, 1024, 2048, 4096 as a max atlas width.
7. (Optional). Add padding between your glyphs, via the **Padding** field.
8. Select your **Range**. A bigger # means that you will have smoother transitions of the glyphs' edges. A smaller number provides a more focused distance field, but 
loses detail the further you are away from the edge.
9. Select the **UV Space**. `One Minus V` is the default for Unity, so that the glyphs aline the rendering engine's sampler.
10. Select your **Color Type**. Currently supported are: Simple, Ink Trap, and Distance.
11. Select your **Degrees**. This helps the algorithm determine what's considered a corner in the character when generating the glyphs.
12. Select the **# of Threads** to execute the work on. 
    - a. _Be mindful of selecting the total # of threads relative to the # of glyphs/chars you want to goenerate. Each thread is responsible for `total_num_of_glyphs / thread_count`._

## Importing

InitialPrefabs.MSDF makes use of [InitialPrefabs.ImportOverrides](https://github.com/InitialPrefabs/ImportOverrides). This allows for easy configuration 
of the TextureImporterSettings.

A default `FontAtlasImportConfig` is provided which 
* Removes the alpha channel
* Removes mipmaps
* Removes filtering
* Removes compression

This is queried by: `FontAtlasTextureImporter`, which attempts to get the first **primary** `FontAtlasImportConfig`. You can create your own for your
project by right clicking in the project and going to `Create -> InitialPrefabs -> MSDF -> FontAtlasImportConfig`.

![font-atlas-import-config-ui](https://github.com/InitialPrefabs/InitialPrefabs.Msdf/blob/main/font-atlas-import-configs.png)

1. To make your custom `FontAtlasImportConfig` the **primary** config, toggle the **Is Primary** field on.
2. Add any file pattern to the **File Pattern** field to post process any incoming textures.
    - a. By default the Atlas Generator names all generated files with **_MSDSAtlas.png** suffix.
    - b. This field is a regex field.
3. Add any `TextureImporterSettings` and `TextureImporterPlatformSettings` to the **Per Platform Settings** array.
    - a. The default one only configures Standalone in the repo.

## Advance Usages
1. If you don't want the importer to do any post processing, add the define: `DISABLE_MSDF_IMPORT` to your project's scripting define.
2. If you want **binary serialization** for the `SerializedFontData`, add the define `MSDF_BINARY` to your project's scripting define.

### Example

An example shader and glyph rendering is provided in this repo, but is by no means a good way of handling text rendering with layouts. It's simply a demo to provide 
basic details on how text rendering can work. You can view this in `com.initialprefabs.msdfgen/Example/Scenes/SampleScene.unity`.

## Using this as a lib
The `msdf-atlas` is a rust project which generates a dll. You are welcome to use that dll and call `get_glyph_data_utf16` to generate an atlas. Please keep in mind 
that the glyph data is returned as pointer so you will need to reinterpret it back to its structured data.

Please view `byte_buffer.rs` tests and source code to see how to interpret a pointer at an index to `GlyphData`.

## Acknowledgements
* Viktor Chlumský for the master thesis and providing MSDFGen to play around with.
* [Cysharp's csbindgen](https://github.com/Cysharp/csbindgen/) for an automated code generation of bindings from Rust -> C#.