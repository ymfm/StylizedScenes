#ifndef RAIN_LIGHTING_INCLUDED
#define RAIN_LIGHTING_INCLUDED

// 手动声明不透明纹理
TEXTURE2D(_CameraOpaqueTexture);
SAMPLER(sampler_CameraOpaqueTexture);

float3 SampleSceneColor(float2 uv)
{
    return SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv).rgb;
}

float3 HeightToNormal(float mask, float2 uv, float strength)
{
    float2 duv = fwidth(uv) + 1e-5;
    float dhdu = ddx(mask) / duv.x;
    float dhdv = ddy(mask) / duv.y;
    float3 n = float3(-dhdu * strength, -dhdv * strength, 1.0);
    return normalize(n);
}

void RainNormal_float(
    float  Mask,
    float2 UV,
    float  NormalStrength,
    float3 LightDir,
    float3 LightColor,
    float3 WaterColor,
    float  Ambient,
    float  RimPower,
    float  RimIntensity,
    float  HighlightPower,
    float  HighlightIntensity,
    float  RefractionStrength,
    float  Tint,
    out float3 Color,
    out float  Alpha)
{
    float3 N = HeightToNormal(Mask, UV, NormalStrength);

    float2 refractedUV = UV - N.xy * RefractionStrength;
    float3 background = SampleSceneColor(refractedUV);

    float3 L = normalize(-LightDir);
    float3 V = float3(0, 0, 1);
    float3 H = normalize(L + V);
    float  spec = pow(saturate(dot(N, H)), HighlightPower) * HighlightIntensity;

    float edge = pow(1.0 - saturate(N.z), RimPower);
    float darkEdge = saturate(1.0 - edge * RimIntensity);

    float3 refracted = lerp(background, background * WaterColor, Tint);
    refracted *= darkEdge;
    
    Color = refracted + spec * float3(1,1,1);
    Alpha = saturate(Mask);
}

#endif