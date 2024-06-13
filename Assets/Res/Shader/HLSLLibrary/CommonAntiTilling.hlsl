#ifndef XRENDER_COMMON_ANTI_TILLING_HLSL
#define XRENDER_COMMON_ANTI_TILLING_HLSL

#include "Common.hlsl"

// Output: weights associated with each hex tile and integer centers
void TriangleGrid(out float3 Weights,
                  out int2 vertex1, out int2 vertex2, out int2 vertex3,
                  float2 UV)
{
    // Scaling of the input
    UV *= 2 * sqrt(3);
    // Skew input space into simplex triangle grid.
    const float2x2 gridToSkewedGrid = float2x2(1.0, -0.57735027, 0.0, 1.15470054);
    float2 skewedCoord = mul(gridToSkewedGrid, UV);
    int2 baseId = int2( floor ( skewedCoord ));
    float3 temp = float3( frac( skewedCoord ), 0);
    temp.z = 1.0 - temp.x - temp.y;
    float s = step(0.0, -temp.z);
    float s2 = 2*s-1;
    Weights.x = -temp.z*s2;
    Weights.y = s - temp.y*s2;
    Weights.z = s - temp.x*s2;
    vertex1 = baseId + int2(s,s);
    vertex2 = baseId + int2(s,1-s);
    vertex3 = baseId + int2(1-s,s);
}

float2 hash(float2 p)
{
    float2 r = mul(float2x2(127.1, 311.7, 269.5, 183.3), p);
    return frac( sin( r )*43758.5453 );
}

float2 MakeCenST(int2 Vertex)
{
    float2x2 invSkewMat = float2x2(1.0, 0.5, 0.0, 1.0/1.15470054);
    return mul(invSkewMat, Vertex) / (2 * sqrt(3));
}

float2x2 LoadRot2x2(int2 idx, float rotStrength)
{
    float angle = abs(idx.x*idx.y) + abs(idx.x+idx.y) + PI;
    // Remap to +/-pi.
    angle = fmod(angle, 2*PI);
    if(angle<0) angle += 2*PI;
    if(angle>PI) angle -= 2*PI;
    angle *= rotStrength;
    float cs = cos(angle), si = sin(angle);
    return float2x2(cs, -si, si, cs);
}

// ref: https://jcgt.org/published/0011/03/05/paper.pdf
// EmptyInput: Weights,UV1,UV2,UV3,DuvDx1,DuvDx2,DuvDx3,DuvDy1,DuvDy2,DuvDy3 
void HexTiling(float2 UV, float RotStrength,
               out float3 Weights, out float2 UV1,out float2 UV2,out float2 UV3,
               out float2 DuvDx1,out float2 DuvDx2,out float2 DuvDx3,
               out float2 DuvDy1,out float2 DuvDy2,out float2 DuvDy3)
{
    float2 DuvDx = ddx(UV);
    float2 DuvDy = ddy(UV);
    // Get triangle info
    int2 vertex1, vertex2, vertex3;
    TriangleGrid(Weights, vertex1, vertex2, vertex3, UV);
    float2x2 rot1 = LoadRot2x2(vertex1, RotStrength);
    float2x2 rot2 = LoadRot2x2(vertex2, RotStrength);
    float2x2 rot3 = LoadRot2x2(vertex3, RotStrength);
    float2 cen1 = MakeCenST(vertex1);
    float2 cen2 = MakeCenST(vertex2);
    float2 cen3 = MakeCenST(vertex3);
    UV1 = mul(UV - cen1, rot1) + cen1 + hash(vertex1);
    UV2 = mul(UV - cen2, rot2) + cen2 + hash(vertex2);
    UV3 = mul(UV - cen3, rot3) + cen3 + hash(vertex3);
    DuvDx1 = mul(DuvDx, rot1);
    DuvDx2 = mul(DuvDx, rot2);
    DuvDx3 = mul(DuvDx, rot3);
    DuvDy1 = mul(DuvDy, rot1);
    DuvDy2 = mul(DuvDy, rot2);
    DuvDy3 = mul(DuvDy, rot3);
	
    //weights = ProduceHexWeights(W.xyz, vertex1, vertex2, vertex3);
}

float3 Gain3(float3 x, float r)
{
    // Increase contrast when r > 0.5 and
    // reduce contrast if less.
    float k = log(1-r) / log(0.5);
    float3 s = 2*step(0.5, x);
    float3 m = 2*(1 - s);
    float3 res = 0.5*s + 0.25*m * pow(max(0.0, s + x*m), k);
    return res.xyz / (res.x+res.y+res.z);
}

void BaryWeightBlend(inout float3 Weights, float4 c1, float4 c2, float4 c3, float FallOffContrast, float Exp,float GainRatio = 0.5)
{
    // Use luminance as weight
    float3 Lw = float3(0.299, 0.587, 0.114);
    float3 Dw = float3(dot(c1.xyz,Lw),dot(c2.xyz,Lw),dot(c3.xyz,Lw));
    Dw = lerp(1.0, Dw, FallOffContrast); // 0.6
    Weights = Dw * pow(Weights, Exp); // 7
    Weights /= (Weights.x+Weights.y+Weights.z);
    if(GainRatio!=0.5) Weights = Gain3(Weights, GainRatio);
}

/// <summary>
/// HexRotation => (-20, 20); HexContrast => (0.001, 0.999); HexExp => (0, 20); HexGain => (0.001, 0.999)
/// </summary>
/// <returns></returns>
float4 SampleTexture2DHex(      Texture2D Map,
                                SamplerState Sampler,
                                float2 Coordinate,
                                float HexRotation = 0,
                                float HexContrast = 0.456,
                                float HexExp = 10,
                                float HexGain = 0.2
                          )
{
    float4 V = float4(0, 0, 0, 0);
    float3 Weights = float3(0, 0, 0);
    float2 UV1, UV2, UV3, DuvDx1, DuvDx2, DuvDx3, DuvDy1, DuvDy2, DuvDy3;
    HexTiling(Coordinate, HexRotation, Weights, UV1, UV2, UV3, DuvDx1, DuvDx2, DuvDx3, DuvDy1, DuvDy2, DuvDy3);
    float4 V1 = SAMPLE_TEXTURE2D_GRAD(Map, Sampler, UV1, DuvDx1, DuvDy1);
    float4 V2 = SAMPLE_TEXTURE2D_GRAD(Map, Sampler, UV2, DuvDx2, DuvDy2);
    float4 V3 = SAMPLE_TEXTURE2D_GRAD(Map, Sampler, UV3, DuvDx3, DuvDy3);
    BaryWeightBlend(Weights, V1, V2, V3, HexContrast, HexExp, HexGain);
    return Weights.x * V1 + Weights.y * V2 + Weights.z * V3;
}

#define SAMPLE_TEXTURE2D_HEX(textureName, samplerName, coord2) SampleTexture2DHex(textureName, samplerName, coord2)

#endif // XRENDER_COMMON_ANTI_TILLING_HLSL