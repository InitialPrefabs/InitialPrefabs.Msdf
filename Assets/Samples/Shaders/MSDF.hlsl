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

Varyings UnlitPassVertex(Attributes input) {
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.positionCS = TransformObjectToHClip(input.positionOS);
    output.uv = input.uv;
    output.color = input.color;

    return output;
}

float median(float r, float g, float b) {
    return max(min(r, g), min(max(r, g), b));
}

// Look into, because ultimately i may not need to use ddx in a 2d orthographic perspective.
// https://github.com/Chlumsky/msdfgen/issues/36#issuecomment-429240110
// The example shader needs work: https://github.com/Chlumsky/msdfgen/issues/22
void UnlitPassFragment(
    Varyings input
    , out half4 outColor : SV_Target0
    #ifdef _WRITE_RENDERING_LAYERS
        , out float4 outRenderingLayers : SV_Target1
    #endif
) {
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half2 uv = input.uv;
    half3 sample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
    float sigDist = median(sample.r, sample.g, sample.b) - 0.5;
    // Calculate the derivatives
    float2 dux = ddx(uv);
    float2 duy = ddy(uv);

    float3 gradDist = SafeNormalize(float3(ddx(sigDist), ddy(sigDist), 0));
    float2 grad = float2(gradDist.x * dux.x + gradDist.y * duy.x, gradDist.x * dux.y + gradDist.y * duy.y);

    // Apply Anti aliasing
    const float kThickness = 0.125;
    const float kNormalization = kThickness * 0.5 * sqrt(2.0);
    float afWidth = min(kNormalization * length(grad), 0.5);
    float opacity = smoothstep(0.0 - afWidth, 0.0 + afWidth, sigDist);
    float4 col = input.color;
    outColor = opacity;
    col.a *= opacity;
    col.rgb = lerp(col.rgb, col.rgb * opacity, 0);
    outColor = col;
}
#endif
