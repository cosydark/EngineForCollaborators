// Author: zhangzhi

#ifndef XRENDER_RES_LAYERED_DECAL_HLSL_INCLUDED
#define XRENDER_RES_LAYERED_DECAL_HLSL_INCLUDED

#include "LayeredSurface.hlsl"


// SRP
float LerpWhiteToXR(float b, float t)
{
    float oneMinusT = 1.0 - t;
    return oneMinusT + b * t;
}

struct DecalMaterialLayer
{
    float4 BaseColor;
    float3 NormalTS;
    float4 Mask;
    float Reflectance;
    float Blend;
};

struct DecalLayer
{
    Texture2D BaseMap;
    float4 BaseColor;
    Texture2D NormalMap;
    float NormalScale;
    Texture2D MaskMap;
    float Metallic;
    float AmbientOcclusion;
    float Roughness; 
    float Reflectance; 
    float DecalBlend; 
    // float Height; 
};

struct DecalToggle
{
    int  BaseColorToggle;
    int  OpacityToggle;
    int  NormalToggle;
    int  MetalToggle;
    int  AmbientOcclusionToggle;
    int  RoughnessToggle;
    int  Reflectance;
    // int  HeightToggle;
};

void SetupDecalMaterialLayer(   float4 BaseColor,
                                float3 NormalTS,
                                float4 Mask,
                                float Reflectance,
                                float Blend,
                                inout DecalMaterialLayer DMLayer
                            )
{
    DMLayer.BaseColor = BaseColor;
    DMLayer.NormalTS = NormalTS;
    DMLayer.Mask = Mask;
    DMLayer.Reflectance = Reflectance;
    DMLayer.Blend = Blend;
}

void WithDecal(DecalLayer SMLayer, DecalToggle SMToggle, float2 Coordinate, inout MInputType MInput)
{
    float4 BaseMap = SAMPLE_TEXTURE2D(SMLayer.BaseMap, SamplerTriLinearRepeat, Coordinate);
    float4 MaskMap = SAMPLE_TEXTURE2D(SMLayer.MaskMap, SamplerLinearRepeat, Coordinate);
    float  DecalBlend = BaseMap.a * SMLayer.DecalBlend;
    float4 BaseColor = BaseMap * SMLayer.BaseColor;
    float4 mask = float4(0,1,0.5,1);
    mask.r = GetMaterialMetallicFromMaskMap(MaskMap) * SMLayer.Metallic;
    mask.g = LerpWhiteToXR(GetMaterialAOFromMaskMap(MaskMap), SMLayer.AmbientOcclusion);

    mask.a = GetPerceptualRoughnessFromMaskMap(MaskMap) * SMLayer.Roughness;
    float4 NormalMap = SAMPLE_TEXTURE2D(SMLayer.NormalMap, SamplerLinearRepeat, Coordinate);
    float3 NormalTS = GetNormalTSFromNormalTex(NormalMap, SMLayer.NormalScale);

    DecalMaterialLayer DMLayer;
    SetupDecalMaterialLayer(BaseColor,NormalTS,mask,SMLayer.Reflectance,DecalBlend,DMLayer);

    MInput.Base.Color = lerp(MInput.Base.Color,DMLayer.BaseColor.rgb,DMLayer.Blend*SMToggle.BaseColorToggle);
    MInput.Base.Opacity = lerp(MInput.Base.Opacity,DMLayer.BaseColor.a,DMLayer.Blend*SMToggle.OpacityToggle);
    MInput.Base.Metallic = lerp(MInput.Base.Metallic,DMLayer.Mask.r,DMLayer.Blend*SMToggle.MetalToggle);
    MInput.Base.Roughness = lerp(MInput.Base.Roughness,DMLayer.Mask.a,DMLayer.Blend*SMToggle.RoughnessToggle);

    MInput.AO.AmbientOcclusion = lerp(MInput.AO.AmbientOcclusion,DMLayer.Mask.g,DMLayer.Blend*SMToggle.AmbientOcclusionToggle);
    MInput.TangentSpaceNormal.NormalTS = lerp(MInput.TangentSpaceNormal.NormalTS,DMLayer.NormalTS,DMLayer.Blend*SMToggle.NormalToggle);
    MInput.Specular.Reflectance = lerp(MInput.Specular.Reflectance,DMLayer.Reflectance,DMLayer.Blend*SMToggle.Reflectance);
}

void SetupDecalLayer(   Texture2D BaseMap,
                        float4 BaseColor,
                        Texture2D NormalMap,
                        float NormalScale,
                        Texture2D MaskMap,
                        float Metallic,
                        float AmbientOcclusion,
                        float Roughness,
                        float Reflectance,
                        float DecalBlend,
                        inout DecalLayer SMLayer
                    )
{
    SMLayer.BaseMap = BaseMap;
    SMLayer.BaseColor = BaseColor;
    SMLayer.NormalMap = NormalMap;
    SMLayer.NormalScale = NormalScale;
    SMLayer.MaskMap = MaskMap;
    SMLayer.Metallic = Metallic;
    SMLayer.AmbientOcclusion = AmbientOcclusion;
    SMLayer.Roughness = Roughness;
    SMLayer.DecalBlend = DecalBlend;
    SMLayer.Reflectance = Reflectance;
}

void SetupDecalToggle(  int  BaseColorToggle,
                        int  OpacityToggle,
                        int  NormalToggle,
                        int  MetalToggle,
                        int  AmbientOcclusionToggle,
                        int  RoughnessToggle,
                        int  Reflectance,
                        inout DecalToggle SMToggle
                    )
{
    SMToggle.BaseColorToggle = BaseColorToggle;
    SMToggle.OpacityToggle = OpacityToggle;
    SMToggle.NormalToggle = NormalToggle;
    SMToggle.MetalToggle = MetalToggle;
    SMToggle.AmbientOcclusionToggle = AmbientOcclusionToggle;
    SMToggle.RoughnessToggle = RoughnessToggle;
    SMToggle.Reflectance = Reflectance;
}
#endif