Shader "Hidden/OccaSoftware/Altos/TemporalAA"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        
        ZWrite Off
        Cull Off
        ZTest Always
        
        Pass
        {
            Name "Temporal AA"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.occasoftware.altos/ShaderLibrary/GetCameraMotionVectors.hlsl"
            #include "Packages/com.occasoftware.altos/ShaderLibrary/TemporalAA.hlsl"

            Texture2D _PREVIOUS_TAA_CLOUD_RESULTS;
            Texture2D _CURRENT_TAA_FRAME;
            float _TAA_BLEND_FACTOR;

            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float2 motionVectors = GetMotionVector(input.texcoord);
                return TemporalAA(_PREVIOUS_TAA_CLOUD_RESULTS, _CURRENT_TAA_FRAME, input.texcoord, _TAA_BLEND_FACTOR, motionVectors);
            }
            ENDHLSL
        }
    }
}