// Author: Ivan Jin

#ifndef XRENDER_COMMON_HLSL
#define XRENDER_COMMON_HLSL

///////////////////////////////////////////////////////////////////////////////////////////////
/// API
/// 
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/D3D11.hlsl"
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
// #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

///////////////////////////////////////////////////////////////////////////////////////////////

#define PI                              3.14159265358979323846
#define TWO_PI                          6.28318530717958647693
#define FOUR_PI                         12.5663706143591729538
#define INV_PI                          0.31830988618379067154
#define INV_TWO_PI                      0.15915494309189533577
#define INV_FOUR_PI                     0.07957747154594766788
#define HALF_PI                         1.57079632679489661923
#define INV_HALF_PI                     0.63661977236758134308
#define LOG2_E                          1.44269504088896340736
#define INV_SQRT2                       0.70710678118654752440
#define PI_DIV_FOUR                     0.78539816339744830961

#define FLT_INF                         asfloat(0x7F800000)
#define FLT_EPS                         5.960464478e-8  // 2^-24, machine epsilon: 1 + EPS = 1 (half of the ULP for 1.0f)



#define FLT_MIN                         1.175494351e-38 // Minimum normalized positive floating-point number
#define FLT_MAX                         3.402823466e+38 // Maximum representable floating-point number
#define HALF_EPS                        4.8828125e-4    // 2^-11, machine epsilon: 1 + EPS = 1 (half of the ULP for 1.0f)



#define HALF_MIN                        6.103515625e-5  // 2^-14, the same value for 10, 11 and 16-bit: https://www.khronos.org/opengl/wiki/Small_Float_Formats



#define HALF_MIN_SQRT                   0.0078125  // 2^-7 == sqrt(HALF_MIN), useful for ensuring HALF_MIN after x^2
#define HALF_MAX                        65504.0
#define UINT_MAX                        0xFFFFFFFFu
#define INT_MAX                         0x7FFFFFFF
#define MAX_11_BITS_FLOAT               65024.0f
#define MAX_10_BITS_FLOAT               64512.0f
#define CLAMP_MAX                       65472.0

#define MILLIMETERS_PER_METER           1000
#define METERS_PER_MILLIMETER           rcp(MILLIMETERS_PER_METER)
#define CENTIMETERS_PER_METER           100
#define METERS_PER_CENTIMETER           rcp(CENTIMETERS_PER_METER)

#define CUBEMAPFACE_POSITIVE_X          0
#define CUBEMAPFACE_NEGATIVE_X          1
#define CUBEMAPFACE_POSITIVE_Y          2
#define CUBEMAPFACE_NEGATIVE_Y          3
#define CUBEMAPFACE_POSITIVE_Z          4
#define CUBEMAPFACE_NEGATIVE_Z          5

///////////////////////////////////////////////////////////////////////////////////////////////

#define FLT_EPS                         5.960464478e-8  // 2^-24, machine epsilon: 1 + EPS = 1 (half of the ULP for 1.0f)

#define SamplerPointClamp sampler_PointClamp
#define SamplerLinearClamp sampler_LinearClamp
#define SamplerPointRepeat sampler_PointRepeat
#define SamplerLinearRepeat sampler_LinearRepeat
#define SamplerTriLinearCLamp sampler_TriLinearClamp
#define SamplerTriLinearRepeat sampler_TriLinearRepeat
#define SamplerLinearRepeatAniso8 sampler_LinearRepeatAniso8
#define SamplerLinearClampAniso8 sampler_LinearClampAniso8;

#define SAMPLE_TEXTURE2D_HEX(textureName, samplerName, coord2) SampleTexture2DHex(textureName, samplerName, coord2)

// TEMPLATE_SWAP(Swap) // Define a Swap(a, b) function for all types

///////////////////////////////////////////////////////////////////////////////////////////////
//
// bool HasFlag(uint bitfield, uint flag)
// {
// 	return (bitfield & flag) != 0;
// }

// Scale default = 1.0
float3 SGUnpackNormalAG(float4 PackedNormal, float Scale)
{
    float3 Normal;
    Normal.xy = PackedNormal.ag * 2.0 - 1.0;
    Normal.z = max(1.0e-16, sqrt(1.0 - saturate(dot(Normal.xy, Normal.xy))));
    Normal.xy *= Scale;
    return Normal;
}

