// Author: QP4B

#ifndef XRENDER_RES_LAYERED_SURFACE_HLSL_INCLUDED
#define XRENDER_RES_LAYERED_SURFACE_HLSL_INCLUDED

#include "CommonAntiTilling.hlsl"
#include "HeightLerp.hlsl"
#include "CustomData.hlsl"

struct MaterialLayer
{
    Texture2D BaseMap;
    float4 BaseColor;
    Texture2D NormalMap;
    float NormalScale;
    Texture2D MaskMap;
    float Reflectance;
    float2 HeightScaleAndShift;
    float2 BlendHeightScaleAndShift;
};
struct SimpleMaterialLayer
{
    float4 BaseColor;
    float BlendHeight;
};
struct DetailLayer
{
    Texture2D BaseMap;
    Texture2D NormalMap;
    float NormalScale;
    Texture2D MaskMap;
    float AmbientOcclusion;
    float AlbedoGrayValue;
};
struct CustomMInput
{
    float3 Color;
    float3 NormalTS;
    float Reflectance;
    float Metallic;
    float AmbientOcclusion;
    float HeightForBlending;
    float MaterialHeight;
    float Roughness;
};
float ComputeColorLuminance(float3 LinearRgb)
{
    return dot(LinearRgb, float3(0.2126729, 0.7151522, 0.0721750));
}
float3 BlendAngelCorrectedNormals(float3 BaseNormal, float3 AdditionalNormal)
{
    float3 Temp_0 = float3(BaseNormal.xy, BaseNormal.z + 1);
    float3 Temp_1 = float3(-AdditionalNormal.xy, AdditionalNormal.z);
    float3 Temp_2 = dot(Temp_0, Temp_1);
    return normalize(Temp_0 * Temp_2 - Temp_1 * Temp_2);
}

