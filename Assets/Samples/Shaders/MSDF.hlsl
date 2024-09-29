#ifndef URP_UNLIT_FORWARD_PASS_INCLUDED
#define URP_UNLIT_FORWARD_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define inverseLerp(a, b, x) ((x - a) / (b - a))

struct Attributes {
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    float2 unitRange : TEXCOORD1;
    float4 color : COLOR;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings {
    float2 uv : TEXCOORD0;
    float2 unitRange : TEXCOORD1;
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

    output.positionCS = TransformObjectToHClip(input.positionOS);
    output.uv = input.uv;
    output.color = input.color;
    output.unitRange = input.unitRange;

    return output;
}

float median(float r, float g, float b) {
	return max(min(r, g), min(max(r, g), b));
}

float screenPxRange(float2 uv, float2 unitRange) {
    float2 screenTexSize = half2(1.0, 1.0) / fwidth(uv);
    return max(1, dot(unitRange, screenTexSize));
}

float screenPxRange(float2 uv) {
    float2 unitRange = 6.0 / _BaseMap_ST.zw;
    float2 screenTexSize = 1.0 / fwidth(uv);
    return max(0.5 * dot(unitRange, screenTexSize), 1.0);
}

half2 SafeNormalize(half2 v) {
    half len = length(v);
    len = len > 0.0 ? 1.0 / len : 0.0;
    return v * len;
}

float FilterSdfTextureExact(float sdf, float2 uvCoordinate, float2 textureSize) {
    // Calculate the derivative of the UV coordinate and build the jacobian-matrix from it.
    half2x2 pixelFootprint = half2x2(ddx(uvCoordinate), ddy(uvCoordinate));
    // Calculate the area under the pixel.
    // Note the use of abs(), because the area may be negative.
    float pixelFootprintDiameterSqr = abs(determinant(pixelFootprint));
    // Multiply by texture size.
    pixelFootprintDiameterSqr *= textureSize.x * textureSize.y ;
    // Compute the diameter.
    float pixelFootprintDiameter = sqrt(pixelFootprintDiameterSqr);
    // Clamp the filter width to [0, 1] so we won't overfilter, which fades the texture into grey
    pixelFootprintDiameter = saturate(pixelFootprintDiameter);
    return saturate(inverseLerp(-pixelFootprintDiameter, pixelFootprintDiameter, sdf));
}

// Look into, because ultimately i may not need to use ddx in a 2d orthographic perspective.
// https://github.com/Chlumsky/msdfgen/issues/36#issuecomment-429240110
// The example shader needs work: https://github.com/Chlumsky/msdfgen/issues/22
void UnlitPassFragment(
    Varyings input,
    out float4 outColor : SV_Target0
    #ifdef _WRITE_RENDERING_LAYERS
        , out float4 outRenderingLayers : SV_Target1
    #endif
) {
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    int totalSamples = _BoxSampleSize * _BoxSampleSize;
    float sum = 0;

    for (int dx = -_BoxSampleSize; dx <= _BoxSampleSize; dx++) {
        for (int dy = -_BoxSampleSize; dy <= _BoxSampleSize; dy++) {
            float2 neighbor = input.uv + float2(dx, dy) / _ScreenParams.xy;
            float3 sample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, neighbor).rgb;
            sum += median(sample.r, sample.g, sample.b) - _Strength;
        }
    }

    float sigDist = sum / totalSamples;
    sigDist = FilterSdfTextureExact(sigDist, input.uv, _BaseMap_TexelSize.zw);
    half t = smoothstep(0, 1, sigDist);
    outColor = t;

    // float dist = _Cutoff - sampleCol.a;
    // float2 ddist = float2(ddx(dist), ddy(dist));
    // float pixelDist = dist / length(ddist);
    // float a = saturate(0.5 - pixelDist);
    // clip(a - 0.01);
    // outColor = float4(input.color.rgb, a);

    // float pxRange = screenPxRange(input.uv);
    // float4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
    // float dist = median(texel.r, texel.g, texel.b);
    // float pxDist = pxRange * (dist - 0.5);
    // float opacity = saturate(pxDist + 0.5);
    // clip(opacity - _Cutoff);
    // outColor = float4(0.80469, 0.917969, 0.9804688, opacity);

    #if B
        float2 msdfUnit = _PxRange / _BaseMap_TexelSize.zw;

        float4 sampleCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
        float sigDist = median(sampleCol.r, sampleCol.g, sampleCol.b) - 0.5;
        sigDist *= max(dot(msdfUnit, 0.5 / fwidth(input.uv)), 1);
        float opacity = saturate(sigDist + 0.5);
        float4 color = float4(input.color.rgb, input.color.a * opacity);
        clip(color.a - _Cutoff);
        outColor = color;
    #endif

    #if A
        half2 uv = input.uv;

        float2 jdx = ddx(uv);
        float2 jdy = ddy(uv);

        half3 sample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
        half sigDist = median(sample.r, sample.g, sample.b) - _Strength;
        
        half2 gradDist = SafeNormalize(half2(ddx(sigDist), ddy(sigDist)));
        half2 grad = half2(gradDist.x * jdx.x + gradDist.y * jdy.x, gradDist.x * jdx.y + gradDist.y * jdy.y);

        // Apply anti-aliasing.
        half kNormalization = 0.125 * 0.5 * sqrt(2.0);
        half afwidth = min(kNormalization * length(grad), 0.5);
        half t = smoothstep(-afwidth, afwidth, sigDist);
        
        half a = pow(abs(input.color.a * t), 1.0 / 2.2);
        half3 rgb = half3(input.color.rgb * a);
        outColor = half4(rgb, a);
    #endif
}
#endif
