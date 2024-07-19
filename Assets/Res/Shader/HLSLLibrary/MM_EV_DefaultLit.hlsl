// #include <UnityShaderVariables.cginc>
#ifndef MM_EV_LAYERED_ROCK
#define MM_EV_LAYERED_ROCK

#include "LayeredSurface.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

struct DefaultLitProperties
{
	// Material Model Options
	float Model;
	// Base Layer
	Texture2D BaseMap;
	float4 BaseMap_ST;
	float4 BaseColor;
	Texture2D NormalMap;
	float NormalScale;
	Texture2D MaskMap;
	float Metallic;
	float Occlusion;
	float Height;
	float Roughness;
	Texture2D EmissiveMap;
	float4 EmissiveColor;
	float Luminance;
	// Detail
	Texture2D Detail_BaseMap;
	Texture2D DetailNormalMap;
	float DetailNormalScale;
	Texture2D Detail_MaskMap;
	float Detail_AmbientOcclusion;
	float Detail_AlbedoGrayValue;
	float DetailNormalTiling;
	// UV Decal
	Texture2D UVDecal_BaseMap;
	float4 UVDecal_BaseColor;
	float UVDecal_OverrideNormal;
	Texture2D UVDecal_NormalMap;
	float UVDecal_NormalScale;
	Texture2D UVDecal_MaskMap;
	Texture2D UVDecal_EmissiveMap;
	float4 UVDecal_EmissiveColor;
	float UVDecal_Luminance;
	float UVDecal_Metallic;
	float UVDecal_Occlusion;
	float UVDecal_Height;
	float UVDecal_Roughness;
};

