#ifndef URP_UNLIT_FORWARD_PASS_INCLUDED
#define URP_UNLIT_FORWARD_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Unlit.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;

    #if defined(DEBUG_DISPLAY)
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv : TEXCOORD0;
    float fogCoord : TEXCOORD1;
    float4 positionCS : SV_POSITION;

    #if defined(DEBUG_DISPLAY)
    float3 positionWS : TEXCOORD2;
    float3 normalWS : TEXCOORD3;
    float3 viewDirWS : TEXCOORD4;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, out InputData inputData)
{
    inputData = (InputData)0;

    #if defined(DEBUG_DISPLAY)
    inputData.positionWS = input.positionWS;
    inputData.normalWS = input.normalWS;
    inputData.viewDirectionWS = input.viewDirWS;
    #else
    inputData.positionWS = float3(0, 0, 0);
    inputData.normalWS = half3(0, 0, 1);
    inputData.viewDirectionWS = half3(0, 0, 1);
    #endif
    inputData.shadowCoord = 0;
    inputData.fogCoord = 0;
    inputData.vertexLighting = half3(0, 0, 0);
    inputData.bakedGI = half3(0, 0, 0);
    inputData.normalizedScreenSpaceUV = 0;
    inputData.shadowMask = half4(1, 1, 1, 1);
}

Varyings UnlitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    output.positionCS = vertexInput.positionCS;
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    #if defined(_FOG_FRAGMENT)
    output.fogCoord = vertexInput.positionVS.z;
    #else
    output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
    #endif

    #if defined(DEBUG_DISPLAY)
    // normalWS and tangentWS already normalize.
    // this is required to avoid skewing the direction during interpolation
    // also required for per-vertex lighting and SH evaluation
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    half3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);

    // already normalized from normal transform to WS.
    output.positionWS = vertexInput.positionWS;
    output.normalWS = normalInput.normalWS;
    output.viewDirWS = viewDirWS;
    #endif

    return output;
}

float median(float r, float g, float b) {
    return max(min(r, g), min(max(r, g), b));
}

float screenPxRange(float2 uv) {
    float2 unitRange = _PxRange / _BaseMap_TexelSize.zw;
    float2 screenTexSize = 1.0 / fwidth(uv);
    return max(0.5 * dot(unitRange, screenTexSize), 1.0);
}

void UnlitPassFragment(
    Varyings input
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // The example shader needs work: https://github.com/Chlumsky/msdfgen/issues/22
    half2 uv = input.uv;
    half3 msd = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
    float sd = median(msd.r, msd.g, msd.b);
    float dx = ddx(uv.x) * _BaseMap_TexelSize.z;
    float dy = ddy(uv.y) * _BaseMap_TexelSize.w;
    float toPixels = 8.0 * 1.0 / sqrt(dx * dx + dy * dy);
    float sigDist = median(msd.r, msd.g, msd.b);
    float w = fwidth(sigDist);
    float opacity = smoothstep(0.5 - w, 0.5 + w, sigDist);
    outColor = lerp(_BackgroundColor, _ForegroundColor, opacity);

#if _
    float w = fwidth(sd);
    float opacity = smoothstep(0.5 - w, 0.5 + w, sd);
    outColor = lerp(_BackgroundColor, _ForegroundColor, opacity);
#endif

#if A
    float screenPxDistance = screenPxRange(uv) * (sd - 0.5);
    float opacity = clamp(screenPxDistance + 0.5, 0.0, 1.0);
    outColor = lerp(_BackgroundColor, _ForegroundColor, opacity);
#endif
    // outColor = half4(_ForegroundColor.rgb, opacity);
}
#endif
