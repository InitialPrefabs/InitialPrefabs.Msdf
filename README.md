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