void PrepareMaterialInput_New(FPixelInput PixelIn, DefaultLitProperties Properties, inout MInputType MInput)
{
    float2 BaseMapUV = PixelIn.UV0 * Properties.BaseMap_ST.xy + Properties.BaseMap_ST.zw;
    float4 BaseMap = SAMPLE_TEXTURE2D(Properties.BaseMap, SamplerTriLinearRepeat, BaseMapUV);
    float4 MaskMap = SAMPLE_TEXTURE2D(Properties.MaskMap, SamplerLinearRepeat, BaseMapUV);
    
    float4 BaseColor = BaseMap * Properties.BaseColor;
    MInput.Base.Color = saturate(BaseColor.rgb);
    MInput.Base.Opacity = BaseColor.a;
    MInput.Base.Metallic = GetMaterialMetallicFromMaskMap(MaskMap) * Properties.Metallic;
    MInput.Base.Roughness = GetPerceptualRoughnessFromMaskMap(MaskMap) * Properties.Roughness;
    MInput.Detail.Height = ModifyHeight(GetHeightFromMaskMap(MaskMap), Properties.Height, 0);
    MInput.AO.AmbientOcclusion = LerpWhiteTo(GetMaterialAOFromMaskMap(MaskMap), Properties.Occlusion);
    
    float4 NormalMap = SAMPLE_TEXTURE2D(Properties.NormalMap, SamplerLinearRepeat, BaseMapUV);
	NormalMap.g = 1 - NormalMap.g;
    MInput.TangentSpaceNormal.NormalTS = GetNormalTSFromNormalTex(NormalMap, Properties.NormalScale);
    
    MInput.Emission.Color = SAMPLE_TEXTURE2D(Properties.EmissiveMap, SamplerLinearRepeat, BaseMapUV).rgb * Properties.EmissiveColor.rgb;
    MInput.Emission.Luminance = Properties.Luminance;
    
    // 2U Decal
	if(abs(Properties.Model - 1) < FLT_EPS)
	{
		float2 Decal_UV = saturate(PixelIn.UV1);
		float4 Decal_BaseMap = SAMPLE_TEXTURE2D(Properties.UVDecal_BaseMap, SamplerTriLinearRepeat, Decal_UV);
		float4 Decal_EmissiveMap = SAMPLE_TEXTURE2D(Properties.UVDecal_EmissiveMap, SamplerTriLinearRepeat, Decal_UV);
		float4 Decal_MaskMap = SAMPLE_TEXTURE2D(Properties.UVDecal_MaskMap, SamplerLinearRepeat, Decal_UV);
		float4 Decal_NormalMap = SAMPLE_TEXTURE2D(Properties.UVDecal_NormalMap, SamplerLinearRepeat, Decal_UV);
		float3 Decal_Normal = GetNormalTSFromNormalTex(Decal_NormalMap, Properties.UVDecal_NormalScale);
		float4 Decal_BaseColor = Decal_BaseMap * Properties.UVDecal_BaseColor;
		float4 Decal_EmissiveColor = Decal_EmissiveMap * Properties.UVDecal_EmissiveColor;
		float Decal_Alpha = Decal_BaseColor.a;
		MInput.Base.Color = lerp(MInput.Base.Color, Decal_BaseColor.rgb, Decal_Alpha);
		MInput.Emission.Color = lerp(MInput.Emission.Color, Decal_EmissiveColor.rgb, Decal_Alpha);
		MInput.Emission.Luminance = lerp(MInput.Emission.Luminance, Properties.UVDecal_Luminance, Decal_Alpha);
		MInput.Base.Metallic = lerp(MInput.Base.Metallic, GetMaterialMetallicFromMaskMap(Decal_MaskMap) * Properties.UVDecal_Metallic, Decal_Alpha);
		MInput.Base.Roughness = lerp(MInput.Base.Roughness, GetPerceptualRoughnessFromMaskMap(Decal_MaskMap) * Properties.UVDecal_Roughness, Decal_Alpha);
		MInput.AO.AmbientOcclusion = lerp(MInput.AO.AmbientOcclusion, LerpWhiteTo(GetMaterialAOFromMaskMap(Decal_MaskMap), Properties.UVDecal_Occlusion), Decal_Alpha);
		MInput.Detail.Height = lerp(MInput.Detail.Height, ModifyHeight(GetHeightFromMaskMap(Decal_MaskMap), Properties.UVDecal_Height, 1), Decal_Alpha);
		float3 BlendNormal = lerp(  BlendAngelCorrectedNormals(MInput.TangentSpaceNormal.NormalTS, Decal_Normal),
									lerp(MInput.TangentSpaceNormal.NormalTS, Decal_Normal, Decal_Alpha),
									Properties.UVDecal_OverrideNormal);
		MInput.TangentSpaceNormal.NormalTS = lerp(MInput.TangentSpaceNormal.NormalTS, BlendNormal, Decal_Alpha);
	}
	// Detail
	if(abs(Properties.Model - 2) < FLT_EPS)
	{
		float2 DetailUV = PixelIn.UV0 * Properties.DetailNormalTiling;
		float BaseMapAlbedoGrayValue = ComputeColorLuminance(SAMPLE_TEXTURE2D(Properties.Detail_BaseMap, SamplerTriLinearRepeat, DetailUV));
		float4 NormalMapBlend = SAMPLE_TEXTURE2D(Properties.DetailNormalMap, SamplerLinearRepeat, DetailUV);
		float3 NormalBlend = GetNormalTSFromNormalTex(NormalMapBlend, Properties.DetailNormalScale);
		float4 MaskMapBlend = SAMPLE_TEXTURE2D(Properties.Detail_MaskMap, SamplerLinearRepeat, DetailUV);
		MInput.Base.Color *= lerp(1, BaseMapAlbedoGrayValue, Properties.Detail_AlbedoGrayValue);
		MInput.TangentSpaceNormal.NormalTS = BlendAngelCorrectedNormals(MInput.TangentSpaceNormal.NormalTS, NormalBlend);
		MInput.AO.AmbientOcclusion = min(MInput.AO.AmbientOcclusion, lerp(1, GetMaterialAOFromMaskMap(MaskMapBlend), Properties.Detail_AmbientOcclusion));
	}
}

