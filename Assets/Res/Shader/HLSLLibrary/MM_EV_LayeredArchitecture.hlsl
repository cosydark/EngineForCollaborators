// #include <UnityShaderVariables.cginc>
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

void TilingLayerR_float(bool IF_RUN_R,
                        bool MATERIAL_USE_USEHEIGHTLERP,
                        float4 BlendMask,
                        float2 TilingLayer_R_Coordinate,
                        UnityTexture2D _TilingLayer_R_BaseMap,
                        float4 _TilingLayer_R_BaseColor,
                        UnityTexture2D _TilingLayer_R_NormalMap,
                        float _TilingLayer_R_NormalScale,
                        UnityTexture2D _TilingLayer_R_MaskMap,
                        float _TilingLayer_R_Reflectance,
                        float _TilingLayer_R_HeightOffset,
                        int _TilingLayer_R_HexTiling,
                        float _TilingLayer_R_BlendMode,
                        float _TilingLayer_R_BlendRadius,
                        float _TilingLayer_R_MaskContrast,
                        float _TilingLayer_R_MaskIntensity,
// MInput---------------
                        float3 Base_Color,
                        float3 TangentSpaceNormal_NormalTS,
                        float Base_Metallic,
                        float AO_AmbientOcclusion,
                        float Detail_Height,
                        float Base_Roughness,
                        float Specular_Reflectance,
// MInput-Out-----------
                        out float3 Out_Base_Color,
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
    MInput.TangentSpaceNormal_NormalTS = TangentSpaceNormal_NormalTS;
    MInput.Base_Metallic               = Base_Metallic;
    MInput.AO_AmbientOcclusion         = AO_AmbientOcclusion;
    MInput.Detail_Height               = Detail_Height;
    MInput.Base_Roughness              = Base_Roughness;
    MInput.Specular_Reflectance        = Specular_Reflectance;
    // MInput---------------
    if (IF_RUN_R)
    {
        BlendMask.r = saturate(pow(BlendMask.r, _TilingLayer_R_MaskContrast) * _TilingLayer_R_MaskIntensity);
        MaterialLayer MLayer_R;
        SetupMaterialLayer(	_TilingLayer_R_BaseMap.tex,
                            _TilingLayer_R_BaseColor,
                            _TilingLayer_R_NormalMap.tex,
                            _TilingLayer_R_NormalScale,
                            _TilingLayer_R_MaskMap.tex,
                            _TilingLayer_R_Reflectance,
                            _TilingLayer_R_HeightOffset,
                            MLayer_R
                            );

        if(MATERIAL_USE_USEHEIGHTLERP)
        {
            if(_TilingLayer_R_HexTiling > FLT_EPS)
            {
                BlendWithHeight_Hex(MLayer_R, TilingLayer_R_Coordinate, BlendMask.r, _TilingLayer_R_BlendRadius, _TilingLayer_R_BlendMode, MInput);
            }
            else
            {
                BlendWithHeight(MLayer_R, TilingLayer_R_Coordinate, BlendMask.r, _TilingLayer_R_BlendRadius, _TilingLayer_R_BlendMode, MInput);
            }
        }else
        {
            if(_TilingLayer_R_HexTiling > FLT_EPS)
            {
                BlendWithOutHeight_Hex(MLayer_R, TilingLayer_R_Coordinate, BlendMask.r, MInput);
            }
            else
            {
                BlendWithOutHeight(MLayer_R, TilingLayer_R_Coordinate, BlendMask.r, MInput);
            }
        }
    }

    // MInput-Out-----------
    Out_Base_Color                  = MInput.Base_Color;
    Out_TangentSpaceNormal_NormalTS = MInput.TangentSpaceNormal_NormalTS;
    Out_Base_Metallic               = MInput.Base_Metallic;
    Out_AO_AmbientOcclusion         = MInput.AO_AmbientOcclusion;
    Out_Detail_Height               = MInput.Detail_Height;
    Out_Base_Roughness              = MInput.Base_Roughness;
    Out_Specular_Reflectance        = MInput.Specular_Reflectance;
    // MInput-Out-----------
    
}


