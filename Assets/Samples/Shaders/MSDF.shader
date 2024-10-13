Shader "Unlit/MSDF"
{
    Properties
    {
        _BaseMap("Main Texture", 2D) = "white" {}
        _UnitRange("Unit Range", Float) = 4
        _Cutoff("AlphaCutout", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "UniversalMaterialType" = "Unlit"
        }

        // -------------------------------------
        // Render State Commands
        Blend One Zero
        ZTest LEqual
        ZWrite On
        Cull Back

        Pass
        {
            Name "Unlit"

            // -------------------------------------
            // Render State Commands
            AlphaToMask[_AlphaToMask]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            // -------------------------------------
            // Includes
            #include "MSDFInput.hlsl"
            #include "MSDF.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    // CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.UnlitShader"
}
