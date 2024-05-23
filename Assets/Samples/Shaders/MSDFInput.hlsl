#ifndef MSDF_INPUT_INCLUDED
#define MSDF_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _ForegroundColor;
    half4 _BackgroundColor;
    half _PxRange;
    half _Cutoff;
    half _Surface;
CBUFFER_END

#endif
