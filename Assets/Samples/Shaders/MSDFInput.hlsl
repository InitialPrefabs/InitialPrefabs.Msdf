#ifndef MSDF_INPUT_INCLUDED
#define MSDF_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float _PxRange;
    float _Cutoff;
    float _Strength;
    int _BoxSampleSize;
CBUFFER_END

#endif
