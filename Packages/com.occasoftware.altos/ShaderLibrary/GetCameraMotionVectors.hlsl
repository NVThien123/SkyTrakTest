#ifndef ALTOS_CAMERA_MOTION_VECTORS_INCLUDED
#define ALTOS_CAMERA_MOTION_VECTORS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.occasoftware.altos/ShaderLibrary/TextureUtils.hlsl"
float4x4 _ViewProjM;
float4x4 _PrevViewProjM;

float2 GetMotionVector(float2 uv)
{
   // #if UNITY_REVERSED_Z => Depth Buffer Range = [1,0]
    // #else => Depth Buffer Range = [0,1]
    
#if UNITY_REVERSED_Z
    float depth = 0;
#else
    float depth = 1.0;
#endif
    
    float4 worldPos = float4(ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP), 1.0);

    float4 prevClipPos = mul(_PrevViewProjM, worldPos);
    float4 curClipPos = mul(_ViewProjM, worldPos);

    half2 prevPosCS = prevClipPos.xy / prevClipPos.w;
    half2 curPosCS = curClipPos.xy / curClipPos.w;
    
    half2 velocity = (prevPosCS - curPosCS);
#if UNITY_UV_STARTS_AT_TOP
        velocity.y = -velocity.y;
#endif
    
    return velocity;
}
#endif