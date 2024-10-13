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

## Using this package