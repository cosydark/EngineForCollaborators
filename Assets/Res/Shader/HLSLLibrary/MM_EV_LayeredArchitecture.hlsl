// #include <UnityShaderVariables.cginc>
#ifndef MM_EV_LAYERED_ARCHITECTURE
#define MM_EV_LAYERED_ARCHITECTURE

#include "LayeredSurface.hlsl"
//
struct LayeredArchitectureProperties
{
	// Material Model Options
	float LayerCount;
	float UseVertexColor;
	float UseBaseLayer;
	// Material Setting
	Texture2D BlendMask;
	// Base Layer
	Texture2D BaseLayer_BaseMap;
	Texture2D BaseLayer_NormalMap;
	float BaseLayer_NormalScale;
	Texture2D BaseLayer_MaskMap;
	float BaseLayer_AmbientOcclusion;
	float BaseLayer_AlbedoGrayValue;
	// Tiling Layer
	Texture2D TilingLayer_BaseMap;
	float4 TilingLayer_BaseColor;
	Texture2D TilingLayer_NormalMap;
	float TilingLayer_NormalScale;
	Texture2D TilingLayer_MaskMap;
	float TilingLayer_Reflectance;
	float TilingLayer_HeightScale;
	float TilingLayer_HeightOffset;
	float4 TilingLayer_PorosityFactor;
	float TilingLayer_Tiling;
	float TilingLayer_Use2U;
	float TilingLayer_HexTiling;
	float TilingLayer_MatchScaling;
	float TilingLayer_BlendHeightScale;
	float TilingLayer_BlendHeightShift;
	// Tiling Layer R
	Texture2D TilingLayer_R_BaseMap;
	float4 TilingLayer_R_BaseColor;
	Texture2D TilingLayer_R_NormalMap;
	float TilingLayer_R_NormalScale;
	Texture2D TilingLayer_R_MaskMap;
	float TilingLayer_R_Reflectance;
	float TilingLayer_R_HeightScale;
	float TilingLayer_R_HeightOffset;
	float4 TilingLayer_R_PorosityFactor;
	float TilingLayer_R_Tiling;
	float TilingLayer_R_Use2U;
	float TilingLayer_R_HexTiling;
	float TilingLayer_R_MatchScaling;
	float4 TilingLayer_R_MaskContrastAndIntensity;
	float TilingLayer_R_BlendMode;
	float TilingLayer_R_BlendHeightScale;
	float TilingLayer_R_BlendHeightShift;
	float TilingLayer_R_BlendRadius;
	// Tiling Layer G
	Texture2D TilingLayer_G_BaseMap;
	float4 TilingLayer_G_BaseColor;
	Texture2D TilingLayer_G_NormalMap;
	float TilingLayer_G_NormalScale;
	Texture2D TilingLayer_G_MaskMap;
	float TilingLayer_G_Reflectance;
	float TilingLayer_G_HeightScale;
	float TilingLayer_G_HeightOffset;
	float4 TilingLayer_G_PorosityFactor;
	float TilingLayer_G_Tiling;
	float TilingLayer_G_Use2U;
	float TilingLayer_G_HexTiling;
	float TilingLayer_G_MatchScaling;
	float4 TilingLayer_G_MaskContrastAndIntensity;
	float TilingLayer_G_BlendMode;
	float TilingLayer_G_BlendHeightScale;
	float TilingLayer_G_BlendHeightShift;
	float TilingLayer_G_BlendRadius;
};
//
void PrepareMaterialInput_New(FPixelInput PixelIn, LayeredArchitectureProperties Properties, inout MInputType MInput)
{
	// Prepare UV
	float2 MaskCoordinate = PixelIn.UV0;
	// Mask For Bend
	float4 BlendMask = 0;
	if(Properties.UseVertexColor > FLT_EPS)
	{
		BlendMask = PixelIn.VertexColor;
	}
	else
	{
		BlendMask = SAMPLE_TEXTURE2D(Properties.BlendMask, SamplerTriLinearRepeat, MaskCoordinate);
	}
	
	// Setup MInput
	SetupMInput(MInput);
	// Tiling Layer
	float2 TilingLayer_Coordinate = lerp(PixelIn.UV0, PixelIn.UV1, Properties.TilingLayer_Use2U) * Properties.TilingLayer_Tiling;
	CustomMInput CMInput;
	MaterialLayer MLayer;
	SetupMaterialLayer(	Properties.TilingLayer_BaseMap,
						Properties.TilingLayer_BaseColor,
						Properties.TilingLayer_NormalMap,
						Properties.TilingLayer_NormalScale,
						Properties.TilingLayer_MaskMap,
						Properties.TilingLayer_Reflectance,
						float2(Properties.TilingLayer_HeightScale, Properties.TilingLayer_HeightOffset),
						float2(Properties.TilingLayer_BlendHeightScale,
						Properties.TilingLayer_BlendHeightShift),MLayer
						);
	if(Properties.TilingLayer_HexTiling > FLT_EPS)
	{
		InitializeTilingLayer_Hex(MLayer, TilingLayer_Coordinate, CMInput);
	}
	else
	{
		InitializeTilingLayer(MLayer, TilingLayer_Coordinate, CMInput);
	}
	// Tiling Layer R
if(Properties.LayerCount > 0)
{
	BlendMask.r = saturate(pow(BlendMask.r, Properties.TilingLayer_R_MaskContrastAndIntensity.x) * Properties.TilingLayer_R_MaskContrastAndIntensity.y);
	float2 TilingLayer_R_Coordinate = lerp(PixelIn.UV0, PixelIn.UV1, Properties.TilingLayer_R_Use2U) * Properties.TilingLayer_R_Tiling;
	MaterialLayer MLayer_R;
	SetupMaterialLayer(	Properties.TilingLayer_R_BaseMap,
						Properties.TilingLayer_R_BaseColor,
						Properties.TilingLayer_R_NormalMap,
						Properties.TilingLayer_R_NormalScale,
						Properties.TilingLayer_R_MaskMap,
						Properties.TilingLayer_R_Reflectance,
						float2(Properties.TilingLayer_R_HeightScale, Properties.TilingLayer_R_HeightOffset),
						float2(Properties.TilingLayer_R_BlendHeightScale, Properties.TilingLayer_R_BlendHeightShift),
						MLayer_R
						);
	if(Properties.TilingLayer_R_HexTiling > FLT_EPS)
	{
		BlendWithHeight_Hex(MLayer_R, TilingLayer_R_Coordinate, BlendMask.r, Properties.TilingLayer_R_BlendRadius, Properties.TilingLayer_R_BlendMode, CMInput);
	}
	else
	{
		BlendWithHeight(MLayer_R, TilingLayer_R_Coordinate, BlendMask.r, Properties.TilingLayer_R_BlendRadius, Properties.TilingLayer_R_BlendMode, CMInput);
	}
}
if(Properties.LayerCount > 1)
{
	// Tiling Layer G
	BlendMask.g = saturate(pow(BlendMask.g, Properties.TilingLayer_G_MaskContrastAndIntensity.x) * Properties.TilingLayer_G_MaskContrastAndIntensity.y);
	float2 TilingLayer_G_Coordinate = lerp(PixelIn.UV0, PixelIn.UV1, Properties.TilingLayer_G_Use2U) * Properties.TilingLayer_G_Tiling;
	MaterialLayer MLayer_G;
	SetupMaterialLayer(	Properties.TilingLayer_G_BaseMap,
						Properties.TilingLayer_G_BaseColor,
						Properties.TilingLayer_G_NormalMap,
						Properties.TilingLayer_G_NormalScale,
						Properties.TilingLayer_G_MaskMap,
						Properties.TilingLayer_G_Reflectance,
						float2(Properties.TilingLayer_G_HeightScale, Properties.TilingLayer_G_HeightOffset),
						float2(Properties.TilingLayer_G_BlendHeightScale, Properties.TilingLayer_G_BlendHeightShift),
						MLayer_G
						);
	if(Properties.TilingLayer_G_HexTiling > FLT_EPS)
	{
		BlendWithHeight_Hex(MLayer_G, TilingLayer_G_Coordinate, BlendMask.g, Properties.TilingLayer_G_BlendRadius, Properties.TilingLayer_G_BlendMode, CMInput);
	}
	else
	{
		BlendWithHeight(MLayer_G, TilingLayer_G_Coordinate, BlendMask.g, Properties.TilingLayer_G_BlendRadius, Properties.TilingLayer_G_BlendMode, CMInput);
	}
}
	// CMInput -> MInput
	MInput.Base.Color = CMInput.Color;
	MInput.TangentSpaceNormal.NormalTS = CMInput.NormalTS;
	MInput.Specular.Reflectance = CMInput.Reflectance;
	MInput.Base.Metallic = CMInput.Metallic;
	MInput.AO.AmbientOcclusion = CMInput.AmbientOcclusion;
	MInput.Detail.Height = CMInput.MaterialHeight;
	MInput.Base.Roughness = CMInput.Roughness;
	// Base Layer
if(Properties.UseBaseLayer > FLT_EPS)
{
	float2 BaseCoordinate = PixelIn.UV0;
	float4 BaseMap = SAMPLE_TEXTURE2D(Properties.BaseLayer_BaseMap, SamplerLinearRepeat, BaseCoordinate);
	float4 MaskMap = SAMPLE_TEXTURE2D(Properties.BaseLayer_MaskMap, SamplerLinearRepeat, BaseCoordinate);
	float4 NormalMap = SAMPLE_TEXTURE2D(Properties.BaseLayer_NormalMap, SamplerLinearRepeat, BaseCoordinate);
	float3 NormalTS = GetNormalTSFromNormalTex(NormalMap, Properties.BaseLayer_NormalScale);
	float BaseMapLuminance = ComputeColorLuminance(BaseMap.rgb);
	MInput.Base.Color *= lerp(1, BaseMapLuminance, Properties.BaseLayer_AlbedoGrayValue);
	MInput.TangentSpaceNormal.NormalTS = BlendAngelCorrectedNormals(NormalTS, MInput.TangentSpaceNormal.NormalTS);
	MInput.AO.AmbientOcclusion *= lerp(1, GetMaterialAOFromMaskMap(MaskMap), Properties.BaseLayer_AmbientOcclusion);
}
}

