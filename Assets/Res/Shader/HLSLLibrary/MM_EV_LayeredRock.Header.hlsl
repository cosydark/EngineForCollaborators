
#ifndef MM_EV_LAYEREDARCHITECTURE_HEADER_HLSL
#define MM_EV_LAYEREDARCHITECTURE_HEADER_HLSL

#define MM_NAME EV_LAYEREDARCHITECTURE
//
// TEXTURE2D(_BlendMask);
// TEXTURE2D(_BaseLayer_BaseMap);
// TEXTURE2D(_BaseLayer_NormalMap);
// TEXTURE2D(_BaseLayer_MaskMap);
// TEXTURE2D(_TilingLayer_BaseMap);
// TEXTURE2D(_TilingLayer_NormalMap);
// TEXTURE2D(_TilingLayer_MaskMap);
// TEXTURE2D(_TilingLayer_R_BaseMap);
// TEXTURE2D(_TilingLayer_R_NormalMap);
// TEXTURE2D(_TilingLayer_R_MaskMap);
// TEXTURE2D(_TilingLayer_G_BaseMap);
// TEXTURE2D(_TilingLayer_G_NormalMap);
// TEXTURE2D(_TilingLayer_G_MaskMap);
//
// //--------------------------------------------------------------------
//
// CBUFFER_START(UnityPerMaterial)
// float _BaseLayer_NormalScale;
// float4 _TilingLayer_BaseColor;
// float _TilingLayer_NormalScale;
// float _TilingLayer_Reflectance;
// float _TilingLayer_HeightOffset;
// int _TilingLayer_Porosity;
// float _TilingLayer_Tiling;
// int _TilingLayer_Use2U;
// int _TilingLayer_HexTiling;
// int _TilingLayer_MatchScaling;
// float4 _TilingLayer_R_BaseColor;
// float _TilingLayer_R_NormalScale;
// float _TilingLayer_R_Reflectance;
// float _TilingLayer_R_HeightOffset;
// int _TilingLayer_R_Porosity;
// float _TilingLayer_R_Tiling;
// int _TilingLayer_R_Use2U;
// int _TilingLayer_R_HexTiling;
// int _TilingLayer_R_MatchScaling;
// float _TilingLayer_R_BlendMode;
// float _TilingLayer_R_BlendRadius;
// float _TilingLayer_R_MaskContrast;
// float _TilingLayer_R_MaskIntensity;
// float4 _TilingLayer_G_BaseColor;
// float _TilingLayer_G_NormalScale;
// float _TilingLayer_G_Reflectance;
// float _TilingLayer_G_HeightOffset;
// int _TilingLayer_G_Porosity;
// float _TilingLayer_G_Tiling;
// int _TilingLayer_G_Use2U;
// int _TilingLayer_G_HexTiling;
// int _TilingLayer_G_MatchScaling;
// float _TilingLayer_G_BlendMode;
// float _TilingLayer_G_BlendRadius;
// float _TilingLayer_G_MaskContrast;
// float _TilingLayer_G_MaskIntensity;
// float _AlphaCutoff;
// float _TerrainBlendHeight;
// float2 _TerrainBlendDetailHeight;
// float4  _DoubleSidedNormalModeConstants;
// #ifdef SHADER_GENERATE_VERSION
// DeclarePMPerMaterialDefine
// #endif
// CBUFFER_END

//--------------------------------------------------------------------

#define MATERIAL_USE_TANGENT_SPACE_NORMALMAP
#define MATERIAL_USE_AMBIENT_OCCLUSION
#define MATERIAL_USE_REFLECTANCE
#define MATERIAL_USE_UV1
#if defined(MATERIAL_USE_CUSTOM_OPTION0)
    #define MATERIAL_USE_USEVERTEXCOLOR
#endif
#if defined(MATERIAL_USE_CUSTOM_OPTION1)
    #define MATERIAL_USE_USEHEIGHTLERP
#endif
#if defined(MATERIAL_USE_CUSTOM_OPTION2)
    #define MATERIAL_USE_USEBASELAYER
#endif
#define MATERIAL_USE_SLAB

//--------------------------------------------------------------------

#define SM_ID SHADING_MODEL_ID_DefaultLit


// Enabled material features
#define MATERIAL_FEATURE_PROXY_TangentSpaceNormalMap SLAB_MATERIAL_FEATURE_TangentSpaceNormalMap
#define MATERIAL_FEATURE_PROXY_Reflectance SLAB_MATERIAL_FEATURE_Reflectance
#define MATERIAL_FEATURE_PROXY_Detail SLAB_MATERIAL_FEATURE_Detail

// Optional material features
// MM material feature define
#define SM_SUPPORTED_MATERIAL_FEATURES SM_MATERIAL_FEATURE_DefaultLit
#define MM_REQUESTED_MATERIAL_FEATURES (MATERIAL_FEATURE_PROXY_TangentSpaceNormalMap | MATERIAL_FEATURE_PROXY_Reflectance | MATERIAL_FEATURE_PROXY_Detail)
#define MM_MATERIAL_FEATURE ( MM_REQUESTED_MATERIAL_FEATURES & SM_SUPPORTED_MATERIAL_FEATURES )
// MM used slab features
#define MM_USED_SLAB_PARAMS (SLAB_PARAMS_AO | SLAB_PARAMS_CustomTBN | SLAB_PARAMS_TangentMap | SLAB_PARAMS_TangentSpaceNormal | SLAB_PARAMS_Decal | SLAB_PARAMS_PluginChannelData | SLAB_PARAMS_TerrainBlend | SLAB_PARAMS_Base | SLAB_PARAMS_Generic | SLAB_PARAMS_Geometry | SLAB_PARAMS_Specular | SLAB_PARAMS_Emission | SLAB_PARAMS_Detail)

// material input struct
struct FSlabParams_MInput
{
    // Always needed
    // AO;
    float AO_AmbientOcclusion;
    
    // FSlabParams_CustomTBN CustomTBN;
    // FSlabParams_TangentMap TangentMap;
    // FSlabParams_TangentSpaceNormal TangentSpaceNormal;
    float3 TangentSpaceNormal_NormalTS;
    // FSlabParams_Decal Decal;
    // FSlabParams_PluginChannelData PluginChannelData;
    // FSlabParams_TerrainBlend TerrainBlend;
    // Base;
    float3 Base_Color;
    // float Base_Opacity;
    float Base_Metallic;
    float Base_Roughness;
    
    // FSlabParams_Generic Generic;
    // FSlabParams_Geometry Geometry;
    
    // Specular;
    float Specular_Reflectance;
    
    // Slab feature params
    // FSlabParams_Emission Emission;

    // Material feature params
    // Detail;
    float Detail_Height;
    
    // Material feature & slab feature params

    // ================================================================================
    // // just for debug
    // #if defined(USE_DEBUG_MODE) || defined(_USE_DEBUG_MODE_DEFERRED)
    // FSlabParams_DecalDebug  DecalDebug;
    // FDebugCustomData        DebugCustomData;
    // #endif
    // ================================================================================

};

#define MInputType FSlabParams_MInput

#endif // MM_EV_LAYEREDARCHITECTURE_HEADER_HLSL
//ss:[[2086904007]]

