#ifndef MSDF_INPUT_INCLUDED
#define MSDF_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half _DistanceFactor;
    half _Cutoff;
CBUFFER_END

#endif
