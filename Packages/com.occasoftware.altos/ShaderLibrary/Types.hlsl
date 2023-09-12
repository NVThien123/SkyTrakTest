#ifndef ALTOS_TYPES_INCLUDED
#define ALTOS_TYPES_INCLUDED

struct AtmosphereData
{
    half atmosThickness;
    half atmosHeight;
    half cloudFadeDistance;
    half distantCoverageAmount;
    half distantCoverageDepth;
};

struct AtmosHitData
{
    bool didHit;
    bool doubleIntersection;
    half nearDist;
    half nearDist2;
    half farDist;
    half farDist2;
};


struct IntersectData
{
    bool hit;
    bool inside;
    half frontfaceDistance;
    half backfaceDistance;
};

struct StaticMaterialData
{
    half2 uv;
	
    half3 mainCameraOrigin;
    half3 rayOrigin;
    half3 sunPos;
    half3 sunColor;
    half sunIntensity;
	
    bool renderLocal;
	
    half cloudiness;
    half alphaAccumulation;
    half3 extinction;
    half3 highAltExtinction;
    half HG;
	
    half ambientExposure;
    half3 ambientColor;
    half3 fogColor;
    half fogPower;
	
    half multipleScatteringAmpGain;
    half multipleScatteringDensityGain;
    int multipleScatteringOctaves;
	
    Texture3D baseTexture;
    half4 baseTexture_TexelSize;
    half3 baseScale;
    half3 baseTimescale;
	
    Texture2D curlNoise;
    half4 curlNoiseTexelSize;
    half curlScale;
    half curlStrength;
    half curlTimescale;
	
    Texture3D detail1Texture;
    half4 detailTexture_TexelSize;
    half3 detail1Scale;
    half detail1Strength;
    half3 detail1Timescale;
    bool detail1Invert;
    half2 detail1HeightRemap;
	
    Texture2D highAltTex1;
    float4 highAltTex1_TexelSize;
	
    Texture2D highAltTex2;
    float4 highAltTex2_TexelSize;
	
    Texture2D highAltTex3;
    float4 highAltTex3_TexelSize;
	
    half highAltitudeAlphaAccumulation;
    half2 highAltOffset1;
    half2 highAltOffset2;
    half2 highAltOffset3;
    half2 highAltScale1;
    half2 highAltScale2;
    half2 highAltScale3;
    half highAltitudeCloudiness;
    half highAltInfluence1;
    half highAltInfluence2;
    half highAltInfluence3;
	
    int lightingDistance;
    int planetRadius;
	
    half heightDensityInfluence;
    half cloudinessDensityInfluence;
	
    Texture2D weathermapTex;
};

struct RayData
{
    float3 rayOrigin;
    float3 rayPosition;
    float3 rayDirection;
    float3 rayDirectionUnjittered;
    half relativeDepth;
    half rayDepth;
    half stepSize;
    half shortStepSize;
    half noiseAdjustment;
    half noiseIntensity;
};

#endif
