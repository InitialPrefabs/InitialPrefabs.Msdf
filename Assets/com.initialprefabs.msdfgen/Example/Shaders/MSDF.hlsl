#ifndef URP_UNLIT_FORWARD_PASS_INCLUDED
#define URP_UNLIT_FORWARD_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes {
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    float4 color : COLOR;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings {
    float2 uv : TEXCOORD0;
    float4 positionCS : SV_POSITION;
    float4 color : COLOR;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings UnlitPassVertex(Attributes input, uint vertexID : SV_VERTEXID) {
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = input.uv;
    output.color = input.color;

    return output;
}

float median(float r, float g, float b) {
	return max(min(r, g), min(max(r, g), b));
}

float ScreenPxRange(float2 uv) {
    float2 unitRange = 100 / _BaseMap_ST.zw;
    float2 screenTexSize = 1.0 / fwidth(uv);
    return max(0.5 * dot(unitRange, screenTexSize), 1.0);
}

void UnlitPassFragment(
    Varyings input,
    out float4 outColor : SV_Target0
    #ifdef _WRITE_RENDERING_LAYERS
        , out float4 outRenderingLayers : SV_Target1
    #endif
) {
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    // What the msdf author recommended. Use a constant screen px range.
    // Should code generate and set the uniform b/c we're in orthographic proj.
    // https://github.com/Chlumsky/msdfgen
    float pxRange = ScreenPxRange(input.uv);
    float4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
    float dist = median(texel.r, texel.g, texel.b);
    float pxDist = pxRange * (dist - 0.5);
    float opacity = saturate(pxDist + 0.5);
    clip(opacity - _Cutoff);
    outColor = float4(input.color.rgb, opacity);
}
#endif