void TilingLayerG_float(bool IF_RUN_G,
                        bool MATERIAL_USE_USEHEIGHTLERP,
                        float4 BlendMask,
                        float2 TilingLayer_G_Coordinate,
                        UnityTexture2D _TilingLayer_G_BaseMap,
                        float4 _TilingLayer_G_BaseColor,
                        UnityTexture2D _TilingLayer_G_NormalMap,
                        float _TilingLayer_G_NormalScale,
                        UnityTexture2D _TilingLayer_G_MaskMap,
                        float _TilingLayer_G_Reflectance,
                        float _TilingLayer_G_HeightOffset,
                        int _TilingLayer_G_HexTiling,
                        float _TilingLayer_G_BlendMode,
                        float _TilingLayer_G_BlendRadius,
                        float _TilingLayer_G_MaskContrast,
                        float _TilingLayer_G_MaskIntensity,
// MInput---------------
                        float3 Base_Color,
                        float3 TangentSpaceNormal_NormalTS,
                        float Base_Metallic,
                        float AO_AmbientOcclusion,
                        float Detail_Height,
                        float Base_Roughness,
                        float Specular_Reflectance,
// MInput-Out-----------
                        out float3 Out_Base_Color,
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
    MInput.TangentSpaceNormal_NormalTS = TangentSpaceNormal_NormalTS;
    MInput.Base_Metallic               = Base_Metallic;
    MInput.AO_AmbientOcclusion         = AO_AmbientOcclusion;
    MInput.Detail_Height               = Detail_Height;
    MInput.Base_Roughness              = Base_Roughness;
    MInput.Specular_Reflectance        = Specular_Reflectance;
    // MInput---------------
    if (IF_RUN_G)
    {
        BlendMask.g = saturate(pow(BlendMask.g, _TilingLayer_G_MaskContrast) * _TilingLayer_G_MaskIntensity);
        MaterialLayer MLayer_G;
        SetupMaterialLayer(	_TilingLayer_G_BaseMap.tex,
                            _TilingLayer_G_BaseColor,
                            _TilingLayer_G_NormalMap.tex,
                            _TilingLayer_G_NormalScale,
                            _TilingLayer_G_MaskMap.tex,
                            _TilingLayer_G_Reflectance,
                            _TilingLayer_G_HeightOffset,
                            MLayer_G
                            );
        
        if(MATERIAL_USE_USEHEIGHTLERP)
        {
            if(_TilingLayer_G_HexTiling > FLT_EPS)
            {
                BlendWithHeight_Hex(MLayer_G, TilingLayer_G_Coordinate, BlendMask.g, _TilingLayer_G_BlendRadius, _TilingLayer_G_BlendMode, MInput);
            }
            else
            {
                BlendWithHeight(MLayer_G, TilingLayer_G_Coordinate, BlendMask.g, _TilingLayer_G_BlendRadius, _TilingLayer_G_BlendMode, MInput);
            }
        }else
        {
            if(_TilingLayer_G_HexTiling > FLT_EPS)
            {
                BlendWithOutHeight_Hex(MLayer_G, TilingLayer_G_Coordinate, BlendMask.g, MInput);
            }
            else
            {
                BlendWithOutHeight(MLayer_G, TilingLayer_G_Coordinate, BlendMask.g, MInput);
            }
        }
    }

    // MInput-Out-----------
    Out_Base_Color                  = MInput.Base_Color;
    Out_TangentSpaceNormal_NormalTS = MInput.TangentSpaceNormal_NormalTS;
    Out_Base_Metallic               = MInput.Base_Metallic;
    Out_AO_AmbientOcclusion         = MInput.AO_AmbientOcclusion;
    Out_Detail_Height               = MInput.Detail_Height;
    Out_Base_Roughness              = MInput.Base_Roughness;
    Out_Specular_Reflectance        = MInput.Specular_Reflectance;
    // MInput-Out-----------
    
}



void Baselayered_float( bool USEBASELAYER,
                        float2 BaseCoordinate,
                        UnityTexture2D _BaseLayer_MaskMap,
                        UnityTexture2D _BaseLayer_NormalMap,
                        float _BaseLayer_NormalScale,
// MInput---------------
                        float3 Base_Color,
                        float3 TangentSpaceNormal_NormalTS,
                        float Base_Metallic,
                        float AO_AmbientOcclusion,
                        float Detail_Height,
                        float Base_Roughness,
                        float Specular_Reflectance,
// MInput-Out-----------
                        out float3 Out_Base_Color,
                        out float3 Out_TangentSpaceNormal_NormalTS,
                        out float Out_Base_Metallic,
                        out float Out_AO_AmbientOcclusion,
                        out float Out_Detail_Height,
                        out float Out_Base_Roughness,
                        out float Out_Specular_Reflectance)
{
    MInputType MInput;
    MInput.Base_Color                  = Base_Color;
    MInput.TangentSpaceNormal_NormalTS = TangentSpaceNormal_NormalTS;
    MInput.Base_Metallic               = Base_Metallic;
    MInput.AO_AmbientOcclusion         = AO_AmbientOcclusion;
    MInput.Detail_Height               = Detail_Height;
    MInput.Base_Roughness              = Base_Roughness;
    MInput.Specular_Reflectance        = Specular_Reflectance;

    if (USEBASELAYER)
    {
        float4 MaskMap = SAMPLE_TEXTURE2D(_BaseLayer_MaskMap.tex, SamplerLinearRepeat, BaseCoordinate);
        float4 NormalMap = SAMPLE_TEXTURE2D(_BaseLayer_NormalMap.tex, SamplerLinearRepeat, BaseCoordinate);
        NormalMap.g = 1 - NormalMap.g;
        float3 NormalTS = GetNormalTSFromNormalTex(NormalMap, _BaseLayer_NormalScale);
        MInput.TangentSpaceNormal_NormalTS = BlendAngelCorrectedNormals(NormalTS, MInput.TangentSpaceNormal_NormalTS);
        MInput.AO_AmbientOcclusion *= GetMaterialAOFromMaskMap(MaskMap);
    }

    Out_Base_Color                  = MInput.Base_Color;
    Out_TangentSpaceNormal_NormalTS = MInput.TangentSpaceNormal_NormalTS;
    Out_Base_Metallic               = MInput.Base_Metallic;
    Out_AO_AmbientOcclusion         = MInput.AO_AmbientOcclusion;
    Out_Detail_Height               = MInput.Detail_Height;
    Out_Base_Roughness              = MInput.Base_Roughness;
    Out_Specular_Reflectance        = MInput.Specular_Reflectance;
}

// void LocalScaleX_float()
// {
//     // Scale
//     float LocalScaleX = length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x));
// }


// ===================================================================================================================

// ===================================================================================================================

#endif //MM_EV_LAYEREDARCHITECTURE