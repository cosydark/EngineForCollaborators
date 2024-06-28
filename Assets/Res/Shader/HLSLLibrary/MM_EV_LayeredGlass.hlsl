// #include <UnityShaderVariables.cginc>
#ifndef MM_EV_LAYEREDROCK
#define MM_EV_LAYEREDROCK

#include "LayeredSurface.hlsl"
#include "LayeredDecal.hlsl"

void DecalLayerR_float(
                        float BlendMask,
                        float2 DecalLayer_R_BaseMapUV,
                        UnityTexture2D _DecalLayer_R_BaseMap,
                        float4 _DecalLayer_R_BaseColor,
                        UnityTexture2D _DecalLayer_R_NormalMap,
                        float _DecalLayer_R_NormalScale,
                        UnityTexture2D _DecalLayer_R_MaskMap,
                        float _DecalLayer_R_Metallic,
                        float _DecalLayer_R_Occlusion,
                        float _DecalLayer_R_Roughness,
                        float _DecalLayer_R_Reflectance,
                        bool _DecalLayer_R_AffectBaseColor,
                        bool _DecalLayer_R_AffectOpacity,
                        bool _DecalLayer_R_AffectNormal,
                        bool _DecalLayer_R_AffectMetal,
                        bool _DecalLayer_R_AffectAmbientOcclusion,
                        bool _DecalLayer_R_AffectRoughness,
                        bool _DecalLayer_R_AffectReflectance,
// MInput---------------
                        float3 Base_Color,
                        float Base_Opacity,
                        float3 TangentSpaceNormal_NormalTS,
                        float Base_Metallic,
                        float AO_AmbientOcclusion,
                        float Detail_Height,
                        float Base_Roughness,
                        float Specular_Reflectance,
// MInput-Out-----------
                        out float3 Out_Base_Color,
                        out float  Out_Base_Opacity,
                        out float3 Out_TangentSpaceNormal_NormalTS,
                        out float Out_Base_Metallic,
                        out float Out_AO_AmbientOcclusion,
                        out float Out_Detail_Height,
                        out float Out_Base_Roughness,
                        out float Out_Specular_Reflectance)
{
    // MInput---------------
    MInputType MInput;
    MInput.Base_Color                  = Base_Color;
    MInput.Base_Opacity                = Base_Opacity;
    MInput.TangentSpaceNormal_NormalTS = TangentSpaceNormal_NormalTS;
    MInput.Base_Metallic               = Base_Metallic;
    MInput.AO_AmbientOcclusion         = AO_AmbientOcclusion;
    MInput.Detail_Height               = Detail_Height;
    MInput.Base_Roughness              = Base_Roughness;
    MInput.Specular_Reflectance        = Specular_Reflectance;
    // MInput---------------
    DecalLayer SMLayerDecal01;
    SetupDecalLayer(_DecalLayer_R_BaseMap.tex,
                    _DecalLayer_R_BaseColor,
                    _DecalLayer_R_NormalMap.tex,
                    _DecalLayer_R_NormalScale,
                    _DecalLayer_R_MaskMap.tex,
                    _DecalLayer_R_Metallic,
                    _DecalLayer_R_Occlusion,
                    _DecalLayer_R_Roughness,
                    _DecalLayer_R_Reflectance,
                    BlendMask.r,
                    SMLayerDecal01);
    DecalToggle SMToggleDecal01;
    SetupDecalToggle(_DecalLayer_R_AffectBaseColor,
                     _DecalLayer_R_AffectOpacity,
                     _DecalLayer_R_AffectNormal,
                     _DecalLayer_R_AffectMetal,
                     _DecalLayer_R_AffectAmbientOcclusion,
                     _DecalLayer_R_AffectRoughness,
                     _DecalLayer_R_AffectReflectance,
                     SMToggleDecal01);
    WithDecal(SMLayerDecal01,SMToggleDecal01,DecalLayer_R_BaseMapUV,MInput);
    // MInput-Out-----------
    Out_Base_Color                  = MInput.Base_Color;
    Out_Base_Opacity                 = MInput.Base_Opacity;
    Out_TangentSpaceNormal_NormalTS = MInput.TangentSpaceNormal_NormalTS;
    Out_Base_Metallic               = MInput.Base_Metallic;
    Out_AO_AmbientOcclusion         = MInput.AO_AmbientOcclusion;
    Out_Detail_Height               = MInput.Detail_Height;
    Out_Base_Roughness              = MInput.Base_Roughness;
    Out_Specular_Reflectance        = MInput.Specular_Reflectance;
    // MInput-Out-----------
    
}


