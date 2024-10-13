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

To make your custom `FontAtlasImportConfig` the **primary** config, toggle the **Is Primary** field on.