void LayeredArchitecture_float( // PixelIn And Something
								float2 UV0,
								float2 UV1,
								float4 VertexColor,
								// Material Model Options
								float LayerCount,
								float UseVertexColor,
								float UseBaseLayer,
								// Material Setting
								Texture2D BlendMask,
								// Base Layer
								Texture2D BaseLayer_BaseMap,
								Texture2D BaseLayer_NormalMap,
								float BaseLayer_NormalScale,
								Texture2D BaseLayer_MaskMap,
								float BaseLayer_AmbientOcclusion,
								float BaseLayer_AlbedoGrayValue,
								// Tiling Layer
								Texture2D TilingLayer_BaseMap,
								float4 TilingLayer_BaseColor,
								Texture2D TilingLayer_NormalMap,
								float TilingLayer_NormalScale,
								Texture2D TilingLayer_MaskMap,
								float TilingLayer_Reflectance,
								float TilingLayer_HeightScale,
								float TilingLayer_HeightOffset,
								float4 TilingLayer_PorosityFactor,
								float TilingLayer_Tiling,
								float TilingLayer_Use2U,
								float TilingLayer_HexTiling,
								float TilingLayer_MatchScaling,
								float TilingLayer_BlendHeightScale,
								float TilingLayer_BlendHeightShift,
								// Tiling Layer R
								Texture2D TilingLayer_R_BaseMap,
								float4 TilingLayer_R_BaseColor,
								Texture2D TilingLayer_R_NormalMap,
								float TilingLayer_R_NormalScale,
								Texture2D TilingLayer_R_MaskMap,
								float TilingLayer_R_Reflectance,
								float TilingLayer_R_HeightScale,
								float TilingLayer_R_HeightOffset,
								float4 TilingLayer_R_PorosityFactor,
								float TilingLayer_R_Tiling,
								float TilingLayer_R_Use2U,
								float TilingLayer_R_HexTiling,
								float TilingLayer_R_MatchScaling,
								float4 TilingLayer_R_MaskContrastAndIntensity,
								float TilingLayer_R_BlendMode,
								float TilingLayer_R_BlendHeightScale,
								float TilingLayer_R_BlendHeightShift,
								float TilingLayer_R_BlendRadius,
								// Tiling Layer G
								Texture2D TilingLayer_G_BaseMap,
								float4 TilingLayer_G_BaseColor,
								Texture2D TilingLayer_G_NormalMap,
								float TilingLayer_G_NormalScale,
								Texture2D TilingLayer_G_MaskMap,
								float TilingLayer_G_Reflectance,
								float TilingLayer_G_HeightScale,
								float TilingLayer_G_HeightOffset,
								float4 TilingLayer_G_PorosityFactor,
								float TilingLayer_G_Tiling,
								float TilingLayer_G_Use2U,
								float TilingLayer_G_HexTiling,
								float TilingLayer_G_MatchScaling,
								float4 TilingLayer_G_MaskContrastAndIntensity,
								float TilingLayer_G_BlendMode,
								float TilingLayer_G_BlendHeightScale,
								float TilingLayer_G_BlendHeightShift,
								float TilingLayer_G_BlendRadius,
								// Out Stuff
								out float3 MColor,
								out float3 MNormalTS,
								out float MMetallic,
								out float MAmbientOcclusion,
								out float MHeight,
								out float MRoughness
							   )
{
	// Fill Properties
	LayeredArchitectureProperties Properties;
	Properties.LayerCount = LayerCount;
	Properties.UseVertexColor = UseVertexColor;
	Properties.UseVertexColor = UseVertexColor;
	Properties.UseBaseLayer = UseBaseLayer;
	Properties.BlendMask = BlendMask;
	Properties.BaseLayer_BaseMap = BaseLayer_BaseMap;
	Properties.BaseLayer_NormalMap = BaseLayer_NormalMap;
	Properties.BaseLayer_NormalScale = BaseLayer_NormalScale;
	Properties.BaseLayer_MaskMap = BaseLayer_MaskMap;
	Properties.BaseLayer_AmbientOcclusion = BaseLayer_AmbientOcclusion;
	Properties.BaseLayer_AlbedoGrayValue = BaseLayer_AlbedoGrayValue;
	// Next
	Properties.TilingLayer_BaseMap = TilingLayer_BaseMap;
	Properties.TilingLayer_BaseColor = TilingLayer_BaseColor;
	Properties.TilingLayer_NormalMap = TilingLayer_NormalMap;
	Properties.TilingLayer_NormalScale = TilingLayer_NormalScale;
	Properties.TilingLayer_MaskMap = TilingLayer_MaskMap;
	Properties.TilingLayer_Reflectance = TilingLayer_Reflectance;
	Properties.TilingLayer_HeightScale = TilingLayer_HeightScale;
	Properties.TilingLayer_HeightOffset = TilingLayer_HeightOffset;
	Properties.TilingLayer_PorosityFactor = TilingLayer_PorosityFactor;
	Properties.TilingLayer_Tiling = TilingLayer_Tiling;
	Properties.TilingLayer_Use2U = TilingLayer_Use2U;
	Properties.TilingLayer_HexTiling = TilingLayer_HexTiling;
	Properties.TilingLayer_MatchScaling = TilingLayer_MatchScaling;
	Properties.TilingLayer_BlendHeightScale = TilingLayer_BlendHeightScale;
	Properties.TilingLayer_BlendHeightShift = TilingLayer_BlendHeightShift;
	// Next
	Properties.TilingLayer_R_BaseMap = TilingLayer_R_BaseMap;
	Properties.TilingLayer_R_BaseColor = TilingLayer_R_BaseColor;
	Properties.TilingLayer_R_NormalMap = TilingLayer_R_NormalMap;
	Properties.TilingLayer_R_NormalScale = TilingLayer_R_NormalScale;
	Properties.TilingLayer_R_MaskMap = TilingLayer_R_MaskMap;
	Properties.TilingLayer_R_Reflectance = TilingLayer_R_Reflectance;
	Properties.TilingLayer_R_HeightScale = TilingLayer_R_HeightScale;
	Properties.TilingLayer_R_HeightOffset = TilingLayer_R_HeightOffset;
	Properties.TilingLayer_R_PorosityFactor = TilingLayer_R_PorosityFactor;
	Properties.TilingLayer_R_Tiling = TilingLayer_R_Tiling;
	Properties.TilingLayer_R_Use2U = TilingLayer_R_Use2U;
	Properties.TilingLayer_R_HexTiling = TilingLayer_R_HexTiling;
	Properties.TilingLayer_R_MatchScaling = TilingLayer_R_MatchScaling;
	Properties.TilingLayer_R_MaskContrastAndIntensity = TilingLayer_R_MaskContrastAndIntensity;
	Properties.TilingLayer_R_BlendMode = TilingLayer_R_BlendMode;
	Properties.TilingLayer_R_BlendHeightScale = TilingLayer_R_BlendHeightScale;
	Properties.TilingLayer_R_BlendHeightShift = TilingLayer_R_BlendHeightShift;
	Properties.TilingLayer_R_BlendRadius = TilingLayer_R_BlendRadius;
	// Next
	Properties.TilingLayer_G_BaseMap = TilingLayer_G_BaseMap;
	Properties.TilingLayer_G_BaseColor = TilingLayer_G_BaseColor;
	Properties.TilingLayer_G_NormalMap = TilingLayer_G_NormalMap;
	Properties.TilingLayer_G_NormalScale = TilingLayer_G_NormalScale;
	Properties.TilingLayer_G_MaskMap = TilingLayer_G_MaskMap;
	Properties.TilingLayer_G_Reflectance = TilingLayer_G_Reflectance;
	Properties.TilingLayer_G_HeightScale = TilingLayer_G_HeightScale;
	Properties.TilingLayer_G_HeightOffset = TilingLayer_G_HeightOffset;
	Properties.TilingLayer_G_PorosityFactor = TilingLayer_G_PorosityFactor;
	Properties.TilingLayer_G_Tiling = TilingLayer_G_Tiling;
	Properties.TilingLayer_G_Use2U = TilingLayer_G_Use2U;
	Properties.TilingLayer_G_HexTiling = TilingLayer_G_HexTiling;
	Properties.TilingLayer_G_MatchScaling = TilingLayer_G_MatchScaling;
	Properties.TilingLayer_G_MaskContrastAndIntensity = TilingLayer_G_MaskContrastAndIntensity;
	Properties.TilingLayer_G_BlendMode = TilingLayer_G_BlendMode;
	Properties.TilingLayer_G_BlendHeightScale = TilingLayer_G_BlendHeightScale;
	Properties.TilingLayer_G_BlendHeightShift = TilingLayer_G_BlendHeightShift;
	Properties.TilingLayer_G_BlendRadius = TilingLayer_G_BlendRadius;
	// Fill Other
	FPixelInput PixelIn;
	PixelIn.UV0 = UV0;
	PixelIn.UV1 = UV1;
	PixelIn.VertexColor = VertexColor;
	MInputType MInput;
	SetupMInput(MInput);
	PrepareMaterialInput_New(PixelIn, Properties, MInput);
	MColor = MInput.Base.Color;
	MNormalTS = MInput.TangentSpaceNormal.NormalTS;
	MMetallic = MInput.Base.Metallic;
	MAmbientOcclusion = MInput.AO.AmbientOcclusion;
	MHeight = MInput.Detail.Height;
	MRoughness = MInput.Base.Roughness;
}
#endif