void DecalLayerG_float(bool IF_RUN_G,
                        float BlendMask,
                        float2 DecalLayer_G_BaseMapUV,
                        UnityTexture2D _DecalLayer_G_BaseMap,
                        float4 _DecalLayer_G_BaseColor,
                        UnityTexture2D _DecalLayer_G_NormalMap,
                        float _DecalLayer_G_NormalScale,
                        UnityTexture2D _DecalLayer_G_MaskMap,
                        float _DecalLayer_G_Metallic,
                        float _DecalLayer_G_Occlusion,
                        float _DecalLayer_G_Roughness,
                        float _DecalLayer_G_Reflectance,
                        bool _DecalLayer_G_AffectBaseColor,
                        bool _DecalLayer_G_AffectOpacity,
                        bool _DecalLayer_G_AffectNormal,
                        bool _DecalLayer_G_AffectMetal,
                        bool _DecalLayer_G_AffectAmbientOcclusion,
                        bool _DecalLayer_G_AffectRoughness,
                        bool _DecalLayer_G_AffectReflectance,
// MInput---------------
                        float3 Base_Color,
                        float Base_Opacity,
                        float3 TangentSpaceNormal_NormalTS,
                        float Base_Metallic,
                        float AO_AmbientOcclusion,
                        float Detail_Height,
                        float Base_Roughness,
                        float Specular_Reflectance,
// MInput-Out-----------
                        out float3 Out_Base_Color,
                        out float  Out_Base_Opacity,
                        out float3 Out_TangentSpaceNormal_NormalTS,
                        out float Out_Base_Metallic,
                        out float Out_AO_AmbientOcclusion,
                        out float Out_Detail_Height,
                        out float Out_Base_Roughness,
                        out float Out_Specular_Reflectance)
{
    // MInput---------------
    MInputType MInput;
    MInput.Base_Color                  = Base_Color;
    MInput.Base_Opacity                  = Base_Opacity;
    MInput.TangentSpaceNormal_NormalTS = TangentSpaceNormal_NormalTS;
    MInput.Base_Metallic               = Base_Metallic;
    MInput.AO_AmbientOcclusion         = AO_AmbientOcclusion;
    MInput.Detail_Height               = Detail_Height;
    MInput.Base_Roughness              = Base_Roughness;
    MInput.Specular_Reflectance        = Specular_Reflectance;
    // MInput---------------
    if (IF_RUN_G)
    {
        DecalLayer SMLayerDecal02;
        SetupDecalLayer(_DecalLayer_G_BaseMap.tex,
                        _DecalLayer_G_BaseColor,
                        _DecalLayer_G_NormalMap.tex,
                        _DecalLayer_G_NormalScale,
                        _DecalLayer_G_MaskMap.tex,
                        _DecalLayer_G_Metallic,
                        _DecalLayer_G_Occlusion,
                        _DecalLayer_G_Roughness,
                        _DecalLayer_G_Reflectance,
                        BlendMask,
                        SMLayerDecal02);
        DecalToggle SMToggleDecal02;
        SetupDecalToggle(_DecalLayer_G_AffectBaseColor,
                         _DecalLayer_G_AffectOpacity,
                         _DecalLayer_G_AffectNormal,
                         _DecalLayer_G_AffectMetal,
                         _DecalLayer_G_AffectAmbientOcclusion,
                         _DecalLayer_G_AffectRoughness,
                         _DecalLayer_G_AffectReflectance,
                         SMToggleDecal02);
        WithDecal(SMLayerDecal02,SMToggleDecal02,DecalLayer_G_BaseMapUV,MInput);
       
    }

    // MInput-Out-----------
    Out_Base_Color                  = MInput.Base_Color;
    Out_Base_Opacity               = MInput.Base_Opacity;
    Out_TangentSpaceNormal_NormalTS = MInput.TangentSpaceNormal_NormalTS;
    Out_Base_Metallic               = MInput.Base_Metallic;
    Out_AO_AmbientOcclusion         = MInput.AO_AmbientOcclusion;
    Out_Detail_Height               = MInput.Detail_Height;
    Out_Base_Roughness              = MInput.Base_Roughness;
    Out_Specular_Reflectance        = MInput.Specular_Reflectance;
    // MInput-Out-----------
    
}
// void LocalScaleX_float()
// {
//     // Scale
//     float LocalScaleX = length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x));
// }


// ===================================================================================================================

// ===================================================================================================================

#endif //MM_EV_LAYEREDROCK