// Unpack normal as DXT5nm (1, y, 0, x) or BC5 (x, y, 0, 1)
float3 SGUnpackNormalmapRGorAG(float4 packedNormal, float scale = 1.0)
{
    // Convert to (?, y, 0, x)
    packedNormal.a *= packedNormal.r;
    
    return SGUnpackNormalAG(packedNormal, scale);
}
// Unpack from normal map
// Scale default = 1.0
float3 SGUnpackNormalScale(float4 packedNormal, float bumpScale)
{
    #if defined(UNITY_ASTC_NORMALMAP_ENCODING)
    return SGUnpackNormalAG(packedNormal, bumpScale);
    #elif defined(UNITY_NO_DXT5nm)
    return UnpackNormalRGB(packedNormal, bumpScale);
    #else
    return SGUnpackNormalmapRGorAG(packedNormal, bumpScale);
    #endif
}
float3 GetNormalTSFromNormalTex(float4 NormalMap, float NormalScale = 1.0)
{
    #if defined(MATERIAL_USE_NORMALTEX_NO_FLIP_Y)
    float4 PackedNormal = NormalMap.rgba;
    #else
    float4 PackedNormal = float4(NormalMap.r, 1.0 - NormalMap.g, NormalMap.b, NormalMap.a);
    #endif

    return SGUnpackNormalScale(PackedNormal, NormalScale);
}



// =================================== FMaterialInput ==========================================

// float3 GetBaseColor(float4 AlbedoTex)
// {
//     return GammaToLinear(AlbedoTex.rgb);
// }

float GetOpacity(float4 AlbedoTex)
{
    return AlbedoTex.a;
}

float GetMaterialMetallicFromMaskMap(float4 MaskMap)
{
    return MaskMap.r;
}

float GetPerceptualRoughnessFromMaskMap(float4 MaskMap)
{
    return MaskMap.a;
}

float GetMaterialAOFromMaskMap(float4 MaskMap)
{
    return MaskMap.g;
}

float GetHeightFromMaskMap(float4 MaskMap)
{
    return MaskMap.b;
}

float GetThicknessFromMaskMap(float4 MaskMap)
{
    return MaskMap.b;
}

// float GetGeometricBakedAOFromVertexColor(in const FPixelInput PixelIn)
// {
//     #if defined(MATERIAL_USE_GEOMETRIC_BAKED_AO)
//     return PixelIn.VertexColor.a;
//     #else
//     return 1.0;
//     #endif
// }

float GetMaterialHeightFromMaskMap(float4 MaskMap)
{
    #if defined(MATERIAL_USE_HEIGHT)
    return MaskMap.b;
    #else
    return 1.0;
    #endif
}

float3 GetEmissionFromEmissioMap(float3 EmissionMap)
{
    return float3(0.0f, 0.0f, 0.0f);
}

#define MATERIAL_USE_NORMALTEX_NO_FLIP_Y

// float3 GetNormalTSFromNormalTex(float4 NormalMap, float NormalScale = 1.0)
// {
//     #if defined(MATERIAL_USE_NORMALTEX_NO_FLIP_Y)
//     float4 PackedNormal = NormalMap.rgba;
//     #else
//     float4 PackedNormal = float4(NormalMap.r, 1.0 - NormalMap.g, NormalMap.b, NormalMap.a);
//     #endif
//
//     return UnpackNormalScale(PackedNormal, NormalScale);
// }

float3 UDNNormalBlend(float3 N1, float3 N2)
{
    return SafeNormalize(float3(N1.xy + N2.xy, N1.z));
}



#if !defined(MATERIAL_USE_SLAB)
bool HasFeature(uint bitfield, uint flag)
{
	return (bitfield & flag) != 0;
}
#endif

static const float3x3 Identity3x3 = {
	1, 0, 0,
	0, 1, 0,
	0, 0, 1
};

static const float4x4 Identity4x4 = {
	1, 0, 0, 0,
	0, 1, 0, 0,
	0, 0, 1, 0,
	0, 0, 0, 1
};

///////////////////////////////////////////////////////////////////////////////////////////////

//#include "./API/Wave.hlsl"
// #include "./API/RenderSubpass.hlsl"
// #include "./API/Intrinsic.hlsl"
//
// #include "./API/Validate.hlsl"
//
// #include "./Math.hlsl"

///////////////////////////////////////////////////////////////////////////////////////////////

#endif // XRENDER_COMMON_HLSL