void DefaultLit_float(	// PixelIn And Something
						float2 UV0,
						float2 UV1,
						// Material Model Options
						float Model,
						// Base Layer
						Texture2D BaseMap,
						float4 BaseMap_ST,
						float4 BaseColor,
						Texture2D NormalMap,
						float NormalScale,
						Texture2D MaskMap,
						float Metallic,
						float Occlusion,
						float Height,
						float Roughness,
						Texture2D EmissiveMap,
						float4 EmissiveColor,
						float Luminance,
						// Detail
						Texture2D Detail_BaseMap,
						Texture2D DetailNormalMap,
						float DetailNormalScale,
						Texture2D Detail_MaskMap,
						float Detail_AmbientOcclusion,
						float Detail_AlbedoGrayValue,
						float DetailNormalTiling,
						// UV Decal
						Texture2D UVDecal_BaseMap,
						float4 UVDecal_BaseColor,
						float UVDecal_OverrideNormal,
						Texture2D UVDecal_NormalMap,
						float UVDecal_NormalScale,
						Texture2D UVDecal_MaskMap,
						Texture2D UVDecal_EmissiveMap,
						float4 UVDecal_EmissiveColor,
						float UVDecal_Luminance,
						float UVDecal_Metallic,
						float UVDecal_Occlusion,
						float UVDecal_Height,
						float UVDecal_Roughness,
						// Out Stuff
						out float3 MColor,
						out float3 MNormalTS,
						out float MMetallic,
						out float MAmbientOcclusion,
						out float MHeight,
						out float MRoughness,
						out float3 MEmissiveColor,
						out float MLuminance
					   )
{
	// Fill Properties
	DefaultLitProperties Properties;
	Properties.Model = Model;
	// Base
	Properties.BaseMap = BaseMap;
	Properties.BaseMap_ST = BaseMap_ST;
	Properties.BaseColor = BaseColor;
	Properties.NormalMap = NormalMap;
	Properties.NormalScale = NormalScale;
	Properties.MaskMap = MaskMap;
	Properties.Metallic = Metallic;
	Properties.Occlusion = Occlusion;
	Properties.Height = Height;
	Properties.Roughness = Roughness;
	Properties.EmissiveMap = EmissiveMap;
	Properties.EmissiveColor = EmissiveColor;
	Properties.Luminance = Luminance;
	// Detail
	Properties.Detail_BaseMap = Detail_BaseMap;
	Properties.DetailNormalMap = DetailNormalMap;
	Properties.DetailNormalScale = DetailNormalScale;
	Properties.Detail_MaskMap = Detail_MaskMap;
	Properties.Detail_AmbientOcclusion = Detail_AmbientOcclusion;
	Properties.Detail_AlbedoGrayValue = Detail_AlbedoGrayValue;
	Properties.DetailNormalTiling = DetailNormalTiling;
	// UV Decal
	Properties.UVDecal_BaseMap = UVDecal_BaseMap;
	Properties.UVDecal_BaseColor = UVDecal_BaseColor;
	Properties.UVDecal_OverrideNormal = UVDecal_OverrideNormal;
	Properties.UVDecal_NormalMap = UVDecal_NormalMap;
	Properties.UVDecal_NormalScale = UVDecal_NormalScale;
	Properties.UVDecal_MaskMap = UVDecal_MaskMap;
	Properties.UVDecal_EmissiveMap = UVDecal_EmissiveMap;
	Properties.UVDecal_EmissiveColor = UVDecal_EmissiveColor;
	Properties.UVDecal_Luminance = UVDecal_Luminance;
	Properties.UVDecal_Metallic = UVDecal_Metallic;
	Properties.UVDecal_Occlusion = UVDecal_Occlusion;
	Properties.UVDecal_Height = UVDecal_Height;
	Properties.UVDecal_Roughness = UVDecal_Roughness;
	// Fill Other
	FPixelInput PixelIn;
	PixelIn.UV0 = UV0;
	PixelIn.UV1 = UV1;
	MInputType MInput;
	SetupMInput(MInput);
	PrepareMaterialInput_New(PixelIn, Properties, MInput);
	MColor = MInput.Base.Color;
	MNormalTS = MInput.TangentSpaceNormal.NormalTS;
	MMetallic = MInput.Base.Metallic;
	// MMetallic = 0;// Not A Dielectric
	MAmbientOcclusion = MInput.AO.AmbientOcclusion;
	MHeight = MInput.Detail.Height;
	MRoughness = MInput.Base.Roughness;
	MEmissiveColor = MInput.Emission.Color;
	MLuminance = MInput.Emission.Luminance;
}
#endif