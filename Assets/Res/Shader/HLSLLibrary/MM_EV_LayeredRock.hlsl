// #include <UnityShaderVariables.cginc>
#ifndef MM_EV_LAYEREDROCK
#define MM_EV_LAYEREDROCK

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
        if(_TilingLayer_R_HexTiling > FLT_EPS)
        {
            BlendWithHeight_Hex(MLayer_R, TilingLayer_R_Coordinate, BlendMask.r, _TilingLayer_R_BlendRadius, _TilingLayer_R_BlendMode, MInput);
        }
        else
        {
            BlendWithHeight(MLayer_R, TilingLayer_R_Coordinate, BlendMask.r, _TilingLayer_R_BlendRadius, _TilingLayer_R_BlendMode, MInput);
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
        
        if(_TilingLayer_G_HexTiling > FLT_EPS)
        {
            BlendWithHeight_Hex(MLayer_G, TilingLayer_G_Coordinate, BlendMask.g, _TilingLayer_G_BlendRadius, _TilingLayer_G_BlendMode, MInput);
        }
        else
        {
            BlendWithHeight(MLayer_G, TilingLayer_G_Coordinate, BlendMask.g, _TilingLayer_G_BlendRadius, _TilingLayer_G_BlendMode, MInput);
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


void AdditionalLayer_float(bool IF_RUN_ADD,
                        float4 BlendMask,
                        float4 _AdditionalLayer_BaseColor,
                        float _AdditionalLayer_NormalScale,
                        float _AdditionalLayer_Height,
                        float _AdditionalLayer_Roughness,
                        float _AdditionalLayer_Reflectance,
                        float _AdditionalLayer_MaskContrast,
                        float _AdditionalLayer_MaskIntensity,
                        float _AdditionalLayer_UseHeightLerp,
                        float _AdditionalLayer_BlendMode,
                        float _AdditionalLayer_BlendRadius,
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
    if (IF_RUN_ADD)
    {
        BlendMask.b = saturate(pow(BlendMask.b, _AdditionalLayer_MaskContrast) * _AdditionalLayer_MaskIntensity);
        SimpleMaterialLayer SMLayer_B;
        SetupSMaterialLayer(	_AdditionalLayer_BaseColor,
                                _AdditionalLayer_NormalScale,
                                float4(0, 1, _AdditionalLayer_Height * 2, _AdditionalLayer_Roughness),
                                _AdditionalLayer_Reflectance,
                                SMLayer_B
                            );
        if(_AdditionalLayer_UseHeightLerp > FLT_EPS)
        {
            BlendWithHeightNoTexture(SMLayer_B, BlendMask.b, _AdditionalLayer_BlendRadius, _AdditionalLayer_BlendMode, MInput);
        }
        else
        {
            BlendWithOutHeightNoTexture(SMLayer_B, BlendMask.b, MInput);
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


void DetailLayer_float(bool IF_RUN_D,
                        float2 DetailCoordinate,
                        UnityTexture2D _Detail_BaseMap,
                        UnityTexture2D _Detail_NormalMap,
                        float _Detail_NormalScale,
                        UnityTexture2D _Detail_MaskMap,
                        float _Detail_AmbientOcclusion,
                        float _Detail_AlbedoGrayValue,
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
    if (IF_RUN_D)
    {
        DetailLayer DLayer;
        SetupDetailLayer( _Detail_BaseMap.tex,
                          _Detail_NormalMap.tex,
                          _Detail_NormalScale,
                          _Detail_MaskMap.tex,
                          _Detail_AmbientOcclusion,
                          _Detail_AlbedoGrayValue,
                          DLayer
                        );
        BlendDetailLayer(DLayer, DetailCoordinate, MInput);
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


void BaseToppinglayered_float( bool TOPPING,
                        bool USEBASELAYER,
                        float2 BaseCoordinate,
                        UnityTexture2D _BaseLayer_MaskMap,
                        UnityTexture2D _BaseLayer_NormalMap,
                        float _BaseLayer_NormalScale,
                        float2 ToppingCoordinates,
                        UnityTexture2D _ToppingLayer_BaseMap,
                        float4 _ToppingLayer_BaseColor,
                        UnityTexture2D _ToppingLayer_NormalMap,
                        float _ToppingLayer_NormalScale,
                        UnityTexture2D _ToppingLayer_MaskMap,
                        float _ToppingLayer_Reflectance,
                        float _ToppingLayer_HeightOffset,
                        float _ToppingLayer_NormalIntensity,
                        float _ToppingLayer_Coverage,
                        float _ToppingLayer_Spread,
                        float _ToppingLayer_HexTiling,
                        float _ToppingLayer_UseHeightLerp,
                        float _ToppingLayer_BlendMode,
                        float _ToppingLayer_BlendRadius,
                        float3x3 PixelIn_TangentToWorldMatrix,
                        float3 PixelIn_GeometricNormalWS,
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

    float4 MaskMap = 0;
    float3 NormalTS = 0;
    if (USEBASELAYER)
    {
        MaskMap = SAMPLE_TEXTURE2D(_BaseLayer_MaskMap.tex, SamplerLinearRepeat, BaseCoordinate);
        float4 NormalMap = SAMPLE_TEXTURE2D(_BaseLayer_NormalMap.tex, SamplerLinearRepeat, BaseCoordinate);
        NormalMap.g = 1 - NormalMap.g;
        NormalTS = GetNormalTSFromNormalTex(NormalMap, _BaseLayer_NormalScale);
    }
    
    if (TOPPING)
    {
        
        float3 NormalWS;
        if (USEBASELAYER)
        {
            NormalWS = TransformVectorTSToVectorWS_RowMajor(NormalTS, PixelIn_TangentToWorldMatrix, true);
            NormalWS = lerp(PixelIn_GeometricNormalWS, NormalWS, _ToppingLayer_NormalIntensity);
        }
        else
        {
            NormalWS = PixelIn_GeometricNormalWS;
        }
        float3 NDotUp = dot(NormalWS, normalize(float3(0, 1, 0)));
        float Coverage = NDotUp - lerp(1, -1, _ToppingLayer_Coverage);
        Coverage = saturate(Coverage / _ToppingLayer_Spread);
        MaterialLayer MLayer_Detail;
        SetupMaterialLayer(	_ToppingLayer_BaseMap.tex,
                            _ToppingLayer_BaseColor,
                            _ToppingLayer_NormalMap.tex,
                            _ToppingLayer_NormalScale,
                            _ToppingLayer_MaskMap.tex,
                            _ToppingLayer_Reflectance,
                            _ToppingLayer_HeightOffset,
                            MLayer_Detail
                            );
        if(_ToppingLayer_HexTiling > FLT_EPS)
        {
            if(_ToppingLayer_UseHeightLerp > FLT_EPS)
            {
                BlendWithHeight_Hex(MLayer_Detail, ToppingCoordinates, Coverage, _ToppingLayer_BlendRadius, _ToppingLayer_BlendMode, MInput);
            }
            else
            {
                BlendWithOutHeight_Hex(MLayer_Detail, ToppingCoordinates, Coverage, MInput);
            }
        }
        else
        {
            if(_ToppingLayer_UseHeightLerp > FLT_EPS)
            {
                BlendWithHeight(MLayer_Detail, ToppingCoordinates, Coverage, _ToppingLayer_BlendRadius, _ToppingLayer_BlendMode, MInput);
            }
            else
            {
                BlendWithOutHeight(MLayer_Detail, ToppingCoordinates, Coverage, MInput);
            }
        }
        
    }

    if (USEBASELAYER)
    {
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

#endif //MM_EV_LAYEREDROCK