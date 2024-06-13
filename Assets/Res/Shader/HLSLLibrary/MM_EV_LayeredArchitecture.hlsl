#ifndef MM_EV_LAYEREDARCHITECTURE
#define MM_EV_LAYEREDARCHITECTURE

#include "LayeredSurface.hlsl"

void TilingLayer_float( float2 TilingLayer_Coordinate,
                        UnityTexture2D _TilingLayer_BaseMap,
                        float4 _TilingLayer_BaseColor,
                        UnityTexture2D _TilingLayer_NormalMap,
                        float _TilingLayer_NormalScale,
                        UnityTexture2D _TilingLayer_MaskMap,
                        float _TilingLayer_Reflectance,
                        float _TilingLayer_HeightOffset,
                        int _TilingLayer_HexTiling,
                        
                        out float3 Base_Color,
                        out float3 TangentSpaceNormal_NormalTS,
                        out float Base_Metallic,
                        out float AO_AmbientOcclusion,
                        out float Detail_Height,
                        out float Base_Roughness,
                        out float Specular_Reflectance)
{
    MInputType MInput;
    SetupMInput(MInput);
    MaterialLayer MLayer;
    SetupMaterialLayer(	_TilingLayer_BaseMap.tex,
                        _TilingLayer_BaseColor,
                        _TilingLayer_NormalMap.tex,
                        _TilingLayer_NormalScale,
                        _TilingLayer_MaskMap.tex,
                        _TilingLayer_Reflectance,
                        _TilingLayer_HeightOffset,
                        MLayer
                        );
    
    if(_TilingLayer_HexTiling > FLT_EPS)
    {
        InitializeTilingLayer_Hex(MLayer, TilingLayer_Coordinate, MInput);
    }
    else
    {
        InitializeTilingLayer(MLayer, TilingLayer_Coordinate, MInput);
    }
    Base_Color                  = MInput.Base_Color;
    TangentSpaceNormal_NormalTS = MInput.TangentSpaceNormal_NormalTS;
    Base_Metallic               = MInput.Base_Metallic;
    AO_AmbientOcclusion         = MInput.AO_AmbientOcclusion;
    Detail_Height               = MInput.Detail_Height;
    Base_Roughness              = MInput.Base_Roughness;
    Specular_Reflectance        = MInput.Specular_Reflectance;
}


void Baselayered_float(float2 BaseCoordinate,
                        Texture2D _BaseLayer_MaskMap,
                        Texture2D _BaseLayer_NormalMap,
                        float _BaseLayer_NormalScale,
// MInput---------------
                        inout float3 Base_Color,
                        inout float3 TangentSpaceNormal_NormalTS,
                        inout float Base_Metallic,
                        inout float AO_AmbientOcclusion,
                        inout float Detail_Height,
                        inout float Base_Roughness,
                        inout float Specular_Reflectance)
{
    MInputType MInput;
    MInput.Base_Color                  = Base_Color;
    MInput.TangentSpaceNormal_NormalTS = TangentSpaceNormal_NormalTS;
    MInput.Base_Metallic               = Base_Metallic;
    MInput.AO_AmbientOcclusion         = AO_AmbientOcclusion;
    MInput.Detail_Height               = Detail_Height;
    MInput.Base_Roughness              = Base_Roughness;
    MInput.Specular_Reflectance        = Specular_Reflectance;
    
    float4 MaskMap = SAMPLE_TEXTURE2D(_BaseLayer_MaskMap, SamplerLinearRepeat, BaseCoordinate);
    float4 NormalMap = SAMPLE_TEXTURE2D(_BaseLayer_NormalMap, SamplerLinearRepeat, BaseCoordinate);
    float3 NormalTS = GetNormalTSFromNormalTex(NormalMap, _BaseLayer_NormalScale);
    MInput.TangentSpaceNormal_NormalTS = BlendAngelCorrectedNormals(NormalTS, MInput.TangentSpaceNormal_NormalTS);
    MInput.AO_AmbientOcclusion *= GetMaterialAOFromMaskMap(MaskMap);

    Base_Color                  = MInput.Base_Color;
    TangentSpaceNormal_NormalTS = MInput.TangentSpaceNormal_NormalTS;
    Base_Metallic               = MInput.Base_Metallic;
    AO_AmbientOcclusion         = MInput.AO_AmbientOcclusion;
    Detail_Height               = MInput.Detail_Height;
    Base_Roughness              = MInput.Base_Roughness;
    Specular_Reflectance        = MInput.Specular_Reflectance;
}


// ===================================================================================================================

// ===================================================================================================================

#endif //MM_EV_LAYEREDARCHITECTURE