void BlendWithHeight(MaterialLayer MLayer, float2 Coordinate, float IntensityMask, float BlendRadius, float BlendMode, inout CustomMInput CMInput )
{
    float3 BaseMapBlend = SAMPLE_TEXTURE2D(MLayer.BaseMap, SamplerTriLinearRepeat, Coordinate) * MLayer.BaseColor;
    float4 NormalMapBlend = SAMPLE_TEXTURE2D(MLayer.NormalMap, SamplerLinearRepeat, Coordinate);
    float3 NormalBlend = GetNormalTSFromNormalTex(NormalMapBlend, MLayer.NormalScale);
    float4 MaskMapBlend = SAMPLE_TEXTURE2D(MLayer.MaskMap, SamplerLinearRepeat, Coordinate);

    float ModifiedHeight = saturate(ModifyHeight(MaskMapBlend.z, MLayer.BlendHeightScaleAndShift.x, MLayer.BlendHeightScaleAndShift.y));
    float HeightBlendMask = ComputeHeightBlendMask(ModifiedHeight, CMInput.HeightForBlending, IntensityMask, BlendRadius, BlendMode);
    MaskMapBlend.z = ModifyHeight(MaskMapBlend.z, MLayer.HeightScaleAndShift.x, MLayer.HeightScaleAndShift.y);

    
    CMInput.Color = lerp(CMInput.Color, BaseMapBlend, HeightBlendMask);
    CMInput.NormalTS = lerp(CMInput.NormalTS, NormalBlend, HeightBlendMask);
    CMInput.Reflectance = lerp(CMInput.Reflectance, MLayer.Reflectance, HeightBlendMask);
    CMInput.Metallic = lerp(CMInput.Metallic, GetMaterialMetallicFromMaskMap(MaskMapBlend), HeightBlendMask);
    CMInput.AmbientOcclusion = lerp(CMInput.AmbientOcclusion, GetMaterialAOFromMaskMap(MaskMapBlend), HeightBlendMask);
    CMInput.HeightForBlending = lerp(CMInput.HeightForBlending, ModifiedHeight, HeightBlendMask);
    CMInput.MaterialHeight = lerp(CMInput.MaterialHeight, GetHeightFromMaskMap(MaskMapBlend), HeightBlendMask);
    CMInput.Roughness = lerp(CMInput.Roughness, GetPerceptualRoughnessFromMaskMap(MaskMapBlend), HeightBlendMask);
}
void BlendWithHeight_Hex(MaterialLayer MLayer, float2 Coordinate, float IntensityMask, float BlendRadius, float BlendMode, inout CustomMInput CMInput )
{
    float3 BaseMapBlend = SAMPLE_TEXTURE2D_HEX(MLayer.BaseMap, SamplerTriLinearRepeat, Coordinate) * MLayer.BaseColor;
    float4 NormalMapBlend = SAMPLE_TEXTURE2D_HEX(MLayer.NormalMap, SamplerLinearRepeat, Coordinate);
    float3 NormalBlend = GetNormalTSFromNormalTex(NormalMapBlend, MLayer.NormalScale);
    float4 MaskMapBlend = SAMPLE_TEXTURE2D_HEX(MLayer.MaskMap, SamplerLinearRepeat, Coordinate);
    
    float ModifiedHeight = saturate(ModifyHeight(MaskMapBlend.z, MLayer.BlendHeightScaleAndShift.x, MLayer.BlendHeightScaleAndShift.y));
    float HeightBlendMask = ComputeHeightBlendMask(ModifiedHeight, CMInput.HeightForBlending, IntensityMask, BlendRadius, BlendMode);
    MaskMapBlend.z = ModifyHeight(MaskMapBlend.z, MLayer.HeightScaleAndShift.x, MLayer.HeightScaleAndShift.y);
    
    CMInput.Color = lerp(CMInput.Color, BaseMapBlend, HeightBlendMask);
    CMInput.NormalTS = lerp(CMInput.NormalTS, NormalBlend, HeightBlendMask);
    CMInput.Reflectance = lerp(CMInput.Reflectance, MLayer.Reflectance, HeightBlendMask);
    CMInput.Metallic = lerp(CMInput.Metallic, GetMaterialMetallicFromMaskMap(MaskMapBlend), HeightBlendMask);
    CMInput.AmbientOcclusion = lerp(CMInput.AmbientOcclusion, GetMaterialAOFromMaskMap(MaskMapBlend), HeightBlendMask);
    CMInput.HeightForBlending = lerp(CMInput.HeightForBlending, ModifiedHeight, HeightBlendMask);
    CMInput.MaterialHeight = lerp(CMInput.MaterialHeight, GetHeightFromMaskMap(MaskMapBlend), HeightBlendMask);
    CMInput.Roughness = lerp(CMInput.Roughness, GetPerceptualRoughnessFromMaskMap(MaskMapBlend), HeightBlendMask);
}
void BlendWithHeightNoTexture(SimpleMaterialLayer SMLayer, float IntensityMask, float BlendRadius, float BlendMode, inout CustomMInput CMInput)
{
    float3 BaseColorBlend = SMLayer.BaseColor;
    
    float2 Weights = lerp(float2(IntensityMask, 1), float2(1, IntensityMask), BlendMode);
    float2 BlendResult = HeightBlend(Weights.x, SMLayer.BlendHeight, Weights.y, CMInput.HeightForBlending, BlendRadius);
    float HeightBlendMask = lerp(BlendResult.x, BlendResult.y, BlendMode);
    BaseColorBlend = lerp(1, BaseColorBlend, HeightBlendMask);
    
    CMInput.Color = saturate(CMInput.Color * BaseColorBlend);// Make BaseColor Physical
}
void BlendWithOutHeightNoTexture(SimpleMaterialLayer SMLayer, float IntensityMask, inout CustomMInput CMInput)
{
    float4 BaseColorBlend = SMLayer.BaseColor;

    BaseColorBlend = lerp(1, BaseColorBlend, IntensityMask);
    
    CMInput.Color = saturate(CMInput.Color * BaseColorBlend);// Make BaseColor Physical
}
void BlendDetailLayer(DetailLayer DLayer, float2 Coordinate, inout CustomMInput CMInput)
{
    float BaseMapAlbedoGrayValue = ComputeColorLuminance(SAMPLE_TEXTURE2D(DLayer.BaseMap, SamplerTriLinearRepeat, Coordinate));
    float4 NormalMapBlend = SAMPLE_TEXTURE2D(DLayer.NormalMap, SamplerLinearRepeat, Coordinate);
    float3 NormalBlend = GetNormalTSFromNormalTex(NormalMapBlend, DLayer.NormalScale);
    float4 MaskMapBlend = SAMPLE_TEXTURE2D(DLayer.MaskMap, SamplerLinearRepeat, Coordinate);
    
    CMInput.Color *= lerp(1, BaseMapAlbedoGrayValue, DLayer.AlbedoGrayValue);
    CMInput.NormalTS = BlendAngelCorrectedNormals(CMInput.NormalTS, NormalBlend);
    CMInput.AmbientOcclusion = min(CMInput.AmbientOcclusion, lerp(1, GetMaterialAOFromMaskMap(MaskMapBlend), DLayer.AmbientOcclusion));
}
void InitializeTilingLayer(MaterialLayer MLayer, float2 Coordinate, inout CustomMInput CMInput)
{
    // Modify Height
    float4 Mask = SAMPLE_TEXTURE2D(MLayer.MaskMap, SamplerLinearRepeat, Coordinate);
    
    float ModifiedHeight = saturate(ModifyHeight(Mask.z, MLayer.BlendHeightScaleAndShift.x, MLayer.BlendHeightScaleAndShift.y));
    Mask.z = ModifyHeight(Mask.z, MLayer.HeightScaleAndShift.x, MLayer.HeightScaleAndShift.y);
    
    // Fill
    CMInput.Color = SAMPLE_TEXTURE2D(MLayer.BaseMap, SamplerTriLinearRepeat, Coordinate).rgb * MLayer.BaseColor.rgb;
    CMInput.NormalTS = GetNormalTSFromNormalTex(SAMPLE_TEXTURE2D(MLayer.NormalMap, SamplerLinearRepeat, Coordinate), MLayer.NormalScale);
    CMInput.Metallic = GetMaterialMetallicFromMaskMap(Mask);
    CMInput.AmbientOcclusion = GetMaterialAOFromMaskMap(Mask);
    CMInput.HeightForBlending = ModifiedHeight;
    CMInput.MaterialHeight = GetHeightFromMaskMap(Mask);
    CMInput.Roughness = GetPerceptualRoughnessFromMaskMap(Mask);
    CMInput.Reflectance = MLayer.Reflectance;
}
void InitializeTilingLayer_Hex(MaterialLayer MLayer, float2 Coordinate, inout CustomMInput CMInput)
{
    // Modify Height
    float4 Mask = SAMPLE_TEXTURE2D_HEX(MLayer.MaskMap, SamplerLinearRepeat, Coordinate);
    float ModifiedHeight = saturate(ModifyHeight(Mask.z, MLayer.BlendHeightScaleAndShift.x, MLayer.BlendHeightScaleAndShift.y));
    Mask.z = ModifyHeight(Mask.z, MLayer.HeightScaleAndShift.x, MLayer.HeightScaleAndShift.y);

    // Fill
    CMInput.Color = SAMPLE_TEXTURE2D_HEX(MLayer.BaseMap, SamplerTriLinearRepeat, Coordinate).rgb * MLayer.BaseColor.rgb;
    CMInput.NormalTS = GetNormalTSFromNormalTex(SAMPLE_TEXTURE2D_HEX(MLayer.NormalMap, SamplerLinearRepeat, Coordinate), MLayer.NormalScale);
    CMInput.Metallic = GetMaterialMetallicFromMaskMap(Mask);
    CMInput.AmbientOcclusion = GetMaterialAOFromMaskMap(Mask);
    CMInput.HeightForBlending = ModifiedHeight;
    CMInput.MaterialHeight = GetHeightFromMaskMap(Mask);
    CMInput.Roughness = GetPerceptualRoughnessFromMaskMap(Mask);
    CMInput.Reflectance = MLayer.Reflectance;
}
void SetupMaterialLayer(    Texture2D BaseMap,
                            float4 BaseColor,
                            Texture2D NormalMap,
                            float NormalScale,
                            Texture2D MaskMap,
                            float Reflectance,
                            float2 HeightScaleAndShift,
                            float2 BlendHeightScaleAndShift,
                            inout MaterialLayer MLayer
                       )
{
    MLayer.BaseMap = BaseMap;
    MLayer.BaseColor = BaseColor;
    MLayer.NormalMap = NormalMap;
    MLayer.NormalScale = NormalScale;
    MLayer.MaskMap = MaskMap;
    MLayer.Reflectance = Reflectance;
    MLayer.HeightScaleAndShift = HeightScaleAndShift;
    MLayer.BlendHeightScaleAndShift = BlendHeightScaleAndShift;
}
void SetupSMaterialLayer(   float4 BaseColor,
                            float BlendHeightShift,
                            inout SimpleMaterialLayer SMLayer
                         )
{
    SMLayer.BaseColor = BaseColor;
    SMLayer.BlendHeight = BlendHeightShift;
}
void SetupDetailLayer(  Texture2D BaseMap,
                        Texture2D NormalMap,
                        float NormalScale,
                        Texture2D MaskMap,
                        float AmbientOcclusion,
                        float AlbedoGrayValue,
                        inout DetailLayer SMLayer
                     )
{
    SMLayer.BaseMap = BaseMap;
    SMLayer.NormalMap = NormalMap;
    SMLayer.NormalScale = NormalScale;
    SMLayer.MaskMap = MaskMap;
    SMLayer.AmbientOcclusion = AmbientOcclusion;
    SMLayer.AlbedoGrayValue = AlbedoGrayValue;
}
void SetupMInput(inout MInputType MInput)
{
    MInput.Base.Color = float3(0.5, 0.5, 0.5);
    MInput.Base.Opacity = 1;
    MInput.Base.Metallic = 0.5;
    MInput.Base.Roughness = 0.5;
    MInput.AO.AmbientOcclusion = 0.5;
    MInput.Detail.Height = 0.5;
    MInput.Specular.Reflectance = 0.5;
    MInput.TangentSpaceNormal.NormalTS = float3(0, 0, 1);
    MInput.Emission.Color = float3(0, 0, 0);
    MInput.Emission.Luminance = 0;
}

#endif