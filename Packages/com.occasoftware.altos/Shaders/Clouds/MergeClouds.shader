Shader "Hidden/OccaSoftware/Altos/MergeClouds"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        
        ZWrite Off
        Cull Off
        ZTest Always
        
        Pass
        {
            Name "Merge Clouds"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            Texture2D _ScreenTexture;
            int _CLOUD_DEPTH_CULL_ON;
            Texture2D _MERGE_PASS_INPUT_TEX;


            #define DEBUG_CLOUD_SHADOWS 0
            #if DEBUG_CLOUD_SHADOWS
            Texture2D _CLOUD_SHADOW_CURRENT_FRAME;
            #endif

            SamplerState point_clamp_sampler;
            SamplerState linear_clamp_sampler;

            float3 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 screenColor = _ScreenTexture.SampleLevel(point_clamp_sampler, input.texcoord, 0).rgb;
                float4 cloudColor = _MERGE_PASS_INPUT_TEX.SampleLevel(point_clamp_sampler, input.texcoord, 0);
                float3 outputColor = screenColor.rgb * cloudColor.a + cloudColor.rgb;
                float3 finalColor = outputColor;
                
                #if UNITY_REVERSED_Z
                    float depth = SampleSceneDepth(input.texcoord);
                #else
                    float depth = 1.0 - SampleSceneDepth(input.texcoord);
                #endif
    
                #if DEBUG_CLOUD_SHADOWS
                if (input.texcoord.x > 0.8 && input.texcoord.y > 0.8)
                {
                    return saturate(_CLOUD_SHADOW_CURRENT_FRAME.SampleLevel(linear_clamp_sampler, (input.texcoord.xy - 0.8) * 5, 0).rgb);
                }
                #endif
    
                if (depth <= 0.0)
                {
                    return outputColor;
                }
                else
                {
                    if (_CLOUD_DEPTH_CULL_ON == 1)
                    {
                        return outputColor;
                    }
                    else
                    {
                        return screenColor;
                    }
                }
            }
            ENDHLSL
        }
    }
}