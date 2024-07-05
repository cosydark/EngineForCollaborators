// Author: QP4B

#ifndef XRENDER_RES_CUSTOM_DATA_HLSL_INCLUDED
#define XRENDER_RES_CUSTOM_DATA_HLSL_INCLUDED

struct STangentSpaceNormal
{
    float3 NormalTS;
};
struct SBase
{
    float3 Color;
    float Metallic;
    float Roughness;
    float Opacity;
};
struct SDetail
{
    float Height;
};
struct SAO
{
    float AmbientOcclusion;
};
struct SSpecular
{
    float Reflectance;
};

struct FPixelInput
{
    float2 UV0;
    float2 UV1;
    float4 VertexColor;
    float3 GNormalWS;
    float3 GNormalWSBump;
};

struct FSlabParams_MInput
{
    SBase Base;
    STangentSpaceNormal TangentSpaceNormal;
    SDetail Detail;
    SAO AO;
    SSpecular Specular;
};
#define MInputType FSlabParams_MInput
#define Texture2D UnityTexture2D